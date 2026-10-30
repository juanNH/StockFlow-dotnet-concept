using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using Npgsql;
using StockFlow.Application.Abstractions;
using StockFlow.Application.Orders;
using StockFlow.Application.Products;
using StockFlow.Application.Warehouses;
using StockFlow.Domain.Orders;

namespace StockFlow.Infrastructure.Persistence;

internal sealed class PostgresOrderRepository(
    IDbConnectionFactory connectionFactory,
    ILogger<PostgresOrderRepository> logger) : IOrderRepository
{
    private const int ExpirationBatchSize = 100;

    public async Task<OrderWriteResult> CreateAndReserveAsync(
        CreateOrderPersistenceRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            var existingOrder = await LoadOrderByIdempotencyKeyAsync(
                connection,
                transaction,
                request.OrganizationId,
                request.IdempotencyKey,
                cancellationToken);
            if (existingOrder is not null)
            {
                EnsureMatchingFingerprint(existingOrder, request.RequestFingerprint);
                await transaction.CommitAsync(cancellationToken);
                return new OrderWriteResult(existingOrder.ToCreationSnapshot(), WasReplay: true);
            }

            await EnsureMembershipExistsAsync(
                connection,
                transaction,
                request.OrganizationId,
                request.CreatedByUserId,
                cancellationToken);
            await EnsureWarehouseExistsAsync(
                connection,
                transaction,
                request.OrganizationId,
                request.WarehouseId,
                cancellationToken);
            var productSnapshots = await LoadProductSnapshotsAsync(connection, transaction, request, cancellationToken);
            if (productSnapshots.Count != request.Lines.Count)
            {
                throw new ProductNotFoundException();
            }

            var orderId = Guid.NewGuid();
            var lineInputs = request.Lines.ToDictionary(line => line.ProductId);
            var orderLines = productSnapshots
                .Select(product =>
                {
                    if (!product.IsActive)
                    {
                        throw new OrderProductInactiveException();
                    }

                    var lineInput = lineInputs[product.Id];
                    return OrderLine.Create(
                        request.OrganizationId,
                        orderId,
                        product.Id,
                        product.Sku,
                        product.Name,
                        lineInput.Quantity,
                        product.UnitPrice,
                        product.Currency);
                })
                .OrderBy(line => line.ProductId)
                .ToArray();

            var order = Order.Reserve(
                orderId,
                request.OrganizationId,
                request.WarehouseId,
                request.CreatedByUserId,
                request.IdempotencyKey,
                request.RequestFingerprint,
                orderLines[0].Currency,
                request.CreatedAtUtc,
                request.ReservationExpiresAtUtc,
                orderLines);

            if (!await InsertOrderIfIdempotencyKeyIsAvailableAsync(connection, transaction, order, cancellationToken))
            {
                var concurrentOrder = await LoadOrderByIdempotencyKeyAsync(
                    connection,
                    transaction,
                    request.OrganizationId,
                    request.IdempotencyKey,
                    cancellationToken)
                    ?? throw new InvalidOperationException("An order idempotency conflict occurred without an existing order.");
                EnsureMatchingFingerprint(concurrentOrder, request.RequestFingerprint);
                await transaction.CommitAsync(cancellationToken);
                return new OrderWriteResult(concurrentOrder.ToCreationSnapshot(), WasReplay: true);
            }

            foreach (var line in order.Lines.OrderBy(line => line.ProductId))
            {
                await ReserveQuantityAsync(
                    connection,
                    transaction,
                    request.OrganizationId,
                    request.WarehouseId,
                    line.ProductId,
                    line.Quantity,
                    request.CreatedAtUtc,
                    cancellationToken);
                await InsertOrderLineAsync(connection, transaction, line, cancellationToken);
                await InsertReservationAsync(
                    connection,
                    transaction,
                    StockReservation.Create(
                        request.OrganizationId,
                        order.Id,
                        request.WarehouseId,
                        line.ProductId,
                        line.Quantity,
                        request.ReservationExpiresAtUtc),
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return new OrderWriteResult(order, WasReplay: false);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.NumericValueOutOfRange)
        {
            await RollbackAsync(transaction);
            throw new OrderStockQuantityOutOfRangeException();
        }
        catch
        {
            await RollbackAsync(transaction);
            throw;
        }
    }

    public async Task<Order?> GetByIdAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await LoadOrderByIdAsync(connection, transaction: null, organizationId, orderId, cancellationToken);
    }

    public async Task<ReservationExpirationResult> ExpireDueReservationsAsync(
        Guid organizationId,
        DateTimeOffset expiredAtUtc,
        CancellationToken cancellationToken)
    {
        var expirationTime = expiredAtUtc.ToUniversalTime();
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            var dueOrderIds = await LoadDueOrderIdsAsync(
                connection,
                transaction,
                organizationId,
                expirationTime,
                cancellationToken);
            var expiredOrders = 0;
            var releasedReservations = 0;
            long releasedQuantity = 0;

            foreach (var orderId in dueOrderIds)
            {
                var order = await LoadOrderByIdAsync(
                    connection,
                    transaction,
                    organizationId,
                    orderId,
                    cancellationToken)
                    ?? throw new InvalidOperationException("A locked expiring order disappeared before transition.");
                if (!order.Expire(expirationTime))
                {
                    continue;
                }

                var reservations = await LoadActiveReservationsAsync(
                    connection,
                    transaction,
                    organizationId,
                    orderId,
                    cancellationToken);
                if (reservations.Count != order.Lines.Count)
                {
                    throw new InvalidOperationException("The expiring order has an incomplete set of active reservations.");
                }

                foreach (var reservation in reservations)
                {
                    if (!reservation.Release(expirationTime))
                    {
                        continue;
                    }

                    await ReleaseQuantityAsync(connection, transaction, reservation, expirationTime, cancellationToken);
                    await MarkReservationReleasedAsync(connection, transaction, reservation, expirationTime, cancellationToken);
                    releasedReservations++;
                    releasedQuantity = checked(releasedQuantity + reservation.Quantity);
                }

                await MarkOrderExpiredAsync(connection, transaction, order, expirationTime, cancellationToken);
                expiredOrders++;
            }

            await transaction.CommitAsync(cancellationToken);
            return new ReservationExpirationResult(expiredOrders, releasedReservations, releasedQuantity);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.NumericValueOutOfRange)
        {
            await RollbackAsync(transaction);
            throw new OrderStockQuantityOutOfRangeException();
        }
        catch
        {
            await RollbackAsync(transaction);
            throw;
        }
    }

    private static async Task EnsureWarehouseExistsAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid organizationId,
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            "SELECT id FROM warehouses WHERE organization_id = @organizationId AND id = @warehouseId FOR KEY SHARE;",
            ("@organizationId", DbType.Guid, organizationId),
            ("@warehouseId", DbType.Guid, warehouseId));
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            throw new WarehouseNotFoundException();
        }
    }

    private static async Task EnsureMembershipExistsAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            "SELECT user_id FROM organization_memberships WHERE organization_id = @organizationId AND user_id = @userId FOR KEY SHARE;",
            ("@organizationId", DbType.Guid, organizationId),
            ("@userId", DbType.Guid, userId));
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            throw new OrderMembershipRequiredException();
        }
    }

    private static async Task<IReadOnlyList<ProductSnapshot>> LoadProductSnapshotsAsync(
        DbConnection connection,
        DbTransaction transaction,
        CreateOrderPersistenceRequest request,
        CancellationToken cancellationToken)
    {
        var productParameters = request.Lines
            .Select((line, index) => ($"@productId{index}", DbType.Guid, (object)line.ProductId))
            .ToArray();
        var productPlaceholders = string.Join(", ", productParameters.Select(parameter => parameter.Item1));
        await using var command = CreateCommand(
            connection,
            transaction,
            $"""
            SELECT id, sku, name, unit_price, currency, is_active
            FROM products
            WHERE organization_id = @organizationId
              AND id IN ({productPlaceholders})
            ORDER BY id
            FOR SHARE;
            """,
            [("@organizationId", DbType.Guid, request.OrganizationId), .. productParameters]);

        var products = new List<ProductSnapshot>(request.Lines.Count);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(new ProductSnapshot(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDecimal(3),
                reader.GetString(4),
                reader.GetBoolean(5)));
        }

        return products;
    }

    private static async Task<bool> InsertOrderIfIdempotencyKeyIsAvailableAsync(
        DbConnection connection,
        DbTransaction transaction,
        Order order,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO orders
                (id, organization_id, warehouse_id, created_by_user_id, idempotency_key,
                 request_fingerprint, status, currency, total_amount, created_at_utc,
                 reservation_expires_at_utc, expired_at_utc)
            VALUES
                (@id, @organizationId, @warehouseId, @createdByUserId, @idempotencyKey,
                 @requestFingerprint, @status, @currency, @totalAmount, @createdAtUtc,
                 @reservationExpiresAtUtc, NULL)
            ON CONFLICT (organization_id, idempotency_key) DO NOTHING
            RETURNING id;
            """,
            ("@id", DbType.Guid, order.Id),
            ("@organizationId", DbType.Guid, order.OrganizationId),
            ("@warehouseId", DbType.Guid, order.WarehouseId),
            ("@createdByUserId", DbType.Guid, order.CreatedByUserId),
            ("@idempotencyKey", DbType.String, order.IdempotencyKey),
            ("@requestFingerprint", DbType.String, order.RequestFingerprint),
            ("@status", DbType.String, order.Status.ToString()),
            ("@currency", DbType.String, order.Currency),
            ("@totalAmount", DbType.Decimal, order.TotalAmount),
            ("@createdAtUtc", DbType.DateTimeOffset, order.CreatedAtUtc),
            ("@reservationExpiresAtUtc", DbType.DateTimeOffset, order.ReservationExpiresAtUtc));

        var insertedOrderId = await command.ExecuteScalarAsync(cancellationToken);
        return insertedOrderId is not null and not DBNull;
    }

    private static async Task ReserveQuantityAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid organizationId,
        Guid warehouseId,
        Guid productId,
        long quantity,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            UPDATE inventory_balances
            SET quantity_reserved = quantity_reserved + @quantity,
                updated_at_utc = @updatedAtUtc
            WHERE organization_id = @organizationId
              AND warehouse_id = @warehouseId
              AND product_id = @productId
              AND quantity_on_hand - quantity_reserved >= @quantity
            RETURNING quantity_reserved;
            """,
            ("@quantity", DbType.Int64, quantity),
            ("@updatedAtUtc", DbType.DateTimeOffset, updatedAtUtc),
            ("@organizationId", DbType.Guid, organizationId),
            ("@warehouseId", DbType.Guid, warehouseId),
            ("@productId", DbType.Guid, productId));

        if (await command.ExecuteScalarAsync(cancellationToken) is null)
        {
            throw new OrderInsufficientStockException();
        }
    }

    private static async Task InsertOrderLineAsync(
        DbConnection connection,
        DbTransaction transaction,
        OrderLine line,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO order_lines
                (organization_id, order_id, product_id, product_sku, product_name,
                 quantity, unit_price, currency, line_total)
            VALUES
                (@organizationId, @orderId, @productId, @productSku, @productName,
                 @quantity, @unitPrice, @currency, @lineTotal);
            """,
            ("@organizationId", DbType.Guid, line.OrganizationId),
            ("@orderId", DbType.Guid, line.OrderId),
            ("@productId", DbType.Guid, line.ProductId),
            ("@productSku", DbType.String, line.ProductSku),
            ("@productName", DbType.String, line.ProductName),
            ("@quantity", DbType.Int64, line.Quantity),
            ("@unitPrice", DbType.Decimal, line.UnitPrice),
            ("@currency", DbType.String, line.Currency),
            ("@lineTotal", DbType.Decimal, line.LineTotal));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertReservationAsync(
        DbConnection connection,
        DbTransaction transaction,
        StockReservation reservation,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO stock_reservations
                (organization_id, order_id, warehouse_id, product_id, quantity,
                 status, expires_at_utc, released_at_utc)
            VALUES
                (@organizationId, @orderId, @warehouseId, @productId, @quantity,
                 @status, @expiresAtUtc, NULL);
            """,
            ("@organizationId", DbType.Guid, reservation.OrganizationId),
            ("@orderId", DbType.Guid, reservation.OrderId),
            ("@warehouseId", DbType.Guid, reservation.WarehouseId),
            ("@productId", DbType.Guid, reservation.ProductId),
            ("@quantity", DbType.Int64, reservation.Quantity),
            ("@status", DbType.String, reservation.Status.ToString()),
            ("@expiresAtUtc", DbType.DateTimeOffset, reservation.ExpiresAtUtc));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<List<Guid>> LoadDueOrderIdsAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid organizationId,
        DateTimeOffset expiredAtUtc,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            SELECT id
            FROM orders
            WHERE organization_id = @organizationId
              AND status = 'Reserved'
              AND reservation_expires_at_utc <= @expiredAtUtc
            ORDER BY reservation_expires_at_utc, id
            LIMIT @batchSize
            FOR UPDATE SKIP LOCKED;
            """,
            ("@organizationId", DbType.Guid, organizationId),
            ("@expiredAtUtc", DbType.DateTimeOffset, expiredAtUtc),
            ("@batchSize", DbType.Int32, ExpirationBatchSize));

        var orderIds = new List<Guid>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            orderIds.Add(reader.GetGuid(0));
        }

        return orderIds;
    }

    private static async Task<IReadOnlyList<StockReservation>> LoadActiveReservationsAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid organizationId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            SELECT warehouse_id, product_id, quantity, status, expires_at_utc, released_at_utc
            FROM stock_reservations
            WHERE organization_id = @organizationId
              AND order_id = @orderId
              AND status = 'Active'
            ORDER BY product_id
            FOR UPDATE;
            """,
            ("@organizationId", DbType.Guid, organizationId),
            ("@orderId", DbType.Guid, orderId));

        var reservations = new List<StockReservation>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            reservations.Add(StockReservation.Restore(
                organizationId,
                orderId,
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetInt64(2),
                Enum.Parse<ReservationStatus>(reader.GetString(3), ignoreCase: false),
                reader.GetFieldValue<DateTimeOffset>(4),
                reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5)));
        }

        return reservations;
    }

    private static async Task ReleaseQuantityAsync(
        DbConnection connection,
        DbTransaction transaction,
        StockReservation reservation,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            UPDATE inventory_balances
            SET quantity_reserved = quantity_reserved - @quantity,
                updated_at_utc = @updatedAtUtc
            WHERE organization_id = @organizationId
              AND warehouse_id = @warehouseId
              AND product_id = @productId
              AND quantity_reserved >= @quantity
            RETURNING quantity_reserved;
            """,
            ("@quantity", DbType.Int64, reservation.Quantity),
            ("@updatedAtUtc", DbType.DateTimeOffset, updatedAtUtc),
            ("@organizationId", DbType.Guid, reservation.OrganizationId),
            ("@warehouseId", DbType.Guid, reservation.WarehouseId),
            ("@productId", DbType.Guid, reservation.ProductId));

        if (await command.ExecuteScalarAsync(cancellationToken) is null)
        {
            throw new InvalidOperationException("The inventory reservation counter is inconsistent with its reservation row.");
        }
    }

    private static async Task MarkReservationReleasedAsync(
        DbConnection connection,
        DbTransaction transaction,
        StockReservation reservation,
        DateTimeOffset releasedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            UPDATE stock_reservations
            SET status = 'Released', released_at_utc = @releasedAtUtc
            WHERE organization_id = @organizationId
              AND order_id = @orderId
              AND product_id = @productId
              AND status = 'Active';
            """,
            ("@releasedAtUtc", DbType.DateTimeOffset, releasedAtUtc),
            ("@organizationId", DbType.Guid, reservation.OrganizationId),
            ("@orderId", DbType.Guid, reservation.OrderId),
            ("@productId", DbType.Guid, reservation.ProductId));

        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw new InvalidOperationException("The active reservation changed before its release could be committed.");
        }
    }

    private static async Task MarkOrderExpiredAsync(
        DbConnection connection,
        DbTransaction transaction,
        Order order,
        DateTimeOffset expiredAtUtc,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            UPDATE orders
            SET status = 'Expired', expired_at_utc = @expiredAtUtc
            WHERE organization_id = @organizationId
              AND id = @orderId
              AND status = 'Reserved'
              AND reservation_expires_at_utc <= @expiredAtUtc;
            """,
            ("@expiredAtUtc", DbType.DateTimeOffset, expiredAtUtc),
            ("@organizationId", DbType.Guid, order.OrganizationId),
            ("@orderId", DbType.Guid, order.Id));

        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw new InvalidOperationException("The order state changed before expiration could be committed.");
        }
    }

    private static async Task<Order?> LoadOrderByIdempotencyKeyAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await LoadOrderAsync(
            connection,
            transaction,
            """
            SELECT id, warehouse_id, created_by_user_id, idempotency_key, request_fingerprint,
                   status, currency, total_amount, created_at_utc, reservation_expires_at_utc, expired_at_utc
            FROM orders
            WHERE organization_id = @organizationId AND idempotency_key = @idempotencyKey;
            """,
            [
                ("@organizationId", DbType.Guid, organizationId),
                ("@idempotencyKey", DbType.String, idempotencyKey)
            ],
            organizationId,
            cancellationToken);
    }

    private static async Task<Order?> LoadOrderByIdAsync(
        DbConnection connection,
        DbTransaction? transaction,
        Guid organizationId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return await LoadOrderAsync(
            connection,
            transaction,
            """
            SELECT id, warehouse_id, created_by_user_id, idempotency_key, request_fingerprint,
                   status, currency, total_amount, created_at_utc, reservation_expires_at_utc, expired_at_utc
            FROM orders
            WHERE organization_id = @organizationId AND id = @orderId;
            """,
            [
                ("@organizationId", DbType.Guid, organizationId),
                ("@orderId", DbType.Guid, orderId)
            ],
            organizationId,
            cancellationToken);
    }

    private static async Task<Order?> LoadOrderAsync(
        DbConnection connection,
        DbTransaction? transaction,
        string sql,
        (string Name, DbType Type, object Value)[] parameters,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        Guid orderId;
        Guid warehouseId;
        Guid createdByUserId;
        string idempotencyKey;
        string requestFingerprint;
        OrderStatus status;
        string currency;
        decimal totalAmount;
        DateTimeOffset createdAtUtc;
        DateTimeOffset reservationExpiresAtUtc;
        DateTimeOffset? expiredAtUtc;

        await using (var command = CreateCommand(connection, transaction, sql, parameters))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            orderId = reader.GetGuid(0);
            warehouseId = reader.GetGuid(1);
            createdByUserId = reader.GetGuid(2);
            idempotencyKey = reader.GetString(3);
            requestFingerprint = reader.GetString(4);
            status = Enum.Parse<OrderStatus>(reader.GetString(5), ignoreCase: false);
            currency = reader.GetString(6);
            totalAmount = reader.GetDecimal(7);
            createdAtUtc = reader.GetFieldValue<DateTimeOffset>(8);
            reservationExpiresAtUtc = reader.GetFieldValue<DateTimeOffset>(9);
            expiredAtUtc = reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10);
        }

        var lines = await LoadOrderLinesAsync(connection, transaction, organizationId, orderId, cancellationToken);
        return Order.Restore(
            orderId,
            organizationId,
            warehouseId,
            createdByUserId,
            idempotencyKey,
            requestFingerprint,
            status,
            currency,
            totalAmount,
            createdAtUtc,
            reservationExpiresAtUtc,
            expiredAtUtc,
            lines);
    }

    private static async Task<IReadOnlyList<OrderLine>> LoadOrderLinesAsync(
        DbConnection connection,
        DbTransaction? transaction,
        Guid organizationId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            SELECT product_id, product_sku, product_name, quantity, unit_price, currency, line_total
            FROM order_lines
            WHERE organization_id = @organizationId AND order_id = @orderId
            ORDER BY product_id;
            """,
            ("@organizationId", DbType.Guid, organizationId),
            ("@orderId", DbType.Guid, orderId));

        var lines = new List<OrderLine>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            lines.Add(OrderLine.Restore(
                organizationId,
                orderId,
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt64(3),
                reader.GetDecimal(4),
                reader.GetString(5),
                reader.GetDecimal(6)));
        }

        return lines;
    }

    private static void EnsureMatchingFingerprint(Order order, string requestFingerprint)
    {
        if (!string.Equals(order.RequestFingerprint, requestFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            throw new OrderIdempotencyConflictException();
        }
    }

    private static DbCommand CreateCommand(
        DbConnection connection,
        DbTransaction? transaction,
        string commandText,
        params (string Name, DbType Type, object Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        foreach (var (name, type, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.DbType = type;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        return command;
    }

    private async Task RollbackAsync(DbTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception rollbackException)
        {
            logger.LogError(rollbackException, "Rolling back an order transaction failed.");
        }
    }

    private sealed record ProductSnapshot(
        Guid Id,
        string Sku,
        string Name,
        decimal UnitPrice,
        string Currency,
        bool IsActive);
}

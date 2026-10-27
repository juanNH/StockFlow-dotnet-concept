using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using StockFlow.Application.Abstractions;
using StockFlow.Application.Inventory;
using StockFlow.Application.Products;
using StockFlow.Application.Warehouses;
using StockFlow.Domain.Inventory;

namespace StockFlow.Infrastructure.Persistence;

internal sealed class EfInventoryRepository(
    StockFlowDbContext dbContext,
    IDbConnectionFactory connectionFactory,
    ICurrentTenant currentTenant,
    ILogger<EfInventoryRepository> logger) : IInventoryRepository
{
    public async Task<StockMovement> ApplyAdjustmentAsync(
        StockAdjustment adjustment,
        CancellationToken cancellationToken)
    {
        if (adjustment.OrganizationId != currentTenant.OrganizationId
            || adjustment.PerformedByUserId != currentTenant.UserId)
        {
            throw new InvalidOperationException("A stock adjustment must use the current organization and user.");
        }

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            await EnsureReferencesExistAsync(connection, transaction, adjustment, cancellationToken);

            if (adjustment.QuantityDelta > 0)
            {
                await EnsureBalanceRowExistsAsync(connection, transaction, adjustment, cancellationToken);
            }

            var quantityAfter = await ApplyQuantityDeltaAsync(connection, transaction, adjustment, cancellationToken);
            var movement = adjustment.ToMovement(quantityAfter);
            await InsertMovementAsync(connection, transaction, movement, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return movement;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.NumericValueOutOfRange)
        {
            await RollbackAsync(transaction);
            throw new StockQuantityOutOfRangeException();
        }
        catch
        {
            await RollbackAsync(transaction);
            throw;
        }
    }

    public async Task<IReadOnlyList<InventoryBalance>> GetBalancesAsync(
        Guid organizationId,
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        return await dbContext.InventoryBalances
            .AsNoTracking()
            .Where(balance => balance.OrganizationId == organizationId && balance.WarehouseId == warehouseId)
            .OrderBy(balance => balance.ProductId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<StockMovementPage> GetMovementsAsync(
        Guid organizationId,
        Guid warehouseId,
        StockMovementSearchOptions options,
        CancellationToken cancellationToken)
    {
        var query = dbContext.StockMovements
            .AsNoTracking()
            .Where(movement => movement.OrganizationId == organizationId && movement.WarehouseId == warehouseId);
        if (options.ProductId is { } productId)
        {
            query = query.Where(movement => movement.ProductId == productId);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var skip = checked((options.Page - 1) * options.PageSize);
        var items = await query
            .OrderByDescending(movement => movement.CreatedAtUtc)
            .ThenByDescending(movement => movement.Id)
            .Skip(skip)
            .Take(options.PageSize)
            .ToArrayAsync(cancellationToken);

        return new StockMovementPage(items, totalCount);
    }

    private static async Task EnsureReferencesExistAsync(
        DbConnection connection,
        DbTransaction transaction,
        StockAdjustment adjustment,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            SELECT
                EXISTS (SELECT 1 FROM warehouses WHERE organization_id = @organizationId AND id = @warehouseId),
                EXISTS (SELECT 1 FROM products WHERE organization_id = @organizationId AND id = @productId);
            """,
            ("@organizationId", DbType.Guid, adjustment.OrganizationId),
            ("@warehouseId", DbType.Guid, adjustment.WarehouseId),
            ("@productId", DbType.Guid, adjustment.ProductId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("The stock adjustment reference check returned no result.");
        }

        if (!reader.GetBoolean(0))
        {
            throw new WarehouseNotFoundException();
        }

        if (!reader.GetBoolean(1))
        {
            throw new ProductNotFoundException();
        }
    }

    private static async Task EnsureBalanceRowExistsAsync(
        DbConnection connection,
        DbTransaction transaction,
        StockAdjustment adjustment,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO inventory_balances
                (organization_id, warehouse_id, product_id, quantity_on_hand, updated_at_utc)
            VALUES (@organizationId, @warehouseId, @productId, 0, @createdAtUtc)
            ON CONFLICT (organization_id, warehouse_id, product_id) DO NOTHING;
            """,
            ("@organizationId", DbType.Guid, adjustment.OrganizationId),
            ("@warehouseId", DbType.Guid, adjustment.WarehouseId),
            ("@productId", DbType.Guid, adjustment.ProductId),
            ("@createdAtUtc", DbType.DateTimeOffset, adjustment.CreatedAtUtc));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<long> ApplyQuantityDeltaAsync(
        DbConnection connection,
        DbTransaction transaction,
        StockAdjustment adjustment,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            UPDATE inventory_balances
            SET quantity_on_hand = quantity_on_hand + @quantityDelta,
                updated_at_utc = @createdAtUtc
            WHERE organization_id = @organizationId
              AND warehouse_id = @warehouseId
              AND product_id = @productId
              AND quantity_on_hand + @quantityDelta >= 0
            RETURNING quantity_on_hand;
            """,
            ("@quantityDelta", DbType.Int64, adjustment.QuantityDelta),
            ("@createdAtUtc", DbType.DateTimeOffset, adjustment.CreatedAtUtc),
            ("@organizationId", DbType.Guid, adjustment.OrganizationId),
            ("@warehouseId", DbType.Guid, adjustment.WarehouseId),
            ("@productId", DbType.Guid, adjustment.ProductId));

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            if (adjustment.QuantityDelta < 0)
            {
                throw new InsufficientStockException();
            }

            throw new InvalidOperationException("The positive stock adjustment did not update an inventory balance.");
        }

        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private static async Task InsertMovementAsync(
        DbConnection connection,
        DbTransaction transaction,
        StockMovement movement,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO stock_movements
                (id, organization_id, warehouse_id, product_id, performed_by_user_id,
                 quantity_delta, quantity_after, reason, created_at_utc)
            VALUES
                (@id, @organizationId, @warehouseId, @productId, @performedByUserId,
                 @quantityDelta, @quantityAfter, @reason, @createdAtUtc);
            """,
            ("@id", DbType.Guid, movement.Id),
            ("@organizationId", DbType.Guid, movement.OrganizationId),
            ("@warehouseId", DbType.Guid, movement.WarehouseId),
            ("@productId", DbType.Guid, movement.ProductId),
            ("@performedByUserId", DbType.Guid, movement.PerformedByUserId),
            ("@quantityDelta", DbType.Int64, movement.QuantityDelta),
            ("@quantityAfter", DbType.Int64, movement.QuantityAfter),
            ("@reason", DbType.String, movement.Reason),
            ("@createdAtUtc", DbType.DateTimeOffset, movement.CreatedAtUtc));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DbCommand CreateCommand(
        DbConnection connection,
        DbTransaction transaction,
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
            logger.LogError(rollbackException, "Rolling back a stock adjustment transaction failed.");
        }
    }
}

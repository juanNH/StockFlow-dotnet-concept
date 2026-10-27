using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;
using StockFlow.Application.Abstractions;
using StockFlow.Application.Products;
using StockFlow.Domain.Products;

namespace StockFlow.Infrastructure.Persistence;

internal sealed class EfProductRepository(
    StockFlowDbContext dbContext,
    ICurrentTenant currentTenant) : IProductRepository
{
    public Task<Product?> GetByIdAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return dbContext.Products.SingleOrDefaultAsync(
            product => product.OrganizationId == organizationId && product.Id == productId,
            cancellationToken);
    }

    public async Task<ProductPage> SearchAsync(
        Guid organizationId,
        ProductSearchOptions options,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Products
            .AsNoTracking()
            .Where(product => product.OrganizationId == organizationId);
        if (!options.IncludeInactive)
        {
            query = query.Where(product => product.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var pattern = $"%{EscapeLikeTerm(options.Search.Trim())}%";
            query = query.Where(product =>
                EF.Functions.ILike(product.Sku, pattern, "!")
                || EF.Functions.ILike(product.Name, pattern, "!"));
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var sortBy = options.SortBy.ToLowerInvariant();
        var descending = options.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        IOrderedQueryable<Product> orderedQuery = sortBy switch
        {
            "sku" => descending ? query.OrderByDescending(product => product.Sku) : query.OrderBy(product => product.Sku),
            "unitprice" => descending ? query.OrderByDescending(product => product.UnitPrice) : query.OrderBy(product => product.UnitPrice),
            "createdatutc" => descending ? query.OrderByDescending(product => product.CreatedAtUtc) : query.OrderBy(product => product.CreatedAtUtc),
            _ => descending ? query.OrderByDescending(product => product.Name) : query.OrderBy(product => product.Name)
        };

        var skip = checked((options.Page - 1) * options.PageSize);
        var items = await orderedQuery
            .ThenBy(product => product.Id)
            .Skip(skip)
            .Take(options.PageSize)
            .ToArrayAsync(cancellationToken);

        return new ProductPage(items, totalCount);
    }

    public void Add(Product product)
    {
        if (product.OrganizationId != currentTenant.OrganizationId)
        {
            throw new InvalidOperationException("A product can only be added to the current organization.");
        }

        dbContext.Products.Add(product);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsProductSkuConflict(exception))
        {
            throw new ProductSkuConflictException();
        }
    }

    private static bool IsProductSkuConflict(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_products_organization_sku"
        };
    }

    private static string EscapeLikeTerm(string term)
    {
        return term
            .Replace("!", "!!", StringComparison.Ordinal)
            .Replace("%", "!%", StringComparison.Ordinal)
            .Replace("_", "!_", StringComparison.Ordinal);
    }
}

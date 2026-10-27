using System.Data.Common;
using Npgsql;
using StockFlow.Application.Abstractions;

namespace StockFlow.Infrastructure.Persistence;

internal sealed class NpgsqlDbConnectionFactory(NpgsqlDataSource dataSource) : IDbConnectionFactory
{
    public async ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        return await dataSource.OpenConnectionAsync(cancellationToken);
    }
}

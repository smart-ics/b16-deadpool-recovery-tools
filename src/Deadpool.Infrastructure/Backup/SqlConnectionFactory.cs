using System.Data.Common;
using Deadpool.Core.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Deadpool.Infrastructure.Backup;

/// <summary>
/// Production implementation: opens a real SqlConnection to SQL Server.
/// </summary>
internal sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IOptions<DeadpoolSettings> settings)
    {
        _connectionString = settings.Value.SqlConnectionString;
    }

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}


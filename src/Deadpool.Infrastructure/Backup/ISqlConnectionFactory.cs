using System.Data.Common;

namespace Deadpool.Infrastructure.Backup;

/// <summary>
/// Creates an open ADO.NET connection to the SQL Server instance being backed up.
/// Abstracted so tests can inject a FakeDbConnection without hitting a real server.
/// </summary>
internal interface ISqlConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}


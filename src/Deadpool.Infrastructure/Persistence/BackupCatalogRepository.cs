using Dapper;
using Deadpool.Core.Domain.Entities;
using Deadpool.Core.Domain.Enums;
using Deadpool.Core.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Deadpool.Infrastructure.Persistence;

/// <summary>
/// SQLite implementation of IBackupCatalogRepository using Dapper.
/// Phase-1 skeleton — SQL DDL and queries pending.
/// </summary>
internal sealed class BackupCatalogRepository : IBackupCatalogRepository
{
    private readonly string _connectionString;

    public BackupCatalogRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DeadpoolDb")
            ?? "Data Source=deadpool.db";
    }

    private SqliteConnection CreateConnection() => new(_connectionString);

    public async Task<BackupCatalog?> GetByIdAsync(Guid backupId, CancellationToken cancellationToken = default)
    {
        // TODO: SELECT * FROM BackupCatalog WHERE BackupId = @backupId
        await using var conn = CreateConnection();
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<BackupCatalog>> GetByDatabaseAsync(Guid databaseId, CancellationToken cancellationToken = default)
    {
        // TODO: SELECT * FROM BackupCatalog WHERE DatabaseId = @databaseId ORDER BY BackupDate DESC
        await using var conn = CreateConnection();
        throw new NotImplementedException();
    }

    public async Task AddAsync(BackupCatalog catalog, CancellationToken cancellationToken = default)
    {
        // TODO: INSERT INTO BackupCatalog ...
        await using var conn = CreateConnection();
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(BackupCatalog catalog, CancellationToken cancellationToken = default)
    {
        // TODO: UPDATE BackupCatalog SET ... WHERE BackupId = @BackupId
        await using var conn = CreateConnection();
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<BackupCatalog>> GetExpiredAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        // TODO: SELECT * FROM BackupCatalog WHERE BackupDate < @olderThan
        await using var conn = CreateConnection();
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(Guid backupId, CancellationToken cancellationToken = default)
    {
        // TODO: DELETE FROM BackupCatalog WHERE BackupId = @backupId
        await using var conn = CreateConnection();
        throw new NotImplementedException();
    }
}


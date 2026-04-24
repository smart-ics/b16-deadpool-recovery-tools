using Deadpool.Core.Domain.Entities;

namespace Deadpool.Core.Interfaces;

/// <summary>
/// Persistence contract for BackupCatalog records (SQLite via Dapper).
/// Implementation lives in Deadpool.Infrastructure.
/// </summary>
public interface IBackupCatalogRepository
{
    Task<BackupCatalog?> GetByIdAsync(Guid backupId, CancellationToken cancellationToken = default);
    Task<IEnumerable<BackupCatalog>> GetByDatabaseAsync(Guid databaseId, CancellationToken cancellationToken = default);
    Task AddAsync(BackupCatalog catalog, CancellationToken cancellationToken = default);
    Task UpdateAsync(BackupCatalog catalog, CancellationToken cancellationToken = default);

    /// <summary>Returns all catalog entries older than <paramref name="olderThan"/> for retention cleanup.</summary>
    Task<IEnumerable<BackupCatalog>> GetExpiredAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid backupId, CancellationToken cancellationToken = default);
}


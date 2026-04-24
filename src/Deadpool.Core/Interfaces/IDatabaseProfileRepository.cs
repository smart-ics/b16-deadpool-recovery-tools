using Deadpool.Core.Domain.Entities;

namespace Deadpool.Core.Interfaces;

/// <summary>
/// Repository for DatabaseProfile records stored in the SQLite metadata catalog.
/// Implementation lives in Deadpool.Infrastructure.
/// </summary>
public interface IDatabaseProfileRepository
{
    /// <summary>
    /// Returns all active database profiles configured for backup.
    /// Used by scheduler to register Quartz jobs at startup.
    /// </summary>
    Task<IEnumerable<DatabaseProfile>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single profile by ID, or null if not found.
    /// Used by BackupJob to load profile details during execution.
    /// </summary>
    Task<DatabaseProfile?> GetByIdAsync(Guid databaseId, CancellationToken cancellationToken = default);
}


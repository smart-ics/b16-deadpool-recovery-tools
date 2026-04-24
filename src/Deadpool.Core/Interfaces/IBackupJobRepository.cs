using Deadpool.Core.Domain.Entities;
using Deadpool.Core.Domain.Enums;

namespace Deadpool.Core.Interfaces;

/// <summary>
/// Persistence contract for BackupJob records (SQLite via Dapper).
/// Implementation lives in Deadpool.Infrastructure.
/// </summary>
public interface IBackupJobRepository
{
    Task<BackupJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>Returns the currently active (non-terminal) job for a database, if any.</summary>
    Task<BackupJob?> GetActiveJobAsync(Guid databaseId, CancellationToken cancellationToken = default);

    Task AddAsync(BackupJob job, CancellationToken cancellationToken = default);
    Task UpdateStateAsync(Guid jobId, BackupJobState newState, string? errorMessage = null, CancellationToken cancellationToken = default);
}


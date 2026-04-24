using Deadpool.Core.Domain.Entities;
using Deadpool.Core.Domain.Enums;

namespace Deadpool.Core.Interfaces;

/// <summary>
/// Executes native T-SQL backup commands against SQL Server.
/// Implementation lives in Deadpool.Infrastructure.
/// </summary>
public interface IBackupExecutor
{
    /// <summary>
    /// Executes a backup for the given database profile and type.
    /// Returns the local path of the produced backup file.
    /// </summary>
    Task<string> ExecuteAsync(DatabaseProfile profile, BackupType backupType, CancellationToken cancellationToken = default);
}


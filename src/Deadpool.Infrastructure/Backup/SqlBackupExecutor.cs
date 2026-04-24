using Deadpool.Core.Domain.Entities;
using Deadpool.Core.Domain.Enums;
using Deadpool.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Deadpool.Infrastructure.Backup;

/// <summary>
/// Executes native T-SQL BACKUP commands against SQL Server.
/// Phase-1 skeleton — full T-SQL implementation pending.
/// </summary>
internal sealed class SqlBackupExecutor : IBackupExecutor
{
    private readonly ILogger<SqlBackupExecutor> _logger;

    public SqlBackupExecutor(ILogger<SqlBackupExecutor> logger)
    {
        _logger = logger;
    }

    public Task<string> ExecuteAsync(DatabaseProfile profile, BackupType backupType, CancellationToken cancellationToken = default)
    {
        // TODO: Build T-SQL BACKUP command and execute via SqlConnection
        // Full  -> BACKUP DATABASE ... TO DISK WITH COMPRESSION, CHECKSUM, INIT, STATS
        // Diff  -> BACKUP DATABASE ... TO DISK WITH DIFFERENTIAL, COMPRESSION, CHECKSUM
        // Log   -> BACKUP LOG       ... TO DISK WITH CHECKSUM
        throw new NotImplementedException("SQL backup execution is not yet implemented.");
    }
}


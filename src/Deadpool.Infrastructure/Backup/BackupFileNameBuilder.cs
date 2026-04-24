using Deadpool.Core.Domain.Enums;

namespace Deadpool.Infrastructure.Backup;

/// <summary>
/// Builds the local backup file path from profile info + backup type + UTC timestamp.
///
/// Naming convention (per ARCHITECTURE.md §7):
///   DB_FULL_yyyyMMdd_HHmmss.bak
///   DB_DIFF_yyyyMMdd_HHmmss.bak
///   DB_LOG_yyyyMMdd_HHmmss.trn
/// </summary>
internal static class BackupFileNameBuilder
{
    public static string Build(string localBackupRoot, string databaseName, BackupType backupType)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        var typeCode = backupType switch
        {
            BackupType.Full           => "FULL",
            BackupType.Differential   => "DIFF",
            BackupType.TransactionLog => "LOG",
            _                         => "FULL"
        };

        var extension = backupType == BackupType.TransactionLog ? ".trn" : ".bak";
        var fileName  = $"{databaseName}_{typeCode}_{timestamp}{extension}";

        return Path.Combine(localBackupRoot, fileName);
    }
}


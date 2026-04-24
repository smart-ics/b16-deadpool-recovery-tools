namespace Deadpool.Core.Configuration;

/// <summary>
/// Application-level settings bound from the "Deadpool" section of appsettings.json.
/// </summary>
public sealed class DeadpoolSettings
{
    public const string SectionName = "Deadpool";

    /// <summary>Local path where backup files are written before being copied.</summary>
    public string LocalBackupRoot { get; set; } = string.Empty;

    /// <summary>Root UNC path on the backup storage server (e.g. \\BackupServer\Backup\HospitalA).</summary>
    public string StorageRoot { get; set; } = string.Empty;

    /// <summary>Retention in days for local backup files.</summary>
    public int LocalRetentionDays { get; set; } = 3;

    /// <summary>Retention in days for storage server backup files.</summary>
    public int StorageRetentionDays { get; set; } = 14;

    /// <summary>Maximum number of copy attempts before the job is marked Failed.</summary>
    public int CopyMaxAttempts { get; set; } = 3;

    /// <summary>Delay in seconds between copy retry attempts.</summary>
    public int CopyRetryDelaySeconds { get; set; } = 30;
}


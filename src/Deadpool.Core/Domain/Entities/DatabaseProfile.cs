using Deadpool.Core.Domain.Enums;

namespace Deadpool.Core.Domain.Entities;

/// <summary>
/// Backup schedule and configuration profile for a specific database.
/// </summary>
public sealed class DatabaseProfile
{
    public Guid DatabaseId { get; set; }
    public Guid ServerId { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public RecoveryModel RecoveryModel { get; set; }
    public bool LogBackupEnabled { get; set; }

    /// <summary>Cron expression for full backups (e.g. "0 2 * * SUN").</summary>
    public string FullBackupSchedule { get; set; } = string.Empty;

    /// <summary>Cron expression for differential backups (e.g. "0 3 * * MON-FRI").</summary>
    public string DiffBackupSchedule { get; set; } = string.Empty;

    /// <summary>Interval in minutes for transaction log backups.</summary>
    public int LogBackupEveryMinute { get; set; }

    public int RetentionDays { get; set; }
}


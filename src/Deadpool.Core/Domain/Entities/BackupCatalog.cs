using Deadpool.Core.Domain.Enums;

namespace Deadpool.Core.Domain.Entities;

/// <summary>
/// Immutable record of a completed (or attempted) backup in the metadata catalog.
/// Naming convention: DB_FULL_yyyyMMdd_HHmmss.bak | DB_DIFF_... | DB_LOG_....trn
/// </summary>
public sealed class BackupCatalog
{
    public Guid BackupId { get; set; }
    public Guid DatabaseId { get; set; }
    public BackupType BackupType { get; set; }
    public DateTime BackupDate { get; set; }

    /// <summary>Full local path of the backup file.</summary>
    public string BackupFile { get; set; } = string.Empty;

    /// <summary>UNC path on the backup storage server.</summary>
    public string StoragePath { get; set; } = string.Empty;

    public decimal FileSizeMB { get; set; }
    public BackupJobState Status { get; set; }
    public bool Verified { get; set; }

    /// <summary>Reference to the parent Full backup (used for Differential).</summary>
    public Guid? ParentBackupId { get; set; }

    public int CopyAttempts { get; set; }
    public string? ErrorMessage { get; set; }
}


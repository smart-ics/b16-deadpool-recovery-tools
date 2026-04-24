using Deadpool.Core.Domain.Enums;

namespace Deadpool.Core.Domain.Entities;

/// <summary>
/// Represents a single scheduled backup job execution.
/// One active job per database at any time (no overlapping).
/// </summary>
public sealed class BackupJob
{
    public Guid JobId { get; set; }
    public Guid DatabaseId { get; set; }
    public BackupType BackupType { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public BackupJobState State { get; set; }
    public string? ErrorMessage { get; set; }
}


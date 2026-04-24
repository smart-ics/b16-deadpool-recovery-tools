using Deadpool.Core.Domain.Enums;

namespace Deadpool.Infrastructure.Backup;

/// <summary>
/// Builds parameterised T-SQL BACKUP / RESTORE VERIFYONLY command strings.
/// Pure static — no I/O, fully unit-testable without a database connection.
/// All commands use named parameter @FilePath.
/// </summary>
internal static class BackupSqlBuilder
{
    /// <summary>Returns the BACKUP command SQL for the given backup type.</summary>
    public static string BuildBackupSql(string databaseName, BackupType backupType) =>
        backupType switch
        {
            BackupType.Full           => BuildFullSql(databaseName),
            BackupType.Differential   => BuildDiffSql(databaseName),
            BackupType.TransactionLog => BuildLogSql(databaseName),
            _ => throw new ArgumentOutOfRangeException(
                     nameof(backupType), backupType, "Unsupported backup type.")
        };

    /// <summary>
    /// Returns the RESTORE VERIFYONLY command SQL.
    /// Validates header integrity + checksum — does not restore any data.
    /// </summary>
    public static string BuildVerifyOnlySql() =>
        "RESTORE VERIFYONLY FROM DISK = @FilePath WITH CHECKSUM";

    // ── Private command builders ──────────────────────────────────────────────

    // ARCHITECTURE.md §11: Full — COMPRESSION, CHECKSUM, INIT, STATS
    private static string BuildFullSql(string db) =>
        $"""
        BACKUP DATABASE [{db}]
        TO DISK = @FilePath
        WITH COMPRESSION, CHECKSUM, INIT, STATS = 10
        """;

    // ARCHITECTURE.md §11: Differential — DIFFERENTIAL, COMPRESSION, CHECKSUM
    private static string BuildDiffSql(string db) =>
        $"""
        BACKUP DATABASE [{db}]
        TO DISK = @FilePath
        WITH DIFFERENTIAL, COMPRESSION, CHECKSUM, STATS = 10
        """;

    // ARCHITECTURE.md §11: Log — BACKUP LOG, CHECKSUM (no COMPRESSION keyword for log)
    private static string BuildLogSql(string db) =>
        $"""
        BACKUP LOG [{db}]
        TO DISK = @FilePath
        WITH CHECKSUM, STATS = 10
        """;
}


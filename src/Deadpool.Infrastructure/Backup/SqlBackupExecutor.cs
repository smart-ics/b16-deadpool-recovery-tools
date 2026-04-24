using Dapper;
using Deadpool.Core.Configuration;
using Deadpool.Core.Domain.Entities;
using Deadpool.Core.Domain.Enums;
using Deadpool.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Deadpool.Infrastructure.Backup;

/// <summary>
/// Executes native T-SQL BACKUP commands against SQL Server via Dapper.
///
/// Workflow per execution:
///   1. Build file path (BackupFileNameBuilder)
///   2. Execute BACKUP DATABASE / BACKUP LOG command (CommandTimeout = 0 — unbounded)
///   3. Execute RESTORE VERIFYONLY to validate checksum + header integrity
///   4. Return the local file path on success
/// </summary>
internal sealed class SqlBackupExecutor : IBackupExecutor
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly DeadpoolSettings _settings;
    private readonly ILogger<SqlBackupExecutor> _logger;

    public SqlBackupExecutor(
        ISqlConnectionFactory connectionFactory,
        IOptions<DeadpoolSettings> settings,
        ILogger<SqlBackupExecutor> logger)
    {
        _connectionFactory = connectionFactory;
        _settings          = settings.Value;
        _logger            = logger;
    }

    public async Task<string> ExecuteAsync(
        DatabaseProfile profile,
        BackupType backupType,
        CancellationToken cancellationToken = default)
    {
        var filePath = BackupFileNameBuilder.Build(
            _settings.LocalBackupRoot, profile.DatabaseName, backupType);

        _logger.LogInformation(
            "Starting {BackupType} backup: {Database} -> {FilePath}",
            backupType, profile.DatabaseName, filePath);

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // ── Step 1: Execute BACKUP ────────────────────────────────────────────
        var backupSql = BackupSqlBuilder.BuildBackupSql(profile.DatabaseName, backupType);

        await connection.ExecuteAsync(new CommandDefinition(
            commandText:       backupSql,
            parameters:        new { FilePath = filePath },
            commandTimeout:    0,       // no timeout — backup duration is unbounded
            cancellationToken: cancellationToken));

        _logger.LogInformation("Backup file written: {FilePath}", filePath);

        // ── Step 2: RESTORE VERIFYONLY — validate header + checksum ──────────
        var verifySql = BackupSqlBuilder.BuildVerifyOnlySql();

        await connection.ExecuteAsync(new CommandDefinition(
            commandText:       verifySql,
            parameters:        new { FilePath = filePath },
            commandTimeout:    0,
            cancellationToken: cancellationToken));

        _logger.LogInformation("Backup verified via RESTORE VERIFYONLY: {FilePath}", filePath);

        return filePath;
    }
}


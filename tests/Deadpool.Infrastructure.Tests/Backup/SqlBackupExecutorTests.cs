using Deadpool.Core.Configuration;
using Deadpool.Core.Domain.Entities;
using Deadpool.Core.Domain.Enums;
using Deadpool.Infrastructure.Backup;
using Deadpool.Infrastructure.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Deadpool.Infrastructure.Tests.Backup;

/// <summary>
/// Unit tests for SqlBackupExecutor.
/// Uses FakeDbConnection to intercept all Dapper calls — no SQL Server required.
///
/// Naming: MethodName_ShouldExpectedResult_WhenCondition
/// Pattern: Arrange / Act / Assert
/// </summary>
public sealed class SqlBackupExecutorTests
{
    // ── Fixtures ───────────���──────────────────────────────────────────────────

    private static readonly DatabaseProfile AnyProfile = new()
    {
        DatabaseId    = Guid.NewGuid(),
        DatabaseName  = "HospitalA",
        RecoveryModel = RecoveryModel.Full
    };

    private static IOptions<DeadpoolSettings> Settings => Options.Create(new DeadpoolSettings
    {
        LocalBackupRoot     = @"C:\Deadpool\Backups",
        SqlConnectionString = "Server=fake;Database=master;"
    });

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an executor wired to a FakeDbConnection.
    /// The fake captures all SQL commands Dapper sends without hitting SQL Server.
    /// </summary>
    private static (SqlBackupExecutor executor, FakeDbConnection fakeConn) BuildExecutor()
    {
        var fakeConn = new FakeDbConnection();

        var connectionFactory = new Mock<ISqlConnectionFactory>();
        connectionFactory
            .Setup(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeConn);

        var executor = new SqlBackupExecutor(
            connectionFactory.Object,
            Settings,
            NullLogger<SqlBackupExecutor>.Instance);

        return (executor, fakeConn);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Test cases
    // ═════════════════════════════════════════════════════════════════════════

    // ── 1. Full backup SQL content ────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ShouldSendFullBackupSqlWithCompressionChecksumInit_WhenBackupTypeIsFull()
    {
        // Arrange
        var (executor, fakeConn) = BuildExecutor();

        // Act
        await executor.ExecuteAsync(AnyProfile, BackupType.Full);

        // Assert — 2 commands: BACKUP then RESTORE VERIFYONLY
        fakeConn.ExecutedCommands.Should().HaveCount(2);

        var backupCmd = fakeConn.ExecutedCommands[0];
        backupCmd.CommandText.Should().Contain("BACKUP DATABASE [HospitalA]");
        backupCmd.CommandText.Should().Contain("COMPRESSION");
        backupCmd.CommandText.Should().Contain("CHECKSUM");
        backupCmd.CommandText.Should().Contain("INIT");
        backupCmd.CommandText.Should().NotContain("DIFFERENTIAL");
        backupCmd.Parameters.Should().ContainKey("FilePath");
    }

    // ── 2. Differential backup SQL content ─────────────────────────────��─────

    [Fact]
    public async Task ExecuteAsync_ShouldSendDifferentialKeywordInSql_WhenBackupTypeIsDifferential()
    {
        // Arrange
        var (executor, fakeConn) = BuildExecutor();

        // Act
        await executor.ExecuteAsync(AnyProfile, BackupType.Differential);

        // Assert
        var backupCmd = fakeConn.ExecutedCommands[0];
        backupCmd.CommandText.Should().Contain("BACKUP DATABASE [HospitalA]");
        backupCmd.CommandText.Should().Contain("DIFFERENTIAL");
        backupCmd.CommandText.Should().NotContain("INIT");
    }

    // ── 3. Transaction log backup SQL content ─────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ShouldSendBackupLogSqlWithoutCompression_WhenBackupTypeIsTransactionLog()
    {
        // Arrange
        var (executor, fakeConn) = BuildExecutor();

        // Act
        await executor.ExecuteAsync(AnyProfile, BackupType.TransactionLog);

        // Assert
        var backupCmd = fakeConn.ExecutedCommands[0];
        backupCmd.CommandText.Should().Contain("BACKUP LOG [HospitalA]");
        backupCmd.CommandText.Should().NotContain("BACKUP DATABASE");
        backupCmd.CommandText.Should().NotContain("COMPRESSION");
    }

    // ── 4. RESTORE VERIFYONLY runs after every backup type ────────────────────

    [Theory]
    [InlineData(BackupType.Full)]
    [InlineData(BackupType.Differential)]
    [InlineData(BackupType.TransactionLog)]
    public async Task ExecuteAsync_ShouldRunRestoreVerifyOnlyAsSecondCommand_AfterBackupSucceeds(
        BackupType backupType)
    {
        // Arrange
        var (executor, fakeConn) = BuildExecutor();

        // Act
        await executor.ExecuteAsync(AnyProfile, backupType);

        // Assert — second command must be RESTORE VERIFYONLY WITH CHECKSUM
        fakeConn.ExecutedCommands.Should().HaveCount(2);

        var verifyCmd = fakeConn.ExecutedCommands[1];
        verifyCmd.CommandText.Should().Contain("RESTORE VERIFYONLY");
        verifyCmd.CommandText.Should().Contain("CHECKSUM");
        verifyCmd.Parameters.Should().ContainKey("FilePath");
    }

    // ── 5. Returned file path follows naming convention ─────���─────────────────

    [Theory]
    [InlineData(BackupType.Full,           "FULL", ".bak")]
    [InlineData(BackupType.Differential,   "DIFF", ".bak")]
    [InlineData(BackupType.TransactionLog, "LOG",  ".trn")]
    public async Task ExecuteAsync_ShouldReturnPathMatchingNamingConvention_WhenBackupSucceeds(
        BackupType backupType, string expectedTypeCode, string expectedExtension)
    {
        // Arrange
        var (executor, _) = BuildExecutor();

        // Act
        var result = await executor.ExecuteAsync(AnyProfile, backupType);

        // Assert — DB_TYPE_yyyyMMdd_HHmmss.ext under LocalBackupRoot
        result.Should().StartWith(@"C:\Deadpool\Backups\");
        result.Should().Contain($"HospitalA_{expectedTypeCode}_");
        result.Should().EndWith(expectedExtension);
    }

    // ── 6. Same FilePath used in both backup and verify commands ──────────────

    [Fact]
    public async Task ExecuteAsync_ShouldPassIdenticalFilePath_ToBothBackupAndVerifyCommands()
    {
        // Arrange
        var (executor, fakeConn) = BuildExecutor();

        // Act
        await executor.ExecuteAsync(AnyProfile, BackupType.Full);

        // Assert — VERIFYONLY targets the exact same file as BACKUP
        var backupPath = fakeConn.ExecutedCommands[0].Parameters["FilePath"] as string;
        var verifyPath = fakeConn.ExecutedCommands[1].Parameters["FilePath"] as string;

        backupPath.Should().NotBeNullOrEmpty();
        verifyPath.Should().Be(backupPath);
    }

    // ── 7. Connection factory called exactly once per execution ─���─────────────

    [Fact]
    public async Task ExecuteAsync_ShouldOpenConnectionExactlyOnce_PerBackupExecution()
    {
        // Arrange
        var fakeConn = new FakeDbConnection();
        var connectionFactory = new Mock<ISqlConnectionFactory>();
        connectionFactory
            .Setup(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeConn);

        var executor = new SqlBackupExecutor(
            connectionFactory.Object,
            Settings,
            NullLogger<SqlBackupExecutor>.Instance);

        // Act
        await executor.ExecuteAsync(AnyProfile, BackupType.Full);

        // Assert — one connection reused for both BACKUP and VERIFYONLY
        connectionFactory.Verify(
            f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}


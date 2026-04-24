using Deadpool.Core.Domain.Enums;
using Deadpool.Infrastructure.Backup;
using FluentAssertions;

namespace Deadpool.Infrastructure.Tests.Backup;

/// <summary>
/// Pure SQL text verification — no database connection required.
/// BackupSqlBuilder is a static class; tests call it directly.
///
/// Naming: MethodName_ShouldExpectedResult_WhenCondition
/// Pattern: Arrange / Act / Assert
/// </summary>
public sealed class BackupSqlBuilderTests
{
    // ── Full backup ───────────────────────────────────────────────────────────

    [Fact]
    public void BuildBackupSql_ShouldContainCompressionChecksumAndInit_WhenBackupTypeIsFull()
    {
        // Arrange & Act
        var sql = BackupSqlBuilder.BuildBackupSql("HospitalA", BackupType.Full);

        // Assert
        sql.Should().Contain("BACKUP DATABASE [HospitalA]");
        sql.Should().Contain("@FilePath");
        sql.Should().Contain("COMPRESSION");
        sql.Should().Contain("CHECKSUM");
        sql.Should().Contain("INIT");
        sql.Should().NotContain("DIFFERENTIAL");
        sql.Should().NotContain("BACKUP LOG");
    }

    // ── Differential backup ───────────────────────────────────────────────────

    [Fact]
    public void BuildBackupSql_ShouldContainDifferentialKeyword_WhenBackupTypeIsDifferential()
    {
        // Arrange & Act
        var sql = BackupSqlBuilder.BuildBackupSql("HospitalA", BackupType.Differential);

        // Assert
        sql.Should().Contain("BACKUP DATABASE [HospitalA]");
        sql.Should().Contain("@FilePath");
        sql.Should().Contain("DIFFERENTIAL");
        sql.Should().Contain("COMPRESSION");
        sql.Should().Contain("CHECKSUM");
        sql.Should().NotContain("INIT");
        sql.Should().NotContain("BACKUP LOG");
    }

    // ── Transaction log backup ────────────────────────────────────────────────

    [Fact]
    public void BuildBackupSql_ShouldUseBackupLogWithoutCompression_WhenBackupTypeIsTransactionLog()
    {
        // Arrange & Act
        var sql = BackupSqlBuilder.BuildBackupSql("HospitalA", BackupType.TransactionLog);

        // Assert
        sql.Should().Contain("BACKUP LOG [HospitalA]");
        sql.Should().Contain("@FilePath");
        sql.Should().Contain("CHECKSUM");
        sql.Should().NotContain("BACKUP DATABASE");
        sql.Should().NotContain("COMPRESSION");
        sql.Should().NotContain("DIFFERENTIAL");
        sql.Should().NotContain("INIT");
    }

    // ── RESTORE VERIFYONLY ────────────────────────────────────────────────────

    [Fact]
    public void BuildVerifyOnlySql_ShouldContainRestoreVerifyonlyAndChecksum()
    {
        // Arrange & Act
        var sql = BackupSqlBuilder.BuildVerifyOnlySql();

        // Assert
        sql.Should().Contain("RESTORE VERIFYONLY");
        sql.Should().Contain("@FilePath");
        sql.Should().Contain("CHECKSUM");
        sql.Should().NotContain("BACKUP");
    }

    // ── Database name injection (bracket-safe) ────────────────────────────────

    [Theory]
    [InlineData("HospitalA")]
    [InlineData("My Database")]          // spaces handled by brackets
    [InlineData("Hospital-Site2")]
    public void BuildBackupSql_ShouldWrapDatabaseNameInBrackets_ForAllBackupTypes(string dbName)
    {
        // Arrange & Act
        var fullSql = BackupSqlBuilder.BuildBackupSql(dbName, BackupType.Full);
        var diffSql = BackupSqlBuilder.BuildBackupSql(dbName, BackupType.Differential);
        var logSql  = BackupSqlBuilder.BuildBackupSql(dbName, BackupType.TransactionLog);

        // Assert — brackets prevent SQL injection / name collision
        fullSql.Should().Contain($"[{dbName}]");
        diffSql.Should().Contain($"[{dbName}]");
        logSql .Should().Contain($"[{dbName}]");
    }

    // ── Unknown backup type guard ─────────────────────────────────────────────

    [Fact]
    public void BuildBackupSql_ShouldThrowArgumentOutOfRange_WhenBackupTypeIsUnknown()
    {
        // Arrange & Act
        var act = () => BackupSqlBuilder.BuildBackupSql("HospitalA", (BackupType)99);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("backupType");
    }
}


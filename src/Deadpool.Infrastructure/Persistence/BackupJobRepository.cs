using Deadpool.Core.Domain.Entities;
using Deadpool.Core.Domain.Enums;
using Deadpool.Core.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Deadpool.Infrastructure.Persistence;

/// <summary>
/// SQLite implementation of IBackupJobRepository using Dapper.
/// Phase-1 skeleton — SQL DDL and queries pending.
/// </summary>
internal sealed class BackupJobRepository : IBackupJobRepository
{
    private readonly string _connectionString;

    public BackupJobRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DeadpoolDb")
            ?? "Data Source=deadpool.db";
    }

    private SqliteConnection CreateConnection() => new(_connectionString);

    public async Task<BackupJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        // TODO: SELECT * FROM BackupJob WHERE JobId = @jobId
        await using var conn = CreateConnection();
        throw new NotImplementedException();
    }

    public async Task<BackupJob?> GetActiveJobAsync(Guid databaseId, CancellationToken cancellationToken = default)
    {
        // TODO: SELECT * FROM BackupJob
        //       WHERE DatabaseId = @databaseId
        //         AND State NOT IN ('Success', 'Failed')
        //       LIMIT 1
        await using var conn = CreateConnection();
        throw new NotImplementedException();
    }

    public async Task AddAsync(BackupJob job, CancellationToken cancellationToken = default)
    {
        // TODO: INSERT INTO BackupJob ...
        await using var conn = CreateConnection();
        throw new NotImplementedException();
    }

    public async Task UpdateStateAsync(
        Guid jobId,
        BackupJobState newState,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: UPDATE BackupJob SET State = @newState, ErrorMessage = @errorMessage,
        //       CompletedAt = (set if terminal state) WHERE JobId = @jobId
        await using var conn = CreateConnection();
        throw new NotImplementedException();
    }
}


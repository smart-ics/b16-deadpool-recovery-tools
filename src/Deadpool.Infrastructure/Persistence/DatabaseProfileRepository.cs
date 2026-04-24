using Deadpool.Core.Domain.Entities;
using Deadpool.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Deadpool.Infrastructure.Persistence;

/// <summary>
/// SQLite implementation of IDatabaseProfileRepository using Dapper.
/// Phase-1: Returns empty collection (no schema yet).
/// Phase-2: Will implement full CRUD + DDL migrations.
/// </summary>
internal sealed class DatabaseProfileRepository : IDatabaseProfileRepository
{
    private readonly ILogger<DatabaseProfileRepository> _logger;

    public DatabaseProfileRepository(ILogger<DatabaseProfileRepository> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<DatabaseProfile>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        // TODO Phase-2: SELECT * FROM DatabaseProfile WHERE IsActive = 1
        _logger.LogWarning("DatabaseProfile repository is not yet implemented. Returning empty collection.");
        return Task.FromResult(Enumerable.Empty<DatabaseProfile>());
    }

    public Task<DatabaseProfile?> GetByIdAsync(Guid databaseId, CancellationToken cancellationToken = default)
    {
        // TODO Phase-2: SELECT * FROM DatabaseProfile WHERE DatabaseId = @databaseId
        _logger.LogWarning("DatabaseProfile repository is not yet implemented. Returning null for {DatabaseId}", databaseId);
        return Task.FromResult<DatabaseProfile?>(null);
    }
}


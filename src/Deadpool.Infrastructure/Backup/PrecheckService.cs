using Deadpool.Core.Domain.Entities;
using Deadpool.Core.Domain.Enums;
using Deadpool.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Deadpool.Infrastructure.Backup;

/// <summary>
/// Validates preconditions before a backup runs.
/// Phase-1 skeleton — full implementation pending.
/// Checks: database online, disk space, backup path writable,
///         full backup exists (for diff), FULL recovery model (for log).
/// </summary>
internal sealed class PrecheckService : IPrecheckService
{
    private readonly ILogger<PrecheckService> _logger;

    public PrecheckService(ILogger<PrecheckService> logger)
    {
        _logger = logger;
    }

    public Task<PrecheckResult> RunAsync(
        DatabaseProfile profile,
        BackupType backupType,
        CancellationToken cancellationToken = default)
    {
        // TODO: implement each check below
        // 1. Database is online
        // 2. Sufficient disk space on local backup path
        // 3. Backup path is writable
        // 4. Full backup exists (required for Differential)
        // 5. Recovery model is FULL (required for TransactionLog)

        throw new NotImplementedException("Precheck service is not yet implemented.");
    }
}


using Deadpool.Core.Domain.Entities;
using Deadpool.Core.Domain.Enums;

namespace Deadpool.Core.Interfaces;

/// <summary>
/// Validates preconditions before a backup job is allowed to run.
/// Implementation lives in Deadpool.Infrastructure.
/// </summary>
public interface IPrecheckService
{
    /// <summary>
    /// Runs all relevant prechecks for the given database and backup type.
    /// Returns a result describing whether all checks passed and any failure reasons.
    /// </summary>
    Task<PrecheckResult> RunAsync(DatabaseProfile profile, BackupType backupType, CancellationToken cancellationToken = default);
}

/// <summary>Outcome of running prechecks before a backup.</summary>
public sealed class PrecheckResult
{
    public bool Passed { get; init; }
    public IReadOnlyList<string> Failures { get; init; } = [];

    public static PrecheckResult Success() => new() { Passed = true };
    public static PrecheckResult Fail(params string[] reasons) => new() { Passed = false, Failures = reasons };
}


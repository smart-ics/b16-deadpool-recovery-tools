using Deadpool.Core.Interfaces;
using Deadpool.Core.Workflow;
using Deadpool.Infrastructure.Backup;
using Deadpool.Infrastructure.Persistence;
using Deadpool.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Deadpool.Infrastructure;

/// <summary>
/// Extension methods to register all Infrastructure services into the DI container.
/// Called from Deadpool.Cli Program.cs.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDeadpoolInfrastructure(this IServiceCollection services)
    {
        // Persistence (SQLite + Dapper)
        services.AddScoped<IBackupCatalogRepository, BackupCatalogRepository>();
        services.AddScoped<IBackupJobRepository, BackupJobRepository>();

        // Backup execution
        services.AddScoped<IBackupExecutor, SqlBackupExecutor>();
        services.AddScoped<IPrecheckService, PrecheckService>();

        // Storage
        services.AddScoped<IFileCopyService, FileCopyService>();

        // Workflow orchestrator
        services.AddScoped<BackupWorkflow>();

        return services;
    }
}


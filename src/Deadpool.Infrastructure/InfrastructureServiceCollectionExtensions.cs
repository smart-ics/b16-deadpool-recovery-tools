using Deadpool.Core.Configuration;
using Deadpool.Core.Interfaces;
using Deadpool.Core.Workflow;
using Deadpool.Infrastructure.Backup;
using Deadpool.Infrastructure.Persistence;
using Deadpool.Infrastructure.Scheduling;
using Deadpool.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Deadpool.Infrastructure;

/// <summary>
/// Extension methods to register all Infrastructure services into the DI container.
/// Called from Deadpool.Cli Program.cs.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDeadpoolInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Settings
        services.Configure<DeadpoolSettings>(configuration.GetSection(DeadpoolSettings.SectionName));

        // SQL connection factory
        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();

        // Filesystem abstraction
        services.AddSingleton<IFileSystem, OsFileSystem>();

        // Persistence (SQLite + Dapper)
        services.AddScoped<IBackupCatalogRepository, BackupCatalogRepository>();
        services.AddScoped<IBackupJobRepository, BackupJobRepository>();
        services.AddScoped<IDatabaseProfileRepository, DatabaseProfileRepository>();

        // Backup execution
        services.AddScoped<IBackupExecutor, SqlBackupExecutor>();
        services.AddScoped<IPrecheckService, PrecheckService>();

        // Storage
        services.AddScoped<IFileCopyService, FileCopyService>();

        // Workflow orchestrator
        services.AddScoped<BackupWorkflow>();

        // Scheduling
        services.AddSingleton<IJobSchedulerService, JobSchedulerService>();

        return services;
    }
}


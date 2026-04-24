using Deadpool.Infrastructure;
using Quartz;
using Serilog;

// Bootstrap Serilog early so startup errors are captured
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Deadpool Backup Tools starting...");

    var builder = Host.CreateApplicationBuilder(args);

    // ── Serilog ──────────────────────────────────────────────────────────────
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/deadpool-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

    // ── Infrastructure (repositories, executors, workflow) ───────────────────
    builder.Services.AddDeadpoolInfrastructure();

    // ── Quartz scheduler ─────────────────────────────────────────────────────
    builder.Services.AddQuartz(q =>
    {
        // TODO: register backup Quartz jobs per DatabaseProfile loaded from catalog
        // Example:
        //   q.AddJob<FullBackupJob>(j => j.WithIdentity("full-HospitalA"));
        //   q.AddTrigger(t => t.ForJob("full-HospitalA")
        //       .WithCronSchedule("0 0 2 ? * SUN")
        //       .WithMisfireHandlingInstructionFireAndProceed());
    });
    builder.Services.AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true);

    // ── Windows Service / console dual-mode ──────────────────────────────────
    builder.Services.AddWindowsService(options => options.ServiceName = "Deadpool Backup Tools");
    builder.Services.AddHostedService<DeadpoolWorker>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Deadpool host terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;

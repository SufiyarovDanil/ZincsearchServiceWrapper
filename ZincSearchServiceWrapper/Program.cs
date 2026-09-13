using Serilog;
using Serilog.Events;
using ZincSearchServiceWrapper;

var builder = Host.CreateDefaultBuilder(args);
builder.UseWindowsService();
builder.UseSerilog((ctx, services, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .MinimumLevel.Override("System", LogEventLevel.Warning)

        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ProcessWrapperService")
        .Enrich.WithProperty("MachineName", Environment.MachineName)

        .WriteTo.Logger((cl) =>
        {
            cl.Filter.ByExcluding(e => e.Level < LogEventLevel.Error)
            .WriteTo.File(
                path: Path.Combine(AppContext.BaseDirectory, "logs/LogError.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
        })
        .WriteTo.Logger(cl =>
        {
            cl.Filter.ByIncludingOnly(e => e.Level < LogEventLevel.Error)
            .WriteTo.File(
                path: Path.Combine(AppContext.BaseDirectory, "logs/LogInfo.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
        });
});
builder.ConfigureServices((ctx, services) =>
{
    services.Configure<ZincSearchSettings>(ctx.Configuration.GetSection(nameof(ZincSearchSettings)));
    services.AddHostedService<ZincSearchServiceWorker>();
});

var host = builder.Build();
host.Run();

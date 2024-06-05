using DSharpPlus;
using DSharpPlus.Extensions;
using Lilia;
using Lilia.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

// what a boilerplate
Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("SourceContext", "Lilia")
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}")
#if DEBUG
    .MinimumLevel.Debug()
#else
    .MinimumLevel.Information()
#endif
    .CreateLogger();

ClientConfiguration clientConfig = new();

await Host
    .CreateDefaultBuilder()
#if DEBUG
    .UseEnvironment("Testing")
#else
    .UseEnvironment("Production")
#endif
    .UseSerilog()
    .UseConsoleLifetime()
    .ConfigureHostConfiguration(x =>
    {
        x.AddEnvironmentVariables();
        x.AddJsonFile("config.json", reloadOnChange: true, optional: false);
    })
    .ConfigureServices((hostBuilderCtx, services) =>
    {
        // easier to retrieve stuffs I guess?
        services.AddSingleton(services);

        // Just Serilog.
        // more like Doki Doki Logging Club.
        services.AddLogging(logging => logging.ClearProviders().AddSerilog());

        // what?
        hostBuilderCtx.Configuration.GetSection(nameof(ClientConfiguration)).Bind(clientConfig);

        // yes.
        services.AddHostedService<LiliaService>()
            .AddDiscordClient(clientConfig.Token, DiscordIntents.AllUnprivileged);

        // the database
        services.AddDbContext<DatabaseService>();
    })
    .RunConsoleAsync();

await Log.CloseAndFlushAsync();

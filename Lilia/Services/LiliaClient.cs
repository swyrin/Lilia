using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Lilia.Commons;
using Lilia.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Serilog;

namespace Lilia.Services;

public class LiliaClient
{
    public CancellationTokenSource Cts;
    public LiliaDatabase Database;
    public JsonConfigurations Configurations;
    private LavalinkExtension _lavalinkExtension;

    public async Task Run()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Information()
            .CreateLogger();
            
        Log.Logger.Information("Loading configurations");
        JsonConfigurationsManager.EnsureConfigFileGenerated();
        this.Configurations = JsonConfigurationsManager.Configurations;
            
        this.Cts = new CancellationTokenSource();
            
        DiscordClient client = new(new DiscordConfiguration
        {
            Token = this.Configurations.Credentials.DiscordToken,
            TokenType = TokenType.Bot,
            MinimumLogLevel = LogLevel.Debug,
            LoggerFactory = new LoggerFactory().AddSerilog()
        });
            
        Log.Logger.Information("Setting up databases");
        this.Database = new LiliaDatabase();
            
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(this)
            .AddSingleton(Log.Logger)
            .BuildServiceProvider();

        CommandsNextExtension commandsNext = client.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = this.Configurations.Client.StringPrefixes,
            Services = services
        });

        this._lavalinkExtension = client.UseLavalink();

        commandsNext.RegisterCommands(Assembly.GetExecutingAssembly());
        commandsNext.SetHelpFormatter<HelpCommandFormatter>();

        client.Ready += this.OnReady;
        client.GuildAvailable += this.OnGuildAvailable;
        client.ClientErrored += this.OnClientErrored;

        commandsNext.CommandErrored += this.OnCommandsNextCommandErrored;

        await client.ConnectAsync();
        await Task.Delay(-1);

        while (!Cts.IsCancellationRequested) await Task.Delay(2000);

        await client.DisconnectAsync();
    }

    private async Task OnReady(DiscordClient sender, ReadyEventArgs e)
    {
        ClientActivityData activityData = this.Configurations.Client.Activity;

        DiscordActivity activity = new DiscordActivity
        {
            ActivityType = (ActivityType)activityData.Type,
            Name = activityData.Name
        };

        ConnectionEndpoint endpoint = new ConnectionEndpoint
        {
            Hostname = this.Configurations.Lavalink.Hostname,
            Port = this.Configurations.Lavalink.Port
        };
            
        await this._lavalinkExtension.ConnectAsync(new LavalinkConfiguration
        {
            Password = this.Configurations.Lavalink.Password,
            RestEndpoint = endpoint,
            SocketEndpoint = endpoint
        });

        await sender.UpdateStatusAsync(activity, (UserStatus)activityData.Status);
        Log.Logger.Information("Client is ready to serve.");
    }

    private Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        Log.Logger.Debug($"Guild cached : {e.Guild.Name}");
        return Task.CompletedTask;
    }

    private Task OnClientErrored(DiscordClient sender, ClientErrorEventArgs e)
    {
        Log.Logger.Fatal(e.Exception, "An exception occured when running bot");
        return Task.CompletedTask;
    }

    private Task OnCommandsNextCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
    {
        Log.Logger.Fatal(e.Exception, "An exception occured when running a command");
        return Task.CompletedTask;
    }
}
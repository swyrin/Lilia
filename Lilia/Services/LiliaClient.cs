using System;
using System.Linq;
using System.Reflection;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Lilia.Commons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using OsuSharp;
using OsuSharp.Extensions;
using Serilog;

namespace Lilia.Services;

public class LiliaClient
{
    public CancellationTokenSource Cts;
    public LiliaDatabase Database;
    public JsonConfigurations Configurations;
    public DiscordClient Client;
    public ServiceProvider Services;
    public DateTime StartTime;

    private LavalinkExtension _lavalinkExtension;
    private SlashCommandsExtension _slashCommandsExtension;

    public async Task Run()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Debug()
            .CreateLogger();

        Log.Logger.Information("Loading configurations");
        this.Configurations = JsonConfigurationsManager.Configurations;

        this.Cts = new CancellationTokenSource();

        this.Client = new DiscordClient(new DiscordConfiguration
        {
            Token = this.Configurations.Credentials.DiscordToken,
            TokenType = TokenType.Bot,
            LoggerFactory = new LoggerFactory().AddSerilog()
        });

        Log.Logger.Information("Setting up databases");
        this.Database = new LiliaDatabase();

        OsuData osuData = this.Configurations.Credentials.Osu;

        this.Services = new ServiceCollection()
            .AddLogging(x => x.AddSerilog())
            .AddDefaultSerializer()
            .AddDefaultRequestHandler()
            .AddOsuSharp(x => x.Configuration = new OsuClientConfiguration
            {
                ModFormatSeparator = string.Empty,
                ClientId = this.Configurations.Credentials.Osu.ClientId,
                ClientSecret = this.Configurations.Credentials.Osu.ClientSecret
            })
            .AddSingleton(this)
            .BuildServiceProvider();

        this._lavalinkExtension = this.Client.UseLavalink();

        this._slashCommandsExtension = this.Client.UseSlashCommands(new SlashCommandsConfiguration
        {
            Services = this.Services
        });

        this.Client.UseInteractivity(new InteractivityConfiguration
        {
            AckPaginationButtons = true,
            ResponseBehavior = InteractionResponseBehavior.Ack,
            Timeout = TimeSpan.FromSeconds(30)
        });

        if (this.Configurations.Client.PrivateGuildIds.Any())
        {
            this.Configurations.Client.PrivateGuildIds.ForEach(guildId =>
            {
                Log.Logger.Warning($"Registering slash commands for private guild with ID \"{guildId}\"");
                this._slashCommandsExtension.RegisterCommands(Assembly.GetExecutingAssembly(), guildId);    
            });
        }
        else
        {
            Log.Logger.Warning("Registering slash commands in global scope");
            this._slashCommandsExtension.RegisterCommands(Assembly.GetExecutingAssembly());
        }
        
        this.Client.Ready += this.OnReady;
        this.Client.GuildAvailable += this.OnGuildAvailable;
        this.Client.ClientErrored += this.OnClientErrored;
        
        this._slashCommandsExtension.SlashCommandErrored += this.OnSlashCommandErrored;

        await this.Client.ConnectAsync();

        while (!Cts.IsCancellationRequested) await Task.Delay(200);

        await this.Client.DisconnectAsync();
        await this.Database.GetContext().DisposeAsync();
    }

    private async Task OnReady(DiscordClient sender, ReadyEventArgs e)
    {
        ClientActivityData activityData = this.Configurations.Client.Activity;

        bool canConvertActivityType = Enum.TryParse(activityData.Type, out ActivityType activityType);
        bool canConvertStatus = Enum.TryParse(activityData.Status, out UserStatus userStatus);

        if (!canConvertActivityType)
        {
            Log.Logger.Warning($"Can not convert \"{activityData.Type}\" to a valid activity type, using \"Playing\"");
            Log.Logger.Information("Valid options are: ListeningTo, Competing, Playing, Watching");
            activityType = ActivityType.Playing;
        }

        if (!canConvertStatus)
        {
            Log.Logger.Warning($"Can not convert \"{activityData.Status}\" to a valid status, using \"Online\"");
            Log.Logger.Information("Valid options are: Online, Invisible, Idle, DoNotDisturb");
            userStatus = UserStatus.Online;
        }

        DiscordActivity activity = new DiscordActivity
        {
            ActivityType = activityType,
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

        await sender.UpdateStatusAsync(activity, userStatus);
        Log.Logger.Information("Client is ready");
        
        this.StartTime = DateTime.Now;
    }

    private Task OnGuildAvailable(DiscordClient _, GuildCreateEventArgs e)
    {
        Log.Logger.Information($"Guild cached: {e.Guild.Name} ({e.Guild.Id})");
        return Task.CompletedTask;
    }

    private Task OnClientErrored(DiscordClient _, ClientErrorEventArgs e)
    {
        Log.Logger.Fatal(e.Exception, "An exception occured when running the bot");
        throw e.Exception;
    }

    private Task OnSlashCommandErrored(SlashCommandsExtension _, SlashCommandErrorEventArgs e)
    {
        Log.Logger.Fatal(e.Exception, "An exception occured when executing a slash command");
        throw e.Exception;
    }
}
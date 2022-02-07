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
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using Lilia.Database;
using Lilia.Json;
using OsuSharp;
using OsuSharp.Extensions;
using Serilog;

namespace Lilia.Services;

public class LiliaClient
{
    public CancellationTokenSource Cts;
    public BotConfiguration BotConfiguration;
    public LiliaDatabase Database;
    public ServiceProvider Services;
    public DateTime StartTime;

    private SlashCommandsExtension _slashCommandsExtension;

    public async Task Run()
    {
        Log.Logger.Information("Loading configurations");
        this.BotConfiguration = JsonManager<BotConfiguration>.Read();

        this.Cts = new CancellationTokenSource();

        DiscordClient client = new DiscordClient(new DiscordConfiguration
        {
            Token = this.BotConfiguration.Credentials.DiscordToken,
            TokenType = TokenType.Bot,
            LoggerFactory = new LoggerFactory().AddSerilog()
        });

        Log.Logger.Information("Setting up databases");
        this.Database = new LiliaDatabase();

        OsuData osuData = this.BotConfiguration.Credentials.Osu;

        this.Services = new ServiceCollection()
            .AddLogging(x => x.AddSerilog())
            .AddDefaultSerializer()
            .AddDefaultRequestHandler()
            .AddOsuSharp(x => x.Configuration = new OsuClientConfiguration
            {
                ModFormatSeparator = string.Empty,
                ClientId = this.BotConfiguration.Credentials.Osu.ClientId,
                ClientSecret = this.BotConfiguration.Credentials.Osu.ClientSecret
            })
            .AddSingleton(this)
            .BuildServiceProvider();

        this._slashCommandsExtension = client.UseSlashCommands(new SlashCommandsConfiguration
        {
            Services = this.Services
        });

        client.UseInteractivity(new InteractivityConfiguration
        {
            AckPaginationButtons = true,
            ResponseBehavior = InteractionResponseBehavior.Ack,
            Timeout = TimeSpan.FromSeconds(30)
        });

        if (this.BotConfiguration.Client.PrivateGuildIds.Any())
        {
            this.BotConfiguration.Client.PrivateGuildIds.ForEach(guildId =>
            {
                Log.Logger.Warning($"Registering slash commands for private guild with ID \"{guildId}\"");
                this._slashCommandsExtension.RegisterCommands(Assembly.GetExecutingAssembly(), guildId);    
            });
        }
        
        if (this.BotConfiguration.Client.SlashCommandsForGlobal)
        {
            Log.Logger.Warning("Registering slash commands in global scope");
            this._slashCommandsExtension.RegisterCommands(Assembly.GetExecutingAssembly());
        }
        
        client.Ready += this.OnReady;
        client.GuildAvailable += this.OnGuildAvailable;
        client.ClientErrored += this.OnClientErrored;
        
        this._slashCommandsExtension.SlashCommandErrored += this.OnSlashCommandErrored;

        await client.ConnectAsync();

        while (!Cts.IsCancellationRequested) await Task.Delay(200);

        await client.DisconnectAsync();
        await this.Database.GetContext().DisposeAsync();
    }

    private async Task OnReady(DiscordClient sender, ReadyEventArgs e)
    {
        ClientActivityData activityData = this.BotConfiguration.Client.Activity;
        
        Log.Logger.Information("Setting client activity");
        
        if (!Enum.TryParse(activityData.Type, out ActivityType activityType))
        {
            Log.Logger.Warning($"Can not convert \"{activityData.Type}\" to a valid activity type, using \"Playing\"");
            Log.Logger.Information("Valid options are: ListeningTo, Competing, Playing, Watching");
            activityType = ActivityType.Playing;
        }

        if (!Enum.TryParse(activityData.Status, out UserStatus userStatus))
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

        await sender.UpdateStatusAsync(activity, userStatus);
        
        Log.Logger.Information("Client is ready");
        this.StartTime = DateTime.Now;
    }

    private Task OnGuildAvailable(DiscordClient _, GuildCreateEventArgs e)
    {
        Log.Logger.Information($"Guild cached: {e.Guild.Name} (ID: {e.Guild.Id})");
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
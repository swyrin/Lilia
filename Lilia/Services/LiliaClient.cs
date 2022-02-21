using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using Lilia.Commons;
using Lilia.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OsuSharp;
using OsuSharp.Extensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Lavalink4NET;
using Lavalink4NET.Artwork;
using Lavalink4NET.DSharpPlus;
using Lavalink4NET.Tracking;
using Lilia.Database;
using Lilia.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog.Extensions.Logging;

namespace Lilia.Services;

public class LiliaClient
{
    public LavalinkNode Lavalink;
    public InactivityTrackingService InactivityTracker;
    public ArtworkService ArtworkService;
    
    public static readonly BotConfiguration BotConfiguration;
    public static readonly CancellationTokenSource Cts;
    public LiliaDatabase Database;
    public static readonly List<DiscordGuild> JoinedGuilds = new();
    public DateTime StartTime;
    public static readonly DbContextOptionsBuilder<LiliaDatabaseContext> OptionsBuilder = new();

    public const Permissions RequiredPermissions = Permissions.ViewAuditLog | Permissions.ManageRoles |
                                                   Permissions.ManageChannels | Permissions.KickMembers |
                                                   Permissions.BanMembers | Permissions.AccessChannels |
                                                   Permissions.ModerateMembers | Permissions.SendMessages |
                                                   Permissions.SendMessagesInThreads | Permissions.EmbedLinks |
                                                   Permissions.AttachFiles | Permissions.ReadMessageHistory |
                                                   Permissions.UseExternalEmojis | Permissions.UseExternalStickers |
                                                   Permissions.AddReactions | Permissions.UseApplicationCommands |
                                                   Permissions.UseVoice | Permissions.Speak |
                                                   Permissions.UseVoiceDetection | Permissions.StartEmbeddedActivities;

    public LiliaClient()
    {
        Log.Logger.Information("Setting up databases");
        Database = new LiliaDatabase();
    }
    
    static LiliaClient()
    {
        BotConfiguration = JsonManager<BotConfiguration>.Read();
        Cts = new CancellationTokenSource();
        
        var sqlConfig = BotConfiguration.Credentials.PostgreSql; 
        
        var connStrBuilder = new NpgsqlConnectionStringBuilder
        {
            Password = sqlConfig.Password,
            Username = sqlConfig.Username,
            Database = sqlConfig.DatabaseName,
            Host = sqlConfig.Host,
            Port = sqlConfig.Port,
            IncludeErrorDetail = true
        };

        OptionsBuilder
#if DEBUG
            .EnableSensitiveDataLogging()
#endif
            .EnableDetailedErrors()
            .UseLoggerFactory(new SerilogLoggerFactory(Log.Logger))
            .UseNpgsql(connStrBuilder.ToString());
    }

    private static async Task CheckForUpdateAsync()
    {
        using HttpClient client = new();

        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        var sourceVersionStr = await client.GetStringAsync("https://raw.githubusercontent.com/Lilia-Workshop/Lilia/master/version.txt");
        var sourceVersion = Version.Parse(sourceVersionStr);
        
        Log.Logger.Debug($"Current version: {currentVersion}  - Latest version: {sourceVersion}");

        if (currentVersion < sourceVersion)
        {
            Log.Logger.Warning("You need to update your bot because there are changes in the code");
            Log.Logger.Warning("Source code can be seen here: https://github.com/Lilia-Workshop/Lilia");
            Log.Logger.Warning("Changelogs in case you miss: https://github.com/Lilia-Workshop/Lilia/releases");
        }
        else
        {
            Log.Logger.Information("You are using latest version, hooray");
        }
    }
    
    public async Task RunAsync()
    {
        Log.Logger.Information("Checking for updates");
        
        await CheckForUpdateAsync();
        
        Log.Logger.Information("Setting up client");

        var client = new DiscordClient(new DiscordConfiguration
        {
            Token = BotConfiguration.Client.Token,
            TokenType = TokenType.Bot,
            LoggerFactory = new LoggerFactory().AddSerilog(),
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers
        });

        var services = new ServiceCollection()
            .AddLogging(x => x.AddSerilog())
            .AddDefaultSerializer()
            .AddDefaultRequestHandler()
            .AddSingleton(Database)
            .AddOsuSharp(x => x.Configuration = new OsuClientConfiguration
            {
                ModFormatSeparator = string.Empty,
                ClientId = BotConfiguration.Credentials.Osu.ClientId,
                ClientSecret = BotConfiguration.Credentials.Osu.ClientSecret
            })
            .AddSingleton(this)
            .BuildServiceProvider();

        var slash = client.UseSlashCommands(new SlashCommandsConfiguration
        {
            Services = services
        });

        client.UseInteractivity(new InteractivityConfiguration
        {
            AckPaginationButtons = true,
            ResponseBehavior = InteractionResponseBehavior.Ack,
            Timeout = TimeSpan.FromSeconds(30)
        });

        var lavalinkConfig = BotConfiguration.Credentials.Lavalink;
        Lavalink = new LavalinkNode(new LavalinkNodeOptions
            {
                RestUri = $"http://{lavalinkConfig.Host}:{lavalinkConfig.Port}",
                WebSocketUri = $"ws://{lavalinkConfig.Host}:{lavalinkConfig.Port}",
                Password = lavalinkConfig.Password,
#if DEBUG
                DebugPayloads = true,
#endif
                DisconnectOnStop = false
            }, new DiscordClientWrapper(client),
            new LavalinkLogger(new SerilogLoggerFactory(Log.Logger).CreateLogger("Lavalink")));

        if (BotConfiguration.Client.PrivateGuildIds.Count > 0)
        {
            BotConfiguration.Client.PrivateGuildIds.ForEach(guildId =>
            {
                Log.Logger.Warning($"Registering slash commands for guild with ID \"{guildId}\"");
                slash.RegisterCommands(Assembly.GetExecutingAssembly(), guildId);
            });
        }
        
        if (BotConfiguration.Client.SlashCommandsForGlobal)
        {
            Log.Logger.Warning("Registering slash commands in global scope");
            slash.RegisterCommands(Assembly.GetExecutingAssembly());
        }

        Log.Logger.Information("Registering event handlers");
        
        client.Ready += OnReady;
        client.GuildAvailable += OnGuildAvailable;
        client.GuildUnavailable += OnGuildUnavailable;
        client.GuildCreated += OnGuildAvailable;
        client.GuildDeleted += OnGuildUnavailable;
        client.GuildMemberAdded += OnGuildMemberAdd;
        client.GuildMemberRemoved += OnGuildMemberRemoved;
        
        client.ClientErrored += OnClientErrored;

        slash.SlashCommandErrored += OnSlashCommandErrored;

        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        
        Log.Logger.Information("Setting client activity");

        #region Activity Setup

        var activityData = BotConfiguration.Client.Activity;

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

        var activity = new DiscordActivity
        {
            ActivityType = activityType,
            Name = activityData.Name
        };

        #endregion Activity Setup

        Log.Logger.Information("Connecting and waiting for shutdown");
        await client.ConnectAsync(activity, userStatus);

        while (!Cts.IsCancellationRequested) await Task.Delay(200);

        await Lavalink.CloseAsync(WebSocketCloseStatus.Empty, "Client shutdown");
        await Lavalink.DisposeAsync();
        await client.DisconnectAsync();
        await Database.GetContext().DisposeAsync();
    }

    private void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
        Cts.Cancel();
    }

    private Task OnReady(DiscordClient sender, ReadyEventArgs e)
    {
        return Task.Run(() =>
        {
            Log.Logger.Information("Initializing Lavalink connection");

            ArtworkService = new ArtworkService();
            InactivityTracker = new InactivityTrackingService(Lavalink, new DiscordClientWrapper(sender),
                new InactivityTrackingOptions
                {
                    PollInterval = TimeSpan.FromSeconds(180),
                    DisconnectDelay = TimeSpan.FromSeconds(5),
                    TrackInactivity = true
                },
                new LavalinkLogger(new SerilogLoggerFactory(Log.Logger).CreateLogger("LavalinkInactivityTracker")));
            
            Lavalink.InitializeAsync();
            
            Log.Logger.Information($"Client is ready to serve as {sender.CurrentUser.Username}#{sender.CurrentUser.Discriminator}");
            StartTime = DateTime.Now;
            return Task.CompletedTask;
        });
    }

    private Task OnGuildAvailable(DiscordClient _, GuildCreateEventArgs e)
    {
        Log.Logger.Debug($"Guild cache added: {e.Guild.Name} (ID: {e.Guild.Id})");
        JoinedGuilds.Add(e.Guild);
        return Task.CompletedTask;
    }

    private Task OnGuildUnavailable(DiscordClient _, GuildDeleteEventArgs e)
    {
        Log.Logger.Debug($"Guild cache removed: {e.Guild.Name} (ID: {e.Guild.Id})");
        JoinedGuilds.Remove(e.Guild);
        return Task.CompletedTask;
    }

    private Task OnGuildMemberAdd(DiscordClient _, GuildMemberAddEventArgs e)
    {
        return Task.Run(() =>
        {
            var dbGuild = Database.GetContext().GetGuildRecord(e.Guild);
            
            if (!dbGuild.IsWelcomeEnabled) return;
            if (dbGuild.WelcomeChannelId == 0) return;
            if (string.IsNullOrWhiteSpace(dbGuild.WelcomeMessage)) return;

            var chn = e.Guild.GetChannel(dbGuild.WelcomeChannelId);

            var postProcessedMessage = dbGuild.WelcomeMessage
                .Replace("{name}", e.Member.Username)
                .Replace("{tag}", e.Member.Discriminator)
                .Replace("{@user}", Formatter.Mention(e.Member))
                .Replace("{guild}", e.Guild.Name);
            
            chn.SendMessageAsync(postProcessedMessage).ConfigureAwait(false).GetAwaiter().GetResult();
        });
    }

    private Task OnGuildMemberRemoved(DiscordClient _, GuildMemberRemoveEventArgs e)
    {
        return Task.Run(() =>
        {
            var dbGuild = Database.GetContext().GetGuildRecord(e.Guild);
            
            if (!dbGuild.IsGoodbyeEnabled) return;
            if (dbGuild.GoodbyeChannelId == 0) return;
            if (string.IsNullOrWhiteSpace(dbGuild.GoodbyeMessage)) return;

            var chn = e.Guild.GetChannel(dbGuild.GoodbyeChannelId);

            var postProcessedMessage = dbGuild.GoodbyeMessage
                .Replace("{name}", e.Member.Username)
                .Replace("{tag}", e.Member.Discriminator)
                .Replace("{guild}", e.Guild.Name);
            
            chn.SendMessageAsync(postProcessedMessage).ConfigureAwait(false).GetAwaiter().GetResult();
        });
    }

    private Task OnClientErrored(DiscordClient _, ClientErrorEventArgs e)
    {
        Log.Logger.Fatal(e.Exception, "An error occured when running the bot");
        throw e.Exception;
    }

    private Task OnSlashCommandErrored(SlashCommandsExtension _, SlashCommandErrorEventArgs e)
    {
        e.Context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            .WithAuthor("An error occured", null, e.Context.Client.CurrentUser.AvatarUrl)
            .WithDescription("Consider asking developers if this issue persists")));
        
        Log.Logger.Fatal(e.Exception, "An error occured when executing a slash command");
        throw e.Exception;
    }
}
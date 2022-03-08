using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Lavalink4NET;
using Lavalink4NET.Artwork;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Tracking;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Interactors;
using Lilia.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OsuSharp;
using OsuSharp.Extensions;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Lilia.Services;

public class LiliaClient
{
	public const GuildPermission RequiredPermissions =
		GuildPermission.ManageRoles | GuildPermission.ManageChannels | GuildPermission.KickMembers |
		GuildPermission.BanMembers | GuildPermission.ViewChannel |
		GuildPermission.ModerateMembers | GuildPermission.SendMessages |
		GuildPermission.SendMessagesInThreads |
		GuildPermission.EmbedLinks |
		GuildPermission.ReadMessageHistory |
		GuildPermission.UseExternalEmojis |
		GuildPermission.UseExternalStickers |
		GuildPermission.AddReactions |
		GuildPermission.Speak | GuildPermission.Connect |
		GuildPermission.StartEmbeddedActivities;

	public static readonly BotConfiguration BotConfiguration;
	public static readonly CancellationTokenSource Cts;
	public static readonly DbContextOptionsBuilder<LiliaDatabaseContext> OptionsBuilder = new();
	private DiscordSocketClient _client;

	private readonly LiliaDatabase _database;
	private InactivityTrackingService _inactivityTracker;
	private InteractionService _interactionService;
	private ServiceProvider _serviceProvider;

	public ArtworkService ArtworkService;
	public InteractiveService InteractiveService;
	public LavalinkNode Lavalink;
	public DateTime StartTime;

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

	public LiliaClient()
	{
		Log.Logger.Information("Setting up databases");
		_database = new LiliaDatabase();
	}

	private static async Task CheckForUpdateAsync()
	{
		Log.Logger.Information("Checking for updates");

		using HttpClient client = new();

		var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
		var sourceVersionStr =
			await client.GetStringAsync("https://raw.githubusercontent.com/Lilia-Workshop/Lilia/master/version.txt");
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
		await CheckForUpdateAsync();

		Log.Logger.Information("Setting up client");

		_client = new DiscordSocketClient(new DiscordSocketConfig
		{
			GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
			LogLevel = LogSeverity.Debug,
			LogGatewayIntentWarnings = true,
			UseInteractionSnowflakeDate = false,
			AlwaysDownloadUsers = true,
			UseSystemClock = false
		});

		_interactionService = new InteractionService(_client.Rest, new InteractionServiceConfig {LogLevel = LogSeverity.Debug, DefaultRunMode = RunMode.Async});

		InteractiveService = new InteractiveService(_client, new InteractiveConfig {DefaultTimeout = TimeSpan.FromMinutes(2), LogLevel = LogSeverity.Debug});

		var lavalinkConfig = BotConfiguration.Credentials.Lavalink;
		Lavalink = new LavalinkNode(new LavalinkNodeOptions
			{
				RestUri = $"http://{lavalinkConfig.Host}:{lavalinkConfig.Port}",
				WebSocketUri = $"ws://{lavalinkConfig.Host}:{lavalinkConfig.Port}",
				Password = lavalinkConfig.Password,
				DebugPayloads = true,
				DisconnectOnStop = false
			}, new DiscordClientWrapper(_client),
			new LavalinkLogger(new SerilogLoggerFactory(Log.Logger).CreateLogger("Lavalink")));

		_serviceProvider = new ServiceCollection()
			.AddLogging(x => x.AddSerilog())
			.AddDefaultSerializer()
			.AddDefaultRequestHandler()
			.AddSingleton(_database)
			.AddOsuSharp(x => x.Configuration = new OsuClientConfiguration {ModFormatSeparator = string.Empty, ClientId = BotConfiguration.Credentials.Osu.ClientId, ClientSecret = BotConfiguration.Credentials.Osu.ClientSecret})
			.AddSingleton(this)
			.BuildServiceProvider();

		await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);

		Log.Logger.Information("Registering event handlers");
		InteractiveService.Log += OnLog;

		_interactionService.Log += OnLog;

		_client.Log += OnLog;
		_client.InteractionCreated += OnClientInteractionCreated;
		_client.GuildAvailable += OnClientGuildAvailable;
		_client.GuildUnavailable += OnClientGuildUnavailable;
		_client.JoinedGuild += OnClientGuildAvailable;
		_client.LeftGuild += OnClientGuildUnavailable;
		_client.UserJoined += OnClientUserJoined;
		_client.UserLeft += OnClientUserLeft;
		_client.Ready += OnClientReady;

		Log.Logger.Information("Connecting and waiting for shutdown");
		await _client.LoginAsync(TokenType.Bot, BotConfiguration.Client.Token);
		await _client.StartAsync();

		while (!Cts.IsCancellationRequested) await Task.Delay(200);

		await Lavalink.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client shutdown");
		await Lavalink.DisposeAsync();
		await _client.StopAsync();
		await _client.LogoutAsync();
		await _database.GetContext().DisposeAsync();
	}

	private async Task OnClientInteractionCreated(SocketInteraction interaction)
	{
		try
		{
			var context = new SocketInteractionContext(_client, interaction);
			await _interactionService.ExecuteCommandAsync(context, _serviceProvider);
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());

			if (interaction.Type == InteractionType.ApplicationCommand)
				await interaction.GetOriginalResponseAsync()
					.ContinueWith(async message => await message.Result.DeleteAsync());
		}
	}

	private static Task OnLog(LogMessage message)
	{
		var severity = message.Severity switch
		{
			LogSeverity.Critical => LogEventLevel.Fatal,
			LogSeverity.Error => LogEventLevel.Error,
			LogSeverity.Warning => LogEventLevel.Warning,
			LogSeverity.Info => LogEventLevel.Information,
			LogSeverity.Verbose => LogEventLevel.Verbose,
			LogSeverity.Debug => LogEventLevel.Debug,
			_ => LogEventLevel.Information
		};

		Log
			.ForContext("SourceContext", message.Source)
			.Write(severity, message.Exception, "{Message:l}", message.Message);

		return Task.CompletedTask;
	}

	private async Task OnClientReady()
	{
		#region Register commands

		Log.Logger.Information("Registering commands");

		if (BotConfiguration.Client.PrivateGuildIds.Count > 0)
			foreach (var guildId in BotConfiguration.Client.PrivateGuildIds)
			{
				Log.Logger.Warning($"Registering slash commands for guild with ID \"{guildId}\"");
				await _interactionService.RegisterCommandsToGuildAsync(guildId);
			}

		if (BotConfiguration.Client.SlashCommandsForGlobal)
		{
			Log.Logger.Warning("Registering slash commands in global scope");
			await _interactionService.RegisterCommandsGloballyAsync();
		}

		#endregion

		#region Activity Setup

		Log.Logger.Information("Setting client activity");

		var clientActivityConfig = BotConfiguration.Client.Activity;

		if (!Enum.TryParse(clientActivityConfig.Type, out ActivityType activityType))
		{
			Log.Logger.Warning(
				$"Can not convert \"{clientActivityConfig.Type}\" to a valid activity type, using \"Playing\"");
			Log.Logger.Information("Valid options are: ListeningTo, Competing, Playing, Watching");
			activityType = ActivityType.Playing;
		}

		if (!Enum.TryParse(clientActivityConfig.Status, out UserStatus userStatus))
		{
			Log.Logger.Warning(
				$"Can not convert \"{clientActivityConfig.Status}\" to a valid status, using \"Online\"");
			Log.Logger.Information("Valid options are: Online, Invisible, Idle, DoNotDisturb");
			userStatus = UserStatus.Online;
		}

		await _client.SetActivityAsync(new Game(BotConfiguration.Client.Activity.Name, activityType));
		await _client.SetStatusAsync(userStatus);

		#endregion Activity Setup

		Log.Logger.Information("Initializing Lavalink connection");
		await Lavalink.InitializeAsync();
		ArtworkService = new ArtworkService();
		_inactivityTracker = new InactivityTrackingService(Lavalink, new DiscordClientWrapper(_client),
			new InactivityTrackingOptions {PollInterval = TimeSpan.FromMinutes(2), DisconnectDelay = TimeSpan.Zero, TrackInactivity = true}, new LavalinkLogger(new SerilogLoggerFactory(Log.Logger).CreateLogger("InteractivityTracker")));

		Log.Logger.Information($"Client is ready to serve as {Format.UsernameAndDiscriminator(_client.CurrentUser)}");
		StartTime = DateTime.Now;
	}

	private static Task OnClientGuildAvailable(SocketGuild guild)
	{
		Log.Logger.Debug($"Guild cache added: {guild.Name} (ID: {guild.Id})");
		return Task.CompletedTask;
	}

	private static Task OnClientGuildUnavailable(SocketGuild guild)
	{
		Log.Logger.Debug($"Guild cache removed: {guild.Name} (ID: {guild.Id})");
		return Task.CompletedTask;
	}

	private async Task OnClientUserJoined(SocketGuildUser user)
	{
		var dbGuild = _database.GetContext().GetGuildRecord(user.Guild);

		if (!dbGuild.IsWelcomeEnabled) return;
		if (dbGuild.WelcomeChannelId == 0 || user.Guild.GetChannel(dbGuild.WelcomeChannelId) == null) return;
		if (string.IsNullOrWhiteSpace(dbGuild.WelcomeMessage)) return;

		var chn = user.Guild.GetChannel(dbGuild.WelcomeChannelId);

		var postProcessedMessage = dbGuild.WelcomeMessage
			.Replace("{name}", user.Username)
			.Replace("{tag}", user.Discriminator)
			.Replace("{@user}", user.Mention)
			.Replace("{guild}", user.Guild.Name);

		await ((SocketTextChannel)chn).SendMessageAsync(postProcessedMessage);
	}

	private async Task OnClientUserLeft(SocketGuild guild, SocketUser user)
	{
		var dbGuild = _database.GetContext().GetGuildRecord(guild);

		if (!dbGuild.IsGoodbyeEnabled) return;
		if (dbGuild.GoodbyeChannelId == 0 || guild.GetChannel(dbGuild.GoodbyeChannelId) == null) return;
		if (string.IsNullOrWhiteSpace(dbGuild.GoodbyeMessage)) return;

		var chn = guild.GetChannel(dbGuild.GoodbyeChannelId);

		var postProcessedMessage = dbGuild.GoodbyeMessage
			.Replace("{name}", user.Username)
			.Replace("{tag}", user.Discriminator)
			.Replace("{guild}", guild.Name);

		await ((SocketTextChannel)chn).SendMessageAsync(postProcessedMessage);
	}
}

using System;
using System.Linq;
using System.Net.Http;
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
using Lavalink4NET.Logging.Microsoft;
using Lavalink4NET.Lyrics;
using Lavalink4NET.MemoryCache;
using Lavalink4NET.Tracking;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Interactors;
using Lilia.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
	private int _availableShardCount;
	private DiscordShardedClient _client;

	private LiliaDatabase _database;
	private InactivityTrackingService _inactivityTracker;
	private InteractionService _interactionService;
	private bool _isGlobalCommandRegistrationFinished;
	private bool _isGlobalCommandRegistrationNotificationLogged;
	private bool _isLavalinkInitialized;
	private bool _isPresenceSet;
	private ServiceProvider _serviceProvider;
	private int _totalShardCount;

	public ArtworkService ArtworkService;
	public InteractiveService InteractiveService;
	public IAudioService Lavalink;
	public LyricsService Lyrics;
	public DateTime StartTime;

	static LiliaClient()
	{
		Log.Information("Loading configuration");
		BotConfiguration = JsonManager<BotConfiguration>.Read();

		var isGuildRegExists = BotConfiguration.Client.PrivateGuildIds.Any();
		var isGlobalRegExists = BotConfiguration.Client.SlashCommandsForGlobal;

		switch (isGlobalRegExists)
		{
			case true when isGuildRegExists:
				Log.Logger.Warning("You are registering for both the guild and global");
				Log.Logger.Warning("This will result in command duplication");
				Log.Logger.Warning("If this was not your intention, shut this down IMMEDIATELY and change the .json setting");
				break;
			case false when !isGuildRegExists:
				Log.Logger.Warning("You are NOT registering any commands");
				Log.Logger.Warning("If this was not your intention, shut this down IMMEDIATELY and change the .json setting");
				break;
		}

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

	private static async Task CheckForUpdatesAsync()
	{
		Log.Logger.Information("Checking for updates");

		using HttpClient client = new();

		var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
		var sourceVersionStr = await client.GetStringAsync("https://raw.githubusercontent.com/Lilia-Workshop/Lilia/master/version.txt");
		var sourceVersion = Version.Parse(sourceVersionStr);

		Log.Logger.Debug("Current version: {CurrentVersion}  - Latest version: {SourceVersion}", currentVersion, sourceVersion);

		if (currentVersion < sourceVersion)
		{
			Log.Logger.Warning("You need to update your bot because there are \"breaking\" changes in the code");
			Log.Logger.Warning("Source code can be seen here: https://github.com/Lilia-Workshop/Lilia");
			Log.Logger.Warning("Changelogs in case you miss: https://github.com/Lilia-Workshop/Lilia/releases");
		}
		else
		{
			Log.Logger.Information("You are using latest version");
		}
	}

	private ServiceProvider GetRequiredServices()
	{
		var lavalinkConfig = BotConfiguration.Credentials.Lavalink;
		_database = new LiliaDatabase();
		_totalShardCount = BotConfiguration.Client.ShardCount;

		return new ServiceCollection()
			.AddLogging(x => x.AddSerilog())
			.AddSingleton(_database)
			.AddSingleton(new DiscordSocketConfig
			{
				TotalShards = _totalShardCount > 0 ? _totalShardCount : null,
				GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
				LogLevel = LogSeverity.Verbose,
				LogGatewayIntentWarnings = true,
				UseInteractionSnowflakeDate = false,
				AlwaysDownloadUsers = true,
				UseSystemClock = false,
				ConnectionTimeout = 24 * 60 * 60 * 1000
			})
			.AddSingleton<DiscordShardedClient>()
			.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordShardedClient>(),
				new InteractionServiceConfig
				{
					LogLevel = LogSeverity.Verbose
				}))
			.AddSingleton(x => new InteractiveService(x.GetRequiredService<DiscordShardedClient>()))
			.AddSingleton<ILavalinkCache, LavalinkCache>()
			.AddSingleton<IAudioService, LavalinkNode>()
			.AddSingleton<DiscordSocketClient>()
			.AddSingleton<LyricsOptions>()
			.AddSingleton<LyricsService>()
			.AddSingleton<ArtworkService>()
			.AddSingleton(new InactivityTrackingOptions
			{
				PollInterval = TimeSpan.FromMinutes(5),
				DisconnectDelay = TimeSpan.Zero,
				TrackInactivity = true
			})
			.AddSingleton<InactivityTrackingService>()
			.AddSingleton<IDiscordClientWrapper, DiscordClientWrapper>(x => new DiscordClientWrapper(x.GetRequiredService<DiscordShardedClient>()))
			.AddMicrosoftExtensionsLavalinkLogging()
			.AddSingleton(new LavalinkNodeOptions
			{
				RestUri = $"http://{lavalinkConfig.Host}:{lavalinkConfig.Port}",
				WebSocketUri = $"ws://{lavalinkConfig.Host}:{lavalinkConfig.Port}",
				Password = lavalinkConfig.Password,
				DebugPayloads = true,
				DisconnectOnStop = false
			})
			.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
			.AddDefaultSerializer()
			.AddDefaultRequestHandler()
			.AddOsuSharp(x => x.Configuration = new OsuClientConfiguration
			{
				ModFormatSeparator = string.Empty,
				ClientId = BotConfiguration.Credentials.Osu.ClientId,
				ClientSecret = BotConfiguration.Credentials.Osu.ClientSecret
			})
			.AddSingleton(this)
			.BuildServiceProvider();
	}

	public async Task RunAsync()
	{
		await CheckForUpdatesAsync();

		Log.Logger.Information("Setting up client");
		StartTime = DateTime.Now;

		await using (_serviceProvider = GetRequiredServices())
		{
			_client = _serviceProvider.GetRequiredService<DiscordShardedClient>();
			InteractiveService = _serviceProvider.GetRequiredService<InteractiveService>();
			_interactionService = _serviceProvider.GetRequiredService<InteractionService>();
			Lavalink = _serviceProvider.GetRequiredService<IAudioService>();
			Lyrics = _serviceProvider.GetRequiredService<LyricsService>();
			ArtworkService = _serviceProvider.GetRequiredService<ArtworkService>();
			_inactivityTracker = _serviceProvider.GetRequiredService<InactivityTrackingService>();

			Log.Logger.Information("Registering event handlers");
			_client.ShardReady += OnShardReady;
			_client.GuildAvailable += OnShardGuildAvailable;
			_client.GuildUnavailable += OnShardGuildUnavailable;
			_client.JoinedGuild += OnShardGuildAvailable;
			_client.LeftGuild += OnShardGuildUnavailable;
			_client.UserJoined += OnShardUserJoined;
			_client.UserLeft += OnShardUserLeft;
			_client.InteractionCreated += OnShardInteractionCreated;
			_client.Log += OnLog;
			_client.ShardConnected += OnShardConnected;
			_client.ShardDisconnected += OnShardDisconnected;
			InteractiveService.Log += OnLog;
			_interactionService.Log += OnLog;

			Log.Logger.Information("Building command modules");
			await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);

			Log.Logger.Information("Connecting and waiting for shutdown");
			await _client.LoginAsync(TokenType.Bot, BotConfiguration.Client.Token);
			await _client.StartAsync();
			while (!Cts.IsCancellationRequested) await Task.Delay(200);
			await _client.StopAsync();
			await _client.LogoutAsync();
			await _database.GetContext().DisposeAsync();
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

	private async Task OnShardReady(DiscordSocketClient client)
	{
		await Task.Run(async () =>
		{
			try
			{
				if (!_isGlobalCommandRegistrationFinished)
				{
					var isGlobalRegExists = BotConfiguration.Client.SlashCommandsForGlobal;

					if (isGlobalRegExists)
					{
						if (!_isGlobalCommandRegistrationNotificationLogged)
						{
							Log.Logger.Information("Adding commands globally");
							_isGlobalCommandRegistrationNotificationLogged = true;
						}

						var result = await _interactionService.RegisterCommandsGloballyAsync();
						Log.Logger.Information("Added {CommandCount} commands globally", result.Count);
					}

					_isGlobalCommandRegistrationFinished = true;
				}
			}
			catch
			{
				Log.Information("Command addition failed, will try again later");
			}
		});

		await Task.Run(async () =>
		{
			if (!_isLavalinkInitialized)
			{
				Log.Logger.Information("Trying to connect to Lavalink server");
				await Lavalink.InitializeAsync();
				if (!_inactivityTracker.IsTracking) _inactivityTracker.BeginTracking();
				_isLavalinkInitialized = true;
			}
		});

		await Task.Run(async () =>
		{
			if (!_isPresenceSet)
			{
				Log.Logger.Information("Setting client presence");

				var clientActivityConfig = BotConfiguration.Client.Activity;

				if (!Enum.TryParse(clientActivityConfig.Type, out ActivityType activityType))
				{
					Log.Logger.Warning("Can not convert \"{Type}\" to a valid activity type, using \"Playing\"", clientActivityConfig.Type);
					Log.Logger.Warning("Valid options are: ListeningTo, Competing, Playing, Watching");
					activityType = ActivityType.Playing;
				}

				if (!Enum.TryParse(clientActivityConfig.Status, out UserStatus userStatus))
				{
					Log.Logger.Warning("Can not convert \"{Status}\" to a valid status, using \"Online\"", clientActivityConfig.Status);
					Log.Logger.Warning("Valid options are: Online, Invisible, Idle, DoNotDisturb");
					userStatus = UserStatus.Online;
				}

				await _client.SetActivityAsync(new Game(BotConfiguration.Client.Activity.Name, activityType));
				await _client.SetStatusAsync(userStatus);

				Log.Logger.Information("Done setting the client presence");
				_isPresenceSet = true;
			}
		});
	}

	private Task OnShardConnected(DiscordSocketClient client)
	{
		++_availableShardCount;
		Log.Information("Shard #{ShardId} connected - {Count}/{Total} shards available", client.ShardId, _availableShardCount, _totalShardCount);
		if (_availableShardCount == _totalShardCount) Log.Information("All shards are ready");
		return Task.CompletedTask;
	}

	private Task OnShardDisconnected(Exception ex, DiscordSocketClient client)
	{
		--_availableShardCount;
		Log.Error(ex, "Shard #{ShardId} disconnected - {Count}/{Total} shards available", client.ShardId, _availableShardCount, _totalShardCount);
		return Task.CompletedTask;
	}

	private async Task OnShardInteractionCreated(SocketInteraction interaction)
	{
		_ = Task.Run(async () =>
		{
			var context = new ShardedInteractionContext(_client, interaction);
			await _interactionService.ExecuteCommandAsync(context, _serviceProvider);
		});
		await Task.CompletedTask;
	}

	private async Task OnShardGuildAvailable(SocketGuild guild)
	{
		Log.Logger.Information("Guild cache added: {GuildName} (ID: {GuildId})", guild.Name, guild.Id);

		var isExists = BotConfiguration.Client.PrivateGuildIds.Any(x => x == guild.Id);

		if (isExists)
		{
			Log.Logger.Information("Adding commands to the guild {GuildId}", guild.Id);
			var result = await _interactionService.RegisterCommandsToGuildAsync(guild.Id);
			Log.Logger.Information("Added {CommandCount} commands to the guild {GuildId}", result.Count, guild.Id);
		}
	}

	private static Task OnShardGuildUnavailable(SocketGuild guild)
	{
		Log.Logger.Information("Guild cache removed: {GuildName} (ID: {GuildId})", guild.Name, guild.Id);
		return Task.CompletedTask;
	}

	private async Task OnShardUserJoined(SocketGuildUser user)
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

	private async Task OnShardUserLeft(SocketGuild guild, SocketUser user)
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Interactors;
using Lilia.Enums;
using Lilia.Modules.Utils;
using Lilia.Services;

namespace Lilia.Modules;

[Group("music", "Music commands")]
public class MusicModule : InteractionModuleBase<ShardedInteractionContext>
{
	private readonly LiliaClient _client;
	private readonly LiliaDatabaseContext _dbCtx;

	public MusicModule(LiliaClient client, LiliaDatabase database)
	{
		_client = client;
		_dbCtx = database.GetContext();
	}

	[SlashCommand("connect", "Connect to your current voice channel")]
	public async Task MusicPlaybackConnectCommand(
		[Summary("connection_type", "Connection type")]
		MusicConnectionType connectionType = MusicConnectionType.Normal)
	{
		await Context.Interaction.DeferAsync();

		var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
		if (!await mmu.EnsureUserInVoiceAsync()) return;

		var player = _client.Lavalink.GetPlayer(Context.Guild.Id);
		var existedPlayerType = player is QueuedLavalinkPlayer ? "queued player" : "normal player";
		var newPlayerType = connectionType == MusicConnectionType.Queued ? "queued player" : "normal player";

		if (player != null)
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = $"Already connected as {existedPlayerType}");

			return;
		}

		if (connectionType == MusicConnectionType.Queued)
			await _client.Lavalink.JoinAsync<QueuedLavalinkPlayer>(Context.Guild.Id,
				((SocketGuildUser)Context.User).VoiceState!.Value.VoiceChannel.Id, true);
		else
			await _client.Lavalink.JoinAsync(Context.Guild.Id, ((SocketGuildUser)Context.User).VoiceState!.Value.VoiceChannel.Id, true);

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = $"Done establishing the connection as {newPlayerType}");
	}

	[SlashCommand("disconnect", "Leave current voice channel")]
	public async Task MusicPlaybackDisconnectCommand()
	{
		await Context.Interaction.DeferAsync();

		var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
		if (!await mmu.EnsureUserInVoiceAsync()) return;

		var player = _client.Lavalink.GetPlayer(Context.Guild.Id);
		_client.Lavalink.TrackStarted -= mmu.OnTrackStarted;
		_client.Lavalink.TrackStuck -= mmu.OnTrackStuck;
		_client.Lavalink.TrackEnd -= mmu.OnTrackEnd;
		_client.Lavalink.TrackException -= mmu.OnTrackException;

		await player!.DisconnectAsync();

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = "Left the voice channel");
	}

	[SlashCommand("play_stream", "Play a stream")]
	public async Task MusicPlaybackPlayStreamCommand(
		[Summary("stream_url", "Stream URL")] string streamUrl)
	{
		await Context.Interaction.DeferAsync();

		var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
		var player = _client.Lavalink.GetPlayer(Context.Guild.Id);

		if (!await mmu.EnsureUserInVoiceAsync()) return;
		if (!await mmu.EnsureClientInVoiceAsync()) return;
		if (!await mmu.EnsureNormalPlayerAsync()) return;

		if (!Uri.IsWellFormedUriString(streamUrl, UriKind.Absolute))
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "I need a valid stream URL to function");

			return;
		}

		var track = await _client.Lavalink.GetTrackAsync(streamUrl);

		if (track == null)
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = $"Unable to get the stream from {streamUrl}");

			return;
		}

		await player!.PlayAsync(track);

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = $"Now streaming from {streamUrl}");

		var dbGuild = _dbCtx.GetGuildRecord(Context.Guild);
		dbGuild.RadioStartTime = DateTime.UtcNow;
		await _dbCtx.SaveChangesAsync();
	}

	[SlashCommand("play_queue", "Play queued tracks")]
	public async Task MusicPlaybackPlayQueueCommand()
	{
		await Context.Interaction.DeferAsync();

		var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));

		if (!await mmu.EnsureUserInVoiceAsync()) return;
		if (!await mmu.EnsureClientInVoiceAsync()) return;
		if (!await mmu.EnsureQueuedPlayerAsync()) return;
		if (!await mmu.EnsureQueueIsNotEmptyAsync()) return;

		var queuedPlayer = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);
		var track = queuedPlayer!.Queue.Dequeue();

		await queuedPlayer.PlayAsync(track, false);

		_client.Lavalink.TrackStarted += mmu.OnTrackStarted;
		_client.Lavalink.TrackStuck += mmu.OnTrackStuck;
		_client.Lavalink.TrackEnd += mmu.OnTrackEnd;
		_client.Lavalink.TrackException += mmu.OnTrackException;
	}

	[SlashCommand("now_playing", "View now playing track")]
	public async Task MusicPlaybackNowPlayingCommand()
	{
		await Context.Interaction.DeferAsync();

		var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
		if (!await mmu.EnsureUserInVoiceAsync()) return;
		if (!await mmu.EnsureClientInVoiceAsync()) return;

		var player = _client.Lavalink.GetPlayer(Context.Guild.Id);
		var track = player!.CurrentTrack;

		if (track == null)
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "Unable to get the playing track");

			return;
		}

		var isStream = track.IsLiveStream;
		var art = await _client.ArtworkService.ResolveAsync(track);

		var dbGuild = _dbCtx.GetGuildRecord(Context.Guild);

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Embed = Context.User.CreateEmbedWithUserData()
				.WithAuthor("Currently playing track", Context.Client.CurrentUser.GetAvatarUrl())
				.WithThumbnailUrl(art?.OriginalString ?? "")
				.AddField("Title", Format.Sanitize(track.Title))
				.AddField("Author", Format.Sanitize(track.Author), true)
				.AddField("Source", Format.Sanitize(track.Source ?? "Unknown"), true)
				.AddField(isStream ? "Playtime" : "Position", isStream
					? DateTime.UtcNow.Subtract(dbGuild.RadioStartTime).ToLongReadableTimeSpan()
					: $"{player.Position.RelativePosition:g}/{track.Duration:g}", true)
				.AddField("Is looping", player is QueuedLavalinkPlayer lavalinkPlayer
					? $"{lavalinkPlayer.IsLooping}"
					: "This is not a queued player", true)
				.AddField("Is paused", $"{player.State == PlayerState.Paused}", true).Build());
	}

	[SlashCommand("skip", "Skip this track")]
	public async Task MusicPlaybackSkipCommand()
	{
		await Context.Interaction.DeferAsync();

		var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
		if (!await mmu.EnsureUserInVoiceAsync()) return;
		if (!await mmu.EnsureClientInVoiceAsync()) return;
		if (!await mmu.EnsureQueuedPlayerAsync()) return;

		var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);
		var track = player!.CurrentTrack;

		if (track == null)
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "Unable to get the track, maybe because I am not playing anything");

			return;
		}

		await player.SkipAsync();

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = $"Skipped - {Format.Bold(Format.Sanitize(track.Title))} by {Format.Bold(Format.Sanitize(track.Author))}");
	}

	[SlashCommand("stop", "Stop this session")]
	public async Task MusicPlaybackStopCommand()
	{
		await Context.Interaction.DeferAsync();

		var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
		if (!await mmu.EnsureUserInVoiceAsync()) return;
		if (!await mmu.EnsureClientInVoiceAsync()) return;

		var player = _client.Lavalink.GetPlayer(Context.Guild.Id);
		await player!.StopAsync();

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = "Stopped this session, the queue will be cleaned");
	}

	[SlashCommand("pause", "Pause this session")]
	public async Task MusicPlaybackPauseCommand()
	{
		await Context.Interaction.DeferAsync();

		var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
		if (!await mmu.EnsureUserInVoiceAsync()) return;
		if (!await mmu.EnsureClientInVoiceAsync()) return;

		var player = _client.Lavalink.GetPlayer(Context.Guild.Id);

		if (player!.State == PlayerState.Paused)
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "Paused earlier");

			return;
		}

		await player.PauseAsync();

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = "Pausing");
	}

	[SlashCommand("resume", "Resume this session")]
	public async Task MusicPlaybackResumeCommand()
	{
		await Context.Interaction.DeferAsync();

		var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
		if (!await mmu.EnsureUserInVoiceAsync()) return;
		if (!await mmu.EnsureClientInVoiceAsync()) return;

		var player = _client.Lavalink.GetPlayer(Context.Guild.Id);

		if (player!.State != PlayerState.Paused)
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "Resumed earlier");

			return;
		}

		await player.ResumeAsync();

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = "Resuming");
	}

	[SlashCommand("loop", "Toggle current track loop")]
	public async Task MusicPlaybackLoopCommand()
	{
		await Context.Interaction.DeferAsync();

		var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));

		if (!await mmu.EnsureUserInVoiceAsync()) return;
		if (!await mmu.EnsureClientInVoiceAsync()) return;
		if (!await mmu.EnsureQueuedPlayerAsync()) return;

		var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);
		var track = player!.CurrentTrack;

		if (track == null)
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "Unable to get the track, maybe because I am not playing anything");

			return;
		}

		player.IsLooping = !player.IsLooping;

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content =
				$"{(player.IsLooping ? "Looping" : "Removed the loop of")} the track: {Format.Bold(Format.Sanitize(track.Title))} by {Format.Bold(Format.Sanitize(track.Author))}");
	}

	[SlashCommand("change_player", "Change current player")]
	public async Task MusicPlaybackChangePlayerCommand(
		[Summary("connection_type", "Connection type")]
		MusicConnectionType connectionType = MusicConnectionType.Normal)
	{
		await Context.Interaction.DeferAsync();

		var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
		if (!await mmu.EnsureUserInVoiceAsync()) return;
		if (!await mmu.EnsureClientInVoiceAsync()) return;

		var oldPlayer = _client.Lavalink.GetPlayer(Context.Guild.Id);

		if (oldPlayer!.State is not PlayerState.NotPlaying)
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "Can not change the player because there is a pending track");

			return;
		}

		switch (oldPlayer)
		{
			case QueuedLavalinkPlayer when connectionType == MusicConnectionType.Queued:
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "Already connected as queued player");

				return;

			case QueuedLavalinkPlayer:
				await oldPlayer.DisconnectAsync();
				await oldPlayer.DestroyAsync();
				await _client.Lavalink.JoinAsync(Context.Guild.Id, ((SocketGuildUser)Context.User).VoiceState!.Value.VoiceChannel.Id, true);

				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "Switched to normal player");
				break;

			case not null when connectionType == MusicConnectionType.Normal:
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "Already connected as normal player");

				return;
			case not null:
				await oldPlayer.DisconnectAsync();
				await oldPlayer.DestroyAsync();
				await _client.Lavalink.JoinAsync<QueuedLavalinkPlayer>(Context.Guild.Id,
					((SocketGuildUser)Context.User).VoiceState!.Value.VoiceChannel.Id, true);

				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "Switched to queued player");
				break;
		}
	}

	[SlashCommand("lyrics", "Check lyrics of a track")]
	public async Task MusicPlaybackLyricsCommand(
		[Summary("artist", "Artist name")] string artist,
		[Summary("track_name", "Track name")] string trackName)
	{
		await Context.Interaction.DeferAsync();

		try
		{
			var lyrics = await _client.Lyrics.GetLyricsAsync(artist, trackName);

			if (string.IsNullOrWhiteSpace(lyrics))
			{
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content =
						$"No lyrics found for: {Format.Bold(Format.Sanitize(artist))} by {Format.Bold(Format.Sanitize(trackName))}");

				return;
			}

			var paginator = new StaticPaginatorBuilder()
				.AddUser(Context.User)
				.WithPages(LiliaUtilities.CreatePagesFromString(lyrics))
				.Build();

			await _client.InteractiveService.SendPaginatorAsync(paginator, Context.Interaction,
				responseType: InteractionResponseType.DeferredChannelMessageWithSource);
		}
		catch
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content =
					$"Can not get lyrics for: {Format.Bold(Format.Sanitize(artist))} by {Format.Bold(Format.Sanitize(trackName))}");
		}
	}

	[Group("queue", "Queue commands")]
	public class MusicQueueModule : InteractionModuleBase<ShardedInteractionContext>
	{
		private readonly LiliaClient _client;

		public MusicQueueModule(LiliaClient client)
		{
			_client = client;
		}

		[SlashCommand("add_playlist", "Add tracks from a playlist to queue")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task MusicQueueAddPlaylistCommand(
			[Summary("playlist_url", "Playlist URL")]
			string playlistUrl)
		{
			await Context.Interaction.DeferAsync();

			var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
			if (!await mmu.EnsureUserInVoiceAsync()) return;
			if (!await mmu.EnsureClientInVoiceAsync()) return;
			if (!await mmu.EnsureQueuedPlayerAsync()) return;

			if (!Uri.IsWellFormedUriString(playlistUrl, UriKind.Absolute))
			{
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "You need to provide a valid URL");

				return;
			}

			var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);
			var tracks = await _client.Lavalink.GetTracksAsync(playlistUrl);
			var lavalinkTracks = tracks.ToList();

			if (!lavalinkTracks.Any())
			{
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "Unable to get the tracks");

				return;
			}

			List<LavalinkTrack> postProcessedTracks = new();

			StringBuilder text = new();
			var idx = 0;

			foreach (var track in lavalinkTracks)
			{
				var testStr = track.IsLiveStream
					? $"Livestream skipped: {Format.Url($"{Format.Bold(track.Title)} by {Format.Bold(track.Author)}", track.Source ?? "https://example.com")}"
					: $"{idx + 1} - {Format.Url($"{Format.Bold(Format.Sanitize(track.Title))} by {Format.Bold(track.Author)}", track.Source ?? "https://example.com")}";

				text.AppendLine(testStr);

				if (track.IsLiveStream) continue;

				++idx;
				postProcessedTracks.Add(track);
			}

			player!.Queue.AddRange(postProcessedTracks);

			var pages = LiliaUtilities.CreatePagesFromString($"{text}");
			var paginator = new StaticPaginatorBuilder()
				.AddUser(Context.User)
				.WithPages(pages)
				.Build();

			await _client.InteractiveService.SendPaginatorAsync(paginator, Context.Interaction,
				responseType: InteractionResponseType.DeferredChannelMessageWithSource);
		}

		[SlashCommand("add_tracks", "Add tracks to queue")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task MusicQueueAddCommand(
			[Summary("query", "Music query")] string query,
			[Summary("source", "Music source")] MusicSource source = MusicSource.YouTube)
		{
			await Context.Interaction.DeferAsync();

			var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
			if (!await mmu.EnsureUserInVoiceAsync()) return;
			if (!await mmu.EnsureClientInVoiceAsync()) return;
			if (!await mmu.EnsureQueuedPlayerAsync()) return;

			Enum.TryParse(source.ToString(), out SearchMode searchMode);

			var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);
			var tracks = await _client.Lavalink.GetTracksAsync(query, searchMode);

			var lavalinkTracks = tracks.ToList();

			if (!lavalinkTracks.Any())
			{
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "Unable to get the tracks");

				return;
			}

			var menuBuilder = new SelectMenuBuilder()
				.WithPlaceholder("Choose your tracks (max 10)")
				.WithCustomId("track-select-drop")
				.WithMaxValues(10)
				.AddOption("Wait I want to go back", "-1", "Use this in case you didn't find anything", Emoji.Parse(":x:"));

			var idx = 0;
			var options = new List<string>();

			foreach (var track in lavalinkTracks.Take(24))
			{
				if (track.IsLiveStream) continue;
				var title = $"{track.Title} - {track.Author}";
				menuBuilder.AddOption(title.Length <= 100 ? title : string.Join("", title.Take(97)) + "...", $"{idx}",
					track.Source);
				options.Add(track.Source);
				++idx;
			}

			var message = await Context.Interaction.ModifyOriginalResponseAsync(x =>
			{
				x.Content = "Choose your tracks";
				x.Components = new ComponentBuilder()
					.WithSelectMenu(menuBuilder)
					.Build();
			});

			var res = (SocketMessageComponent)await InteractionUtility.WaitForMessageComponentAsync(Context.Client, message, TimeSpan.FromMinutes(2));

			List<LavalinkTrack> tracksList = new();
			StringBuilder text = new();

			foreach (var value in res.Data.Values)
			{
				var iidx = Convert.ToInt32(value);

				if (iidx == -1)
				{
					await Context.Interaction.ModifyOriginalResponseAsync(x =>
						x.Content = "No tracks added");

					text.Clear();
					break;
				}

				var track = await _client.Lavalink.GetTrackAsync(options[iidx]);
				tracksList.Add(track);
				text.AppendLine($"{Format.Bold(Format.Sanitize(track!.Title))} by {Format.Bold(track.Author)}");
			}

			player!.Queue.AddRange(tracksList);

			await res.UpdateAsync(x =>
			{
				x.Content = "Tracks added";
				x.Components = new ComponentBuilder().Build();
				x.Embed = Context.User.CreateEmbedWithUserData()
					.WithAuthor("Added tracks to queue", Context.Client.CurrentUser.GetAvatarUrl())
					.WithDescription(string.IsNullOrWhiteSpace($"{text}") ? "Nothing" : $"{text}")
					.Build();
			});
		}

		[SlashCommand("view", "Check the queue of current playing session")]
		public async Task MusicQueueCheckCommand()
		{
			await Context.Interaction.DeferAsync();

			var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
			if (!await mmu.EnsureUserInVoiceAsync()) return;
			if (!await mmu.EnsureClientInVoiceAsync()) return;
			if (!await mmu.EnsureQueuedPlayerAsync()) return;
			if (!await mmu.EnsureQueueIsNotEmptyAsync()) return;

			var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);
			var queue = player!.Queue;

			var idx = 0;

			StringBuilder text = new();

			foreach (var track in queue)
			{
				var testStr =
					$"{idx + 1} - {Format.Url($"{Format.Bold(Format.Sanitize(track.Title))} by {Format.Bold(track.Author)}", track.Source ?? "https://example.com")}";
				text.AppendLine(testStr);
				++idx;
			}

			var pages = LiliaUtilities.CreatePagesFromString($"{text}");

			var paginator = new StaticPaginatorBuilder()
				.AddUser(Context.User)
				.WithPages(pages)
				.Build();

			await _client.InteractiveService.SendPaginatorAsync(paginator, Context.Interaction,
				responseType: InteractionResponseType.DeferredChannelMessageWithSource);
		}

		[SlashCommand("shuffle", "Shuffle the queue")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task MusicQueueShuffleCommand()
		{
			await Context.Interaction.DeferAsync();

			var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
			if (!await mmu.EnsureUserInVoiceAsync()) return;
			if (!await mmu.EnsureClientInVoiceAsync()) return;
			if (!await mmu.EnsureQueuedPlayerAsync()) return;
			if (!await mmu.EnsureQueueIsNotEmptyAsync()) return;

			var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);

			player!.Queue.Shuffle();

			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "Shuffled the queue");
		}

		[SlashCommand("clear", "Clear the queue")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task MusicQueueClearCommand()
		{
			await Context.Interaction.DeferAsync();

			var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
			if (!await mmu.EnsureUserInVoiceAsync()) return;
			if (!await mmu.EnsureClientInVoiceAsync()) return;
			if (!await mmu.EnsureQueuedPlayerAsync()) return;
			if (!await mmu.EnsureQueueIsNotEmptyAsync()) return;

			var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);

			player!.Queue.Clear();

			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "Cleared all tracks of the queue");
		}

		[SlashCommand("remove", "Remove a track from the queue")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task MusicQueueRemoveCommand(
			[Summary("index", "Index to remove from 0 (first track)")]
			long index)
		{
			await Context.Interaction.DeferAsync();

			var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
			if (!await mmu.EnsureUserInVoiceAsync()) return;
			if (!await mmu.EnsureClientInVoiceAsync()) return;
			if (!await mmu.EnsureQueuedPlayerAsync()) return;
			if (!await mmu.EnsureQueueIsNotEmptyAsync()) return;

			var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);

			if (index < 0 || index >= player!.Queue.Count)
			{
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "Invalid index");

				return;
			}

			var posInt = Convert.ToInt32(index);

			var track = player.Queue[posInt];
			player.Queue.RemoveAt(posInt);

			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content =
					$"Removed track at index {index}: {Format.Bold(Format.Sanitize(track.Title))} by {Format.Bold(Format.Sanitize(track.Author))}");
		}

		[SlashCommand("remove_range", "Remove a range of tracks from the queue")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task MusicQueueRemoveRangeCommand(
			[Summary("start_index", "Starting index to remove from 0 (first track)")]
			long startIndex,
			[Summary("end_index", "Ending index to remove from 0 (first track)")]
			long endIndex)
		{
			await Context.Interaction.DeferAsync();

			var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
			if (!await mmu.EnsureUserInVoiceAsync()) return;
			if (!await mmu.EnsureClientInVoiceAsync()) return;
			if (!await mmu.EnsureQueuedPlayerAsync()) return;
			if (!await mmu.EnsureQueueIsNotEmptyAsync()) return;

			var startIndexInt = Convert.ToInt32(startIndex);
			var endIndexInt = Convert.ToInt32(endIndex);

			var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);

			if (startIndexInt < 0 || endIndexInt >= player!.Queue.Count || startIndexInt > endIndexInt)
			{
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "Invalid index");

				return;
			}

			List<PageBuilder> pages = new();

			for (var p = startIndexInt; p <= endIndexInt; ++p)
			{
				var track = player.Queue[p];
				var page = new PageBuilder();
				page.WithText($"{Format.Bold(Format.Sanitize(track.Title))} by {Format.Bold(Format.Sanitize(track.Author))}");
				pages.Add(page);
			}

			player.Queue.RemoveRange(startIndexInt, endIndexInt - startIndexInt + 1);

			var paginator = new StaticPaginatorBuilder()
				.AddUser(Context.User)
				.WithPages(pages)
				.Build();

			await _client.InteractiveService.SendPaginatorAsync(paginator, Context.Channel);
		}

		[SlashCommand("make_unique", "Remove duplicating tracks from the list")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task MusicQueueRemoveRangeCommand()
		{
			await Context.Interaction.DeferAsync();

			var mmu = new MusicModuleUtils(Context.Interaction, _client.Lavalink.GetPlayer(Context.Guild.Id));
			if (!await mmu.EnsureUserInVoiceAsync()) return;
			if (!await mmu.EnsureClientInVoiceAsync()) return;
			if (!await mmu.EnsureQueuedPlayerAsync()) return;
			if (!await mmu.EnsureQueueIsNotEmptyAsync()) return;

			var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);

			player!.Queue.Distinct();

			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "Removed duplicating tracks with same source from the queue");
		}
	}
}

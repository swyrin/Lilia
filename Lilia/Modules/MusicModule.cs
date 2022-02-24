using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using Lilia.Commons;
using Lilia.Enums;
using Lilia.Modules.Utils;
using Lilia.Services;

namespace Lilia.Modules;

[SlashCommandGroup("music", "Music commands")]
public class MusicModule : ApplicationCommandModule
{
    [SlashCommandGroup("playback", "Music playback commands")]
    public class MusicPlaybackModule : ApplicationCommandModule
    {
        private readonly LiliaClient _client;

        public MusicPlaybackModule(LiliaClient client)
        {
            _client = client;
        }

        [SlashCommand("connect", "Connect to your current voice channel")]
        public async Task MusicPlaybackConnectCommand(InteractionContext ctx,
            [Option("connection_type", "Connection type")]
            MusicConnectType connectType = MusicConnectType.QueuedPlayer)
        {
            await ctx.DeferAsync();

            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;

            var player = _client.Lavalink.GetPlayer(ctx.Guild.Id);

            var existedPlayerType = player is QueuedLavalinkPlayer ? "as queued player" : "as normal player";
            var newPlayerType = connectType == MusicConnectType.QueuedPlayer ? "as queued player" : "as normal player";

            if (player != null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"Already connected {existedPlayerType}"));
                
                return;
            }
            
            if (connectType == MusicConnectType.QueuedPlayer)
                await _client.Lavalink.JoinAsync<QueuedLavalinkPlayer>(ctx.Guild.Id, ctx.Member.VoiceState.Channel.Id, true);
            else
                await _client.Lavalink.JoinAsync(ctx.Guild.Id, ctx.Member.VoiceState.Channel.Id, true);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Done establishing the connection {newPlayerType}"));
        }

        [SlashCommand("disconnect", "Leave current voice channel")]
        public async Task MusicPlaybackDisconnectCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;

            var player = _client.Lavalink.GetPlayer(ctx.Guild.Id);
            await player.DisconnectAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Left the voice channel"));
            
            _client.Lavalink.TrackStarted -= new MusicModuleUtils(ctx).OnTrackStarted;
            _client.Lavalink.TrackStuck -= new MusicModuleUtils(ctx).OnTrackStuck;
            _client.Lavalink.TrackEnd -= new MusicModuleUtils(ctx).OnTrackEnd;
            _client.Lavalink.TrackException -= new MusicModuleUtils(ctx).OnTrackException;
        }
        
        [SlashCommand("play_stream", "Play a stream")]
        public async Task MusicPlaybackPlayStreamCommand(InteractionContext ctx,
            [Option("stream_url", "Stream URL")]
            string streamUrl)
        {
            await ctx.DeferAsync();
            
            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureNormalPlayerAsync(ctx)) return;

            if (!Uri.IsWellFormedUriString(streamUrl, UriKind.Absolute))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("You need to provide a valid URL"));

                return;
            }

            var track = await _client.Lavalink.GetTrackAsync(streamUrl);

            if (track == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Unable to get the stream"));

                return;
            }

            var player = _client.Lavalink.GetPlayer(ctx.Guild.Id);
            await player.PlayAsync(track);
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Now playing a stream from {streamUrl}"));
        }

        [SlashCommand("play_queue", "Play queued tracks")]
        public async Task MusicPlaybackPlayQueueCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueuedPlayerAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueueIsNotEmptyAsync(ctx)) return;

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);
            var track = player.Queue.Dequeue();
            
            await player.PlayAsync(track, false);

            _client.Lavalink.TrackStarted += new MusicModuleUtils(ctx).OnTrackStarted;
            _client.Lavalink.TrackStuck += new MusicModuleUtils(ctx).OnTrackStuck;
            _client.Lavalink.TrackEnd += new MusicModuleUtils(ctx).OnTrackEnd;
            _client.Lavalink.TrackException += new MusicModuleUtils(ctx).OnTrackException;
        }

        [SlashCommand("now_playing", "View now playing track")]
        public async Task MusicPlaybackNowPlayingCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            
            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;

            var player = _client.Lavalink.GetPlayer(ctx.Guild.Id);
            var track = player.CurrentTrack;

            if (track == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Unable to get the track, maybe because I am not playing anything"));

                return;
            }

            var isStream = track.IsLiveStream;

            var art = await _client.ArtworkService.ResolveAsync(track);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(ctx.Member.GetDefaultEmbedTemplateForUser()
                    .WithAuthor("Currently playing track", null, ctx.Client.CurrentUser.AvatarUrl)
                    .WithThumbnail(art?.OriginalString ?? "")
                    .AddField("Title", Formatter.Sanitize(track.Title))
                    .AddField("Author", Formatter.Sanitize(track.Author), true)
                    .AddField("Source", Formatter.Sanitize(track.Source ?? "Unknown"), true)
                    .AddField(isStream ? "Playtime": "Position",
                        isStream ? (player.Position.RelativePosition - track.Position).ToLongReadableTimeSpan() : $"{player.Position.RelativePosition:g}/{track.Duration:g}",
                        true)
                    .AddField("Is looping",
                        player is QueuedLavalinkPlayer lavalinkPlayer
                            ? $"{lavalinkPlayer.IsLooping}"
                            : "This is not a queued player", true)
                    .AddField("Is paused", $"{player.State == PlayerState.Paused}", true)));
        }

        [SlashCommand("skip", "Skip this track")]
        public async Task MusicPlaybackSkipCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueuedPlayerAsync(ctx)) return;

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);
            var track = player.CurrentTrack;

            if (track == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Unable to get the track, maybe because I am not playing anything"));

                return;
            }

            await player.SkipAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Skipped track: {Formatter.Bold(Formatter.Sanitize(track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Author))}"));
        }

        [SlashCommand("stop", "Stop this session")]
        public async Task MusicPlaybackStopCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;

            var player = _client.Lavalink.GetPlayer(ctx.Guild.Id);
            await player.StopAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Stopped this session, the queue will be cleaned"));
        }

        [SlashCommand("pause", "Pause this session")]
        public async Task MusicPlaybackPauseCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            
            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;
            
            var player = _client.Lavalink.GetPlayer(ctx.Guild.Id);

            if (player.State == PlayerState.Paused)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Paused earlier"));

                return;
            }

            await player.PauseAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Pausing"));
        }

        [SlashCommand("resume", "Resume this session")]
        public async Task MusicPlaybackResumeCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            
            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;
            
            var player = _client.Lavalink.GetPlayer(ctx.Guild.Id);
            
            if (player.State != PlayerState.Paused)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Resumed earlier"));

                return;
            }

            await player.ResumeAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Resuming"));
        }

        [SlashCommand("loop", "Toggle current track loop")]
        public async Task MusicPlaybackLoopCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            
            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueuedPlayerAsync(ctx)) return;

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);
            var track = player.CurrentTrack;

            if (track == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Unable to get the track, maybe because I am not playing anything"));

                return;
            }

            player.IsLooping = !player.IsLooping;

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"{(player.IsLooping ? "Looping" : "Removed the loop of")} the track: {Formatter.Bold(Formatter.Sanitize(track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Author))}"));
        }
    }

    [SlashCommandGroup("queue", "Queue commands")]
    public class MusicQueueModule : ApplicationCommandModule
    {
        private readonly LiliaClient _client;

        public MusicQueueModule(LiliaClient client)
        {
            _client = client;
        }

        [SlashCommand("add_playlist", "Add tracks from a playlist to queue")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task MusicQueueAddPlaylistCommand(InteractionContext ctx,
            [Option("playlist_url", "Playlist URl")]
            string playlistUrl)
        {
            await ctx.DeferAsync();
            
            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueuedPlayerAsync(ctx)) return;
            
            if (!Uri.IsWellFormedUriString(playlistUrl, UriKind.Absolute))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("You need to provide a valid URL"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);
            var tracks = await _client.Lavalink.GetTracksAsync(playlistUrl);
            var lavalinkTracks = tracks.ToList();
            
            if (!lavalinkTracks.Any())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Unable to get the tracks"));

                return;
            }

            List<LavalinkTrack> postProcessedTracks = new();

            StringBuilder text = new();
            var idx = 1;

            foreach (var track in lavalinkTracks)
            {
                if (track.IsLiveStream)
                {
                    text.AppendLine($"Livestream skipped - {Formatter.MaskedUrl($"{Formatter.Bold(track.Title)} by {Formatter.Bold(track.Author)}", new Uri(track.Source ?? "https://example.com"))}");
                    continue;
                }

                text.AppendLine($"{idx} - {Formatter.MaskedUrl($"{Formatter.Bold(Formatter.Sanitize(track.Title))} by {Formatter.Bold(track.Author)}", new Uri(track.Source ?? "https://example.com"))}");
                ++idx;
                postProcessedTracks.Add(track);
            }

            player.Queue.AddRange(postProcessedTracks);

            var pages = ctx.Client.GetInteractivity().GeneratePagesInEmbed($"{text}", SplitType.Line,
                ctx.Member.GetDefaultEmbedTemplateForUser()
                    .WithAuthor($"{idx + 1} tracks from playlist has been added to queue", playlistUrl, ctx.Client.CurrentUser.AvatarUrl));

            await ctx.Interaction.SendPaginatedResponseAsync(false, ctx.Member, pages, asEditResponse: true);
        }

        [SlashCommand("add_track", "Add tracks to queue")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task MusicQueueAddCommand(InteractionContext ctx,
            [Option("query", "Music query")] string query,
            [Option("source", "Music source")] MusicSource source = MusicSource.YouTube)
        {
            await ctx.DeferAsync();
            
            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueuedPlayerAsync(ctx)) return;
            
            Enum.TryParse(source.ToString(), out SearchMode searchMode);

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);
            var tracks = await _client.Lavalink.GetTracksAsync(query, searchMode);

            var lavalinkTracks = tracks.ToList();
            
            if (!lavalinkTracks.Any())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Unable to get the tracks"));

                return;
            }
            
            var selectComponentOptions = new List<DiscordSelectComponentOption>();
            var options = new List<string>();

            selectComponentOptions.Add(new DiscordSelectComponentOption("Wait, I want to go back!!!", "-1",
                "Cancel current operation", emoji: new DiscordComponentEmoji(":x:")));
            
            var idx = 0;
            foreach (var track in lavalinkTracks)
            {
                if (track.IsLiveStream) continue;
                
                var title = $"{track.Title} - {track.Author}";

                selectComponentOptions.Add(new DiscordSelectComponentOption(string.Join("", title.Take(97)) + "...", $"{idx}", track.Source));
                options.Add(track.Source);
                
                ++idx;
            }
            
            DiscordSelectComponent selectComponent = new("trackSelectDrop", "Choose the track", selectComponentOptions, maxOptions: 10);

            var message = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Choose your tracks (max 10)")
                .AddComponents(selectComponent));

            var res = await message.WaitForSelectAsync(x => x.User == ctx.Member, TimeSpan.FromMinutes(5));

            if (res.TimedOut)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Timed out"));
                
                return;
            }

            StringBuilder text = new();
            List<LavalinkTrack> tracksList = new();

            foreach (var value in res.Result.Values)
            {
                int _idx = Convert.ToInt32(value);

                if (_idx == -1)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("No tracks added"));

                    text.Clear();
                    
                    break;
                }

                var track = await _client.Lavalink.GetTrackAsync(options[_idx]);
                tracksList.Add(track);
                text.AppendLine($"{Formatter.Bold(Formatter.Sanitize(track.Title))} by {Formatter.Bold(track.Author)})");
            }
            
            player.Queue.AddRange(tracksList);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(ctx.Member.GetDefaultEmbedTemplateForUser()
                    .WithAuthor("Added tracks to queue", null, ctx.Client.CurrentUser.AvatarUrl)
                    .WithDescription(string.IsNullOrWhiteSpace($"{text}") ? "Nothing" : $"{text}")));
        }

        [SlashCommand("view", "Check the queue of current playing session")]
        public async Task MusicQueueCheckCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            
            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueuedPlayerAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueueIsNotEmptyAsync(ctx)) return;

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);
            var queue = player.Queue;
            
            StringBuilder text = new();
            var idx = 0;

            foreach (var track in queue)
            {
                text.AppendLine($"{idx} - {Formatter.MaskedUrl($"{Formatter.Bold(track.Title)} by {Formatter.Bold(track.Author)}", new Uri(track.Source ?? "https://example.com"))}");
                ++idx;
            }

            var pages = ctx.Client.GetInteractivity().GeneratePagesInEmbed($"{text}", SplitType.Line,
                ctx.Member.GetDefaultEmbedTemplateForUser()
                    .WithAuthor("Queued tracks", null, ctx.Client.CurrentUser.AvatarUrl));

            await ctx.Interaction.SendPaginatedResponseAsync(false, ctx.Member, pages, asEditResponse: true);
        }

        [SlashCommand("shuffle", "Shuffle the queue")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task MusicQueueShuffleCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            
            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueuedPlayerAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueueIsNotEmptyAsync(ctx)) return;

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);
            
            player.Queue.Shuffle();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Shuffled the queue"));
        }

        [SlashCommand("clear", "Clear the queue")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task MusicQueueClearCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            
            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueuedPlayerAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueueIsNotEmptyAsync(ctx)) return;

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            player.Queue.Clear();
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Cleared all tracks of the queue"));
        }
        
        [SlashCommand("remove", "Remove a track from the queue")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task MusicQueueRemoveCommand(InteractionContext ctx, 
            [Option("index", "Index to remove from 0 (first track)")] long index)
        {
            await ctx.DeferAsync();
            
            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueuedPlayerAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueueIsNotEmptyAsync(ctx)) return;
            
            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (index < 0 || index >= player.Queue.Count)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Invalid index"));

                return;
            }

            var posInt = Convert.ToInt32(index);
            
            var track = player.Queue[posInt];
            player.Queue.RemoveAt(posInt);
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Removed track at index {index}: {Formatter.Bold(Formatter.Sanitize(track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Author))}"));           
        }

        [SlashCommand("remove_range", "Remove a range of tracks from the queue")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task MusicQueueRemoveRangeCommand(InteractionContext ctx,
            [Option("start_index", "Starting index to remove from 0 (first track)")] long startIndex,
            [Option("end_index", "Ending index to remove from 0 (first track)")] long endIndex)
        {
            await ctx.DeferAsync();
            
            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueuedPlayerAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueueIsNotEmptyAsync(ctx)) return;

            var startIndexInt = Convert.ToInt32(startIndex);
            var endIndexInt = Convert.ToInt32(endIndex);

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (startIndexInt < 0 || endIndexInt >= player.Queue.Count || startIndexInt > endIndexInt)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Invalid index"));

                return;
            }

            StringBuilder text = new();

            for (var p = startIndexInt; p <= endIndexInt; ++p)
            {
                var track = player.Queue[p];
                text.AppendLine($"{Formatter.Bold(Formatter.Sanitize(track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Author))}");
            }
            
            player.Queue.RemoveRange(startIndexInt, endIndexInt - startIndexInt + 1);

            var pages = ctx.Client.GetInteractivity().GeneratePagesInEmbed($"{text}", SplitType.Line, ctx.Member.GetDefaultEmbedTemplateForUser()
                .WithAuthor("Deleted tracks", null, ctx.Client.CurrentUser.AvatarUrl));

            await ctx.Interaction.SendPaginatedResponseAsync(false, ctx.Member, pages, asEditResponse: true);
        }

        [SlashCommand("make_unique", "Remove duplicating tracks from the list")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task MusicQueueRemoveRangeCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            
            if (!await MusicModuleUtils.EnsureUserInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureClientInVoiceAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueuedPlayerAsync(ctx)) return;
            if (!await MusicModuleUtils.EnsureQueueIsNotEmptyAsync(ctx)) return;
            
            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            player.Queue.Distinct();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Removed duplicating tracks with same source from the queue"));
        }
    }
}
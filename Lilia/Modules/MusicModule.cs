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
using Lilia.Services;

namespace Lilia.Modules;

public enum MusicSourceChoice
{
    [ChoiceName("soundcloud")]
    SoundCloud,
    
    [ChoiceName("youtube")]
    YouTube,
    
    [ChoiceName("raw")]
    None
}

[SlashCommandGroup("music", "Music commands")]
public class MusicModule : ApplicationCommandModule
{
    [SlashCommandGroup("playback", "Music playback commands")]
    public class MusicPlaybackModule : ApplicationCommandModule
    {
        private LiliaClient _client;

        public MusicPlaybackModule(LiliaClient client)
        {
            _client = client;
        }

        [SlashCommand("join", "Join voice channel")]
        public async Task MusicPlaybackJoinCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var _ = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id) ??
                    await _client.Lavalink.JoinAsync<QueuedLavalinkPlayer>(ctx.Guild.Id,
                        ctx.Member.VoiceState.Channel.Id, true);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Done establishing the connection"));
        }

        [SlashCommand("leave", "Leave voice channel and clear the queue")]
        public async Task MusicPlaybackLeaveCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Left already"));

                return;
            }

            await player.DisconnectAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Left the voice channel"));
        }

        [SlashCommand("play", "Play queued tracks")]
        public async Task MusicPlaybackPlayCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

            if (player.Queue.IsEmpty)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("The queue is empty"));

                return;
            }

            var track = player.Queue.Dequeue();

            await player.PlayAsync(track, false);

            _client.Lavalink.TrackStarted += async (_, e) =>
            {
                var currentTrack = e.Player.CurrentTrack;

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent(
                        $"Now playing: {Formatter.Bold(Formatter.Sanitize(currentTrack?.Title ?? "Unknown"))} by {Formatter.Bold(Formatter.Sanitize(currentTrack?.Author ?? "Unknown"))}\n" +
                        "You should pin this message for playing status"));
            };

            _client.Lavalink.TrackStuck += async (_, e) =>
            {
                var currentTrack = e.Player.CurrentTrack;

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"Track stuck: {Formatter.Bold(Formatter.Sanitize(currentTrack?.Title ?? "Unknown"))} by {Formatter.Bold(Formatter.Sanitize(currentTrack?.Author ?? "Unknown"))}"));
            };
        }

        [SlashCommand("now_playing", "Check now playing track")]
        public async Task MusicPlaybackNowPlayingCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

            var track = player.CurrentTrack;

            if (track == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Unable to get the track, maybe because I am not playing anything"));

                return;
            }

            var art = await _client.ArtworkService.ResolveAsync(track);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(ctx.Member.GetDefaultEmbedTemplateForUser()
                    .WithAuthor("Currently playing track", null, ctx.Client.CurrentUser.AvatarUrl)
                    .WithThumbnail(art?.OriginalString ?? "")
                    .AddField("Title", Formatter.Sanitize(track.Title), true)
                    .AddField("Author", Formatter.Sanitize(track.Author), true)
                    .AddField("Source", Formatter.Sanitize(track.Source ?? "Unknown"))
                    .AddField("Playback position", $"{player.Position.RelativePosition:g}/{track.Duration:g}", true)
                    .AddField("Is looping", $"{player.IsLooping}", true)));
        }

        [SlashCommand("skip", "Skip playing track")]
        public async Task MusicPlaybackSkipCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

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

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

            await player.StopAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Stopped this session, the queue will be cleaned"));
        }

        [SlashCommand("pause", "Pause this session")]
        public async Task MusicPlaybackPauseCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

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

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

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

        [SlashCommand("loop", "Loop playing track")]
        public async Task MusicPlaybackLoopCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

            var track = player.CurrentTrack;

            if (track == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Unable to get the track, maybe because I am not playing anything"));

                return;
            }

            player.IsLooping = true;

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Looping the track: {Formatter.Bold(Formatter.Sanitize(track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Author))}"));
        }
    }

    [SlashCommandGroup("queue", "Queue commands")]
    public class MusicQueueModule : ApplicationCommandModule
    {
        private LiliaClient _client;

        public MusicQueueModule(LiliaClient client)
        {
            _client = client;
        }

        [SlashCommand("add_playlist", "Add a playlist to queue")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task MusicQueueAddPlaylistCommand(InteractionContext ctx,
            [Option("playlist_url", "Music query")]
            string playlistUrl)
        {
            await ctx.DeferAsync();

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

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

        [SlashCommand("add", "Add a track to queue")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task MusicQueueAddCommand(InteractionContext ctx,
            [Option("query", "Music query")] string query,
            [Option("source", "Music source")] MusicSourceChoice sourceChoice = MusicSourceChoice.YouTube)
        {
            await ctx.DeferAsync();

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            Enum.TryParse(sourceChoice.ToString(), out SearchMode searchMode);

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

            var track = await _client.Lavalink.GetTrackAsync(query, searchMode);

            if (track == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Unable to get the track or the queue is empty"));

                return;
            }

            if (track.IsLiveStream)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("You are not allowed to queue a stream"));

                return;
            }

            player.Queue.Add(track);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Enqueued {Formatter.Bold(Formatter.Sanitize(track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Author))}"));
        }

        [SlashCommand("undo", "In case you enqueue the wrong track")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task MusicQueueUndoCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

            if (player.Queue.IsEmpty)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("The queue is empty"));

                return;
            }

            var len = player.Queue.Count;
            var track = player.Queue[len - 1];

            player.Queue.RemoveAt(len - 1);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Removed previously added track - {Formatter.Bold(Formatter.Sanitize(track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Author))}"));
        }

        [SlashCommand("view", "Check the queue of current playing session")]
        public async Task MusicQueueCheckCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

            var queue = player.Queue;

            if (queue.IsEmpty)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("The queue is empty"));

                return;
            }

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

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

            if (player.Queue.IsEmpty)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("The queue is empty"));

                return;
            }

            player.Queue.Shuffle();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Shuffled the queue"));
        }

        [SlashCommand("remove", "Remove a track from the queue")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task MusicQueueRemoveCommand(InteractionContext ctx, 
            [Option("index", "Index to remove from 0 (first track)")] long index)
        {
            await ctx.DeferAsync();

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

            if (player.Queue.IsEmpty)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("The queue is empty"));

                return;
            }

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

        [SlashCommand("clear", "Clear the queue")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task MusicQueueClearCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

            if (player.Queue.IsEmpty)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("The queue is empty"));

                return;
            }

            player.Queue.Clear();
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Cleared all tracks of the queue"));
        }

        [SlashCommand("remove_range", "Remove a range of tracks from the queue")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task MusicQueueRemoveRangeCommand(InteractionContext ctx,
            [Option("start_index", "Starting index to remove from 0 (first track)")] long startIndex,
            [Option("end_index", "Ending index to remove from 0 (first track)")] long endIndex)
        {
            await ctx.DeferAsync();

            var startIndexInt = Convert.ToInt32(startIndex);
            var endIndexInt = Convert.ToInt32(endIndex);

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

            if (player.Queue.IsEmpty)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("The queue is empty"));

                return;
            }

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
        
        [SlashCommand("remove_dupes", "Remove duplicating tracks from the list")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task MusicQueueRemoveRangeCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Join a voice channel please"));

                return;
            }

            var player = _client.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I am not connected to the voice channel"));

                return;
            }

            if (player.Queue.IsEmpty)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("The queue is empty"));

                return;
            }

            player.Queue.Distinct();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Removed duplicating tracks with same source from the queue"));
        }
    }
}
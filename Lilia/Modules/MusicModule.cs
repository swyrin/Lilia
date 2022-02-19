using System;
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
    private LiliaClient _client;

    public MusicModule(LiliaClient client)
    {
        _client = client;
    }
    
    [SlashCommand("join", "Join voice channel")]
    public async Task MusicJoinCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        
        if (ctx.Member.VoiceState?.Channel.Id == null)
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

    [SlashCommand("leave", "Leave voice channel, also destroys the queue")]
    public async Task LeaveCommand(InteractionContext ctx)
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

    [SlashCommand("enqueue", "Enqueue a track")]
    [SlashRequireUserPermissions(Permissions.ManageGuild)]
    public async Task EnqueueCommand(InteractionContext ctx,
        [Option("query", "Music query")]
        string query,
        [Option("source", "Music source")]
        MusicSourceChoice sourceChoice = MusicSourceChoice.YouTube)
    {
        await ctx.DeferAsync();
        
        if (ctx.Member.VoiceState?.Channel.Id == null)
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
            .WithContent($"Enqueued {Formatter.Bold(track.Title)} by {Formatter.Bold(track.Author)}"));
    }
    
    [SlashCommand("unqueue", "In case you enqueue the wrong track")]
    [SlashRequireUserPermissions(Permissions.ManageGuild)]
    public async Task DequeueCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        
        if (ctx.Member.VoiceState?.Channel.Id == null)
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
            .WithContent($"Removed previously added track from the queue - {Formatter.Bold(track.Title)} by {Formatter.Bold(track.Author)}"));
    }
    
    [SlashCommand("play", "Play queued tracks")]
    public async Task PlayCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        
        if (ctx.Member.VoiceState?.Channel.Id == null)
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

        player.Queue.TryDequeue(out var track);
        
        if (track == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Unable to get the track, maybe because the queue is empty"));
            
            return;
        }

        await player.PlayAsync(track);
        
        _client.Lavalink.TrackStarted += async (_, e) =>
        {
            var currentTrack = e.Player.CurrentTrack;
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Now playing {Formatter.Bold(currentTrack?.Title ?? "Unknown")} by {Formatter.Bold(currentTrack?.Author ?? "Unknown")}\n" +
                             "You should pin this message for playing status"));
        };
    }

    [SlashCommand("now_playing", "Check now playing track")]
    public async Task NowPlayingCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        
        if (ctx.Member.VoiceState?.Channel.Id == null)
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

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(ctx.Member.GetDefaultEmbedTemplateForUser()
                .WithAuthor("Currently playing track", null, ctx.Client.CurrentUser.AvatarUrl)
                .AddField("Title", track.Title, true)
                .AddField("Author", track.Author, true)
                .AddField("Source", track.Source ?? "Unknown")
                .AddField("Playback position", $"{track.Position:g}/{track.Duration:g}")));
    }

    [SlashCommand("queue", "Check the queue of current playing session")]
    public async Task ViewQueueCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        
        if (ctx.Member.VoiceState?.Channel.Id == null)
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
        int pos = 1;

        foreach (var track in queue)
        {
            text.AppendLine($"{pos} - {Formatter.MaskedUrl($"{Formatter.Bold(track.Title)} by {Formatter.Bold(track.Author)}", new Uri(track.Source ?? "https://example.com"))}");
            ++pos;
        }

        await ctx.Interaction.SendPaginatedResponseAsync(false, ctx.Member,
            ctx.Client.GetInteractivity().GeneratePagesInEmbed(text.ToString(), SplitType.Line,
                ctx.Member.GetDefaultEmbedTemplateForUser()), asEditResponse: true);
    }
}
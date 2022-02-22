using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Lavalink4NET.Events;
using Lavalink4NET.Player;
using Lilia.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Lilia.Modules.Utils;

public class MusicModuleUtils
{
    private readonly InteractionContext _ctx;

    public MusicModuleUtils(InteractionContext ctx)
    {
        _ctx = ctx;
    }
    
    public static async Task<bool> EnsureUserInVoiceAsync(InteractionContext ctx)
    {
        if (ctx.Member.VoiceState?.Channel != null) return true;
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Join a voice channel please"));

        return false;
    }

    public static async Task<bool> EnsureClientInVoiceAsync(InteractionContext ctx)
    {
        var player = ctx.Services.GetService<LiliaClient>()?.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

        if (player != null) return true;
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("I am not in a voice channel right now"));

        return false;
    }

    public static async Task<bool> EnsureQueueIsNotEmptyAsync(InteractionContext ctx)
    {
        var player = ctx.Services.GetService<LiliaClient>()?.Lavalink.GetPlayer<QueuedLavalinkPlayer>(ctx.Guild.Id);

        if (!player.Queue.IsEmpty) return true;
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("The queue is empty"));

        return false;
    }

    public async Task OnTrackStarted(object sender, TrackStartedEventArgs e)
    {
        var currentTrack = e.Player.CurrentTrack;

        await _ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent(
                $"Now playing: {Formatter.Bold(Formatter.Sanitize(currentTrack?.Title ?? "Unknown"))} by {Formatter.Bold(Formatter.Sanitize(currentTrack?.Author ?? "Unknown"))}\n" +
                "You should pin this message for playing status"));
    }

    public async Task OnTrackStuck(object sender, TrackStuckEventArgs e)
    {
        var currentTrack = e.Player.CurrentTrack;

        await _ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Track stuck: {Formatter.Bold(Formatter.Sanitize(currentTrack?.Title ?? "Unknown"))} by {Formatter.Bold(Formatter.Sanitize(currentTrack?.Author ?? "Unknown"))}"));
    }

    public async Task OnTrackEnd(object sender, TrackEventArgs e)
    {
        var currentTrack = e.Player.CurrentTrack;

        await _ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Finished playing: {Formatter.Bold(Formatter.Sanitize(currentTrack?.Title ?? "Unknown"))} by {Formatter.Bold(Formatter.Sanitize(currentTrack?.Author ?? "Unknown"))}\n" +
                         "If you see this instead of Playing {{next track}}, probably this is the end of the queue"));
    }

    public async Task OnTrackException(object sender, TrackExceptionEventArgs e)
    {
        var currentTrack = e.Player.CurrentTrack;

        await _ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"There was an error playing: {Formatter.Bold(Formatter.Sanitize(currentTrack?.Title ?? "Unknown"))} by {Formatter.Bold(Formatter.Sanitize(currentTrack?.Author ?? "Unknown"))}")
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Error message")
                .WithDescription(e.ErrorMessage)));
    }
}
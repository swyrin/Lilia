using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Lavalink4NET.Events;
using Lavalink4NET.Player;

namespace Lilia.Modules.Utils;

public class MusicModuleUtils
{
    private readonly SocketInteraction _interaction;
    private readonly LavalinkPlayer _player;

    public MusicModuleUtils(SocketInteraction interaction, LavalinkPlayer player)
    {
        _interaction = interaction;
        _player = player;
    }

    public async Task<bool> EnsureNormalPlayerAsync()
    {
        if (_player is not QueuedLavalinkPlayer) return true;

        await _interaction.ModifyOriginalResponseAsync(x =>
            x.Content = "You have to use the normal player to use this command");

        return false;
    }

    public async Task<bool> EnsureQueuedPlayerAsync()
    {
        if (_player is QueuedLavalinkPlayer) return true;

        await _interaction.ModifyOriginalResponseAsync(x =>
            x.Content = "You have to use the queued player to use this command");

        return false;
    }

    public async Task<bool> EnsureUserInVoiceAsync()
    {
        if (((SocketGuildUser) _interaction.User).VoiceState != null) return true;

        await _interaction.ModifyOriginalResponseAsync(x =>
            x.Content = "Join a voice channel please");

        return false;
    }

    public async Task<bool> EnsureClientInVoiceAsync()
    {
        if (_player != null) return true;

        await _interaction.ModifyOriginalResponseAsync(x =>
            x.Content = "I am not in a voice channel now");

        return false;
    }

    public async Task<bool> EnsureQueueIsNotEmptyAsync()
    {
        if (!((QueuedLavalinkPlayer) _player).Queue.IsEmpty) return true;

        await _interaction.ModifyOriginalResponseAsync(x =>
            x.Content = "The queue is empty now");

        return false;
    }

    public async Task OnTrackStarted(object _, TrackStartedEventArgs e)
    {
        var currentTrack = e.Player.CurrentTrack;

        await _interaction.ModifyOriginalResponseAsync(x =>
            x.Content =
                $"Now playing: {Format.Bold(Format.Sanitize(currentTrack?.Title ?? "Unknown"))} by {Format.Bold(Format.Sanitize(currentTrack?.Author ?? "Unknown"))}\n" +
                "You should pin this message for playing status");
    }

    public async Task OnTrackStuck(object _, TrackStuckEventArgs e)
    {
        var currentTrack = e.Player.CurrentTrack;

        await _interaction.ModifyOriginalResponseAsync(x =>
            x.Content =
                $"Track stuck: {Format.Bold(Format.Sanitize(currentTrack?.Title ?? "Unknown"))} by {Format.Bold(Format.Sanitize(currentTrack?.Author ?? "Unknown"))}\n");
    }

    public async Task OnTrackEnd(object _, TrackEventArgs e)
    {
        var currentTrack = e.Player.CurrentTrack;

        await _interaction.ModifyOriginalResponseAsync(x =>
            x.Content =
                $"Finished playing: {Format.Bold(Format.Sanitize(currentTrack?.Title ?? "Unknown"))} by {Format.Bold(Format.Sanitize(currentTrack?.Author ?? "Unknown"))}\n" +
                "You should pin this message for playing status");
    }

    public async Task OnTrackException(object _, TrackExceptionEventArgs e)
    {
        var currentTrack = e.Player.CurrentTrack;

        await _interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content =
                    $"There was an error playing: {Format.Bold(Format.Sanitize(currentTrack?.Title ?? "Unknown"))} by {Format.Bold(Format.Sanitize(currentTrack?.Author ?? "Unknown"))}";
                x.Embed = new EmbedBuilder()
                    .WithTitle("Error message")
                    .WithDescription(e.ErrorMessage)
                    .Build();
            }
        );
    }
}
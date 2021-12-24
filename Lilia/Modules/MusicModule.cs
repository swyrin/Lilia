using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;

namespace Lilia.Modules;

public class MusicModule : BaseCommandModule
{
    [Command("join")]
    [Description("Jumps right to the voice channel that you are in.")]
    public async Task JoinVoiceCommand(CommandContext ctx)
    {
        DiscordChannel channel = ctx.Member.VoiceState?.Channel;

        if (channel == null)
        {
            await ctx.RespondAsync("Join a voice channel please.");
            return;
        }

        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        if (connection != null && connection.Channel == channel && connection.IsConnected)
        {
            await ctx.RespondAsync("Already there.");
            return;
        }

        await node.ConnectAsync(channel);
        await (await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id)).SetDeafAsync(true, "Self deaf");
        await ctx.RespondAsync("Connected.");
    }

    [Command("leave")]
    [Description("Leave currently joined voice channel")]
    public async Task LeaveVoiceCommand(CommandContext ctx)
    {
        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        if (!connection.IsConnected)
        {
            await ctx.RespondAsync("Already left.");
            return;
        }

        await connection.DisconnectAsync();
        await ctx.RespondAsync("Disconnected.");
    }

    [Command("play")]
    [Aliases("p")]
    [Description("Play music, that's it. Soundcloud tracks must prepend 'sc-' before the search.")]
    public async Task PlayCommand(CommandContext ctx,
        [Description("Text to search, can be an url.")] [RemainingText] string search)
    {
        if (string.IsNullOrEmpty(search))
        {
            await ctx.RespondAsync("Did you forget something?");
            return;
        }

        DiscordChannel channel = ctx.Member.VoiceState?.Channel;

        if (channel == null)
        {
            await ctx.RespondAsync("Join a voice channel please.");
            return;
        }

        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);
        LavalinkLoadResult loadResult;

        bool canConstructUri = Uri.TryCreate(search, UriKind.Absolute, out Uri result);
        
        if (canConstructUri) loadResult = await node.Rest.GetTracksAsync(result);
        else
        {
            if (search.ToLower().StartsWith("sc-")) loadResult = await node.Rest.GetTracksAsync($"{search.Replace("sc-", "")}", LavalinkSearchType.SoundCloud);
            else loadResult = await node.Rest.GetTracksAsync(search);
        }

        LavalinkTrack track = loadResult.Tracks.First();
        await connection.PlayAsync(track);
        await ctx.RespondAsync($"Now playing {track.Uri}");
    }

    [Command("nowplaying")]
    [Aliases("np", "now")]
    [Description("Check currently played track")]
    public async Task NowPlayingCommand(CommandContext ctx)
    {
        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        LavalinkTrack track = connection.CurrentState.CurrentTrack;
        
        if (!connection.IsConnected)
        {
            await ctx.RespondAsync("I did not join the voice chat");
            return;
        }

        if (connection.CurrentState.CurrentTrack == null)
        {
            await ctx.RespondAsync("Not playing anything");
            return;
        }

        if (connection.CurrentState.CurrentTrack.IsStream)
        {
            await ctx.RespondAsync($"I am playing a stream from: {connection.CurrentState.CurrentTrack.Uri}");
            return;
        }

        StringBuilder text = new StringBuilder();
        text.AppendLine($"{Formatter.Bold("Track")}: {track.Title}");
        text.AppendLine($"{Formatter.Bold("Author")}: {track.Author}");
        text.AppendLine($"{Formatter.Bold("Position")}: {connection.CurrentState.PlaybackPosition:g}/{track.Length:g}");
        text.AppendLine($"{Formatter.Bold("Source")}: {track.Uri}");

        await ctx.RespondAsync(text.ToString());

    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;

namespace Lilia.Modules;

public class MusicModule : BaseCommandModule
{
    [Command("join")]
    [RequireOwner]
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
            
        await node.ConnectAsync(channel);
        await (await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id)).SetDeafAsync(true, "Self deaf");
        await ctx.RespondAsync("Connected.");
    }
        
    [Command("leave")]
    [RequireOwner]
    public async Task LeaveVoiceCommand(CommandContext ctx)
    {
        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);
            
        await connection.DisconnectAsync();
        await ctx.RespondAsync("Disconnected.");
    }

    [Command("radio")]
    [RequireOwner]
    public async Task TransmitRadioCommand(CommandContext ctx, string search = "https://listen.moe/stream")
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
        LavalinkLoadResult loadResult;
            
        if (search.ToLower().Contains("youtube")) loadResult = await node.Rest.GetTracksAsync(search);
        else if (search.ToLower().Contains("soundcloud")) loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.SoundCloud);
        else loadResult = await node.Rest.GetTracksAsync(new Uri(search, UriKind.RelativeOrAbsolute));

        LavalinkTrack track = loadResult.Tracks.First();
        await connection.PlayAsync(track);
        await ctx.RespondAsync($"Now playing {track.Uri}");
    }
}
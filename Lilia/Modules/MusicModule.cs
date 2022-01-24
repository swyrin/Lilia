using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace Lilia.Modules;

[SlashCommandGroup("music", "Commands related to music playbacks")]
public class MusicModule : ApplicationCommandModule
{
    [SlashCommand("join", "Join a voice channel")]
    public async Task JoinVoiceCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        DiscordChannel channel = ctx.Member.VoiceState?.Channel;

        if (channel == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Join a voice channel please"));

            return;
        }

        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        if (connection is {IsConnected: true})
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I have joined already, maybe in other voice channel"));

            return;
        }

        await node.ConnectAsync(channel);
        await (await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id)).SetDeafAsync(true, "Self deaf");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Connected to your current voice channel"));
    }
    
    [SlashCommand("summon", "Jump to your current voice channel")]
    [SlashRequirePermissions(Permissions.MoveMembers)]
    public async Task SummonVoiceCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        
        DiscordChannel channel = ctx.Member.VoiceState?.Channel;

        if (channel == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Join a voice channel please"));

            return;
        }

        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        if (connection is {IsConnected: true})
        {
            if (connection.Channel != channel)
            {
                await connection.DisconnectAsync();
                await node.ConnectAsync(channel);
                await (await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id)).SetDeafAsync(true, "Self deaf");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Jumped to your new voice channel"));
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("We are in the same voice channel"));
            }
        }
    }

    [SlashCommand("leave", "Leave the voice chanel")]
    public async Task LeaveVoiceCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        if (connection == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I am not in any voice channel right now"));

            return;
        }

        await connection.DisconnectAsync();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Left the voice channel"));
    }

    [SlashCommand("play", "Play music")]
    public async Task PlayCommand(InteractionContext ctx,
        [Option("search", "Search query")] string search,
        [Choice("youtube", "youtube")]
        [Choice("soundcloud", "soundcloud")]
        [Choice("stream", "stream")]
        [Option("mode", "Search mode")]
        string mode = "youtube")
    {
        await ctx.DeferAsync();
        
        DiscordChannel channel = ctx.Member.VoiceState?.Channel;
        if (channel == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Join a voice channel please"));

            return;
        }

        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        if (connection == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I am not in any voice channel right now"));

            return;
        }

        LavalinkLoadResult loadResult;

        switch (mode)
        {
            case "stream":
            {
                bool canConstructUri = Uri.TryCreate(search, UriKind.Absolute, out Uri result);
                if (canConstructUri)
                    loadResult = await node.Rest.GetTracksAsync(result);
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Invalid URI provided"));

                    return;
                }

                break;
            }
            case "soundcloud":
                loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.SoundCloud);
                break;
            default:
                loadResult = await node.Rest.GetTracksAsync(search);
                break;
        }


        if (!loadResult.Tracks.Any())
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I am unable to find a suitable track from your search"));

            return;
        }

        LavalinkTrack track = loadResult.Tracks.First();
        await connection.PlayAsync(track);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Now playing the track {track.Uri}"));
    }
    
    [SlashCommand("nowplaying", "Check playing track")]
    public async Task NowPlayingCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        if (connection == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I am not in any channel right now"));

            return;
        }

        LavalinkTrack track = connection.CurrentState.CurrentTrack;

        if (connection.CurrentState.CurrentTrack == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I am not playing anything"));

            return;
        }

        if (connection.CurrentState.CurrentTrack.IsStream)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"I am playing a stream from: {connection.CurrentState.CurrentTrack.Uri}"));

            return;
        }

        StringBuilder text = new StringBuilder();
        text.AppendLine($"{Formatter.Bold("Track title")}: {track.Title}");
        text.AppendLine($"{Formatter.Bold("Author/Uploader")}: {track.Author}");
        text.AppendLine(
            $"{Formatter.Bold("Playback position")}: {connection.CurrentState.PlaybackPosition:g}/{track.Length:g}");
        text.AppendLine($"{Formatter.Bold("Source")}: {track.Uri}");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent(text.ToString()));
    }

    [SlashCommand("controls", "Show playback controls")]
    [SlashRequirePermissions(Permissions.KickMembers | Permissions.BanMembers)]
    public async Task ShowControlsCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        if (connection == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I am not in any channel right now"));

            return;
        }

        LavalinkTrack track = connection.CurrentState.CurrentTrack;

        if (connection.CurrentState.CurrentTrack == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I am not playing anything"));

            return;
        }

        DiscordButtonComponent pauseBtn = new DiscordButtonComponent(ButtonStyle.Primary, "pauseBtn", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":pause_button:")));
        DiscordButtonComponent stopBtn = new DiscordButtonComponent(ButtonStyle.Primary, "stopBtn", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":stop_button:")));
        DiscordButtonComponent resumeBtn = new DiscordButtonComponent(ButtonStyle.Primary, "resumeBtn", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":play_pause:")));

        DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddComponents(pauseBtn, resumeBtn, stopBtn)
            .WithContent("Here's the controls"));

        var result = await message.WaitForButtonAsync();

        if (result.TimedOut)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Timed out"));

            return;
        }

        string id = result.Result.Id;

        switch (id)
        {
            case "pauseBtn":
                await connection.PauseAsync();
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Pausing"));

                break;
            case "stopBtn":
                await connection.StopAsync();
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Stopping"));

                break;
            case "resumeBtn":
                await connection.ResumeAsync();
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Resuming"));

                break;
        }
    }
}
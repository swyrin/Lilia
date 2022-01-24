using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Lilia.Database;
using Lilia.Database.Extensions;
using Lilia.Database.Models;
using Lilia.Services;

namespace Lilia.Modules;

[SlashCommandGroup("music", "Commands related to music playbacks")]
public class MusicModule : ApplicationCommandModule
{
    private LiliaClient _client;
    private LiliaDbContext _dbCtx;
    
    public MusicModule(LiliaClient client)
    {
        this._client = client;
        this._dbCtx = client.Database.GetContext();
    }
    
    private async Task OnChannelUpdated(DiscordClient sender, ChannelUpdateEventArgs e)
    {
        LavalinkExtension lavalinkExtension = sender.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection guildConnection = node.GetGuildConnection(e.Guild);

        if (e.ChannelAfter == guildConnection.Channel)
        {
            if (e.ChannelAfter.Users.Count == 1 && guildConnection.IsConnected)
            {
                await guildConnection.PauseAsync();
            }
        }
    }

    private async Task EnsureUserInVoiceAsync(InteractionContext ctx)
    {
        DiscordChannel channel = ctx.Member.VoiceState?.Channel;
        
        if (channel == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Join a voice channel please"));
        }
    }

    private async Task EnsureClientInVoiceAsync(InteractionContext ctx)
    {
        DiscordChannel channel = ctx.Member.VoiceState?.Channel;
        
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
    
    [SlashCommand("join", "Join a voice channel")]
    public async Task JoinVoiceCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        await this.EnsureUserInVoiceAsync(ctx);
        await this.EnsureClientInVoiceAsync(ctx);

        ctx.Client.ChannelUpdated += OnChannelUpdated;
    }
    
    [SlashCommand("summon", "Jump to your current voice channel")]
    [SlashRequirePermissions(Permissions.MoveMembers)]
    public async Task SummonVoiceCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        
        await this.EnsureUserInVoiceAsync(ctx);
        
        DiscordChannel channel = ctx.Member.VoiceState?.Channel;

        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        if (connection is {IsConnected: true})
        {
            if (connection.Channel != channel)
            {
                // I don't know
                ctx.Client.ChannelUpdated -= OnChannelUpdated;
                
                await connection.DisconnectAsync();
                await node.ConnectAsync(channel);
                await (await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id)).SetDeafAsync(true, "Self deaf");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Jumped to your new voice channel"));
                ctx.Client.ChannelUpdated += OnChannelUpdated;
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

        ctx.Client.ChannelUpdated -= OnChannelUpdated;
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

        await this.EnsureUserInVoiceAsync(ctx);
        await this.EnsureClientInVoiceAsync(ctx);
        
        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

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

    [SlashCommand("playqueue", "Play whole queue")]
    public async Task PlayQueueCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        await this.EnsureUserInVoiceAsync(ctx);
        await this.EnsureClientInVoiceAsync(ctx);

        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection guildConnection = node.GetGuildConnection(ctx.Guild);
        
        DbGuild guild = this._dbCtx.GetOrCreateGuildRecord(ctx.Guild);
        
        List<string> tracks = string.IsNullOrWhiteSpace(guild.Queue) 
            ? new() 
            : guild.Queue.Split("||").ToList();
            
        List<string> trackNames = string.IsNullOrWhiteSpace(guild.QueueWithNames)
            ? new()
            : guild.QueueWithNames.Split("||").ToList();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Processing"));
        
        if (tracks.Any() && trackNames.Any())
        {
            while (tracks.Any())
            {
                string id = tracks.First();
                string full = trackNames.First();
                
                tracks.RemoveAt(0);
                trackNames.RemoveAt(0);
                
                // In case we are at the end of the list
                // known issue I guess?
                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(full))
                {
                    guild.Queue = string.Empty;
                    guild.QueueWithNames = string.Empty;
                    break;
                }
                
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"Processing {full}"));

                LavalinkTrack track = await guildConnection.Node.Rest.DecodeTrackAsync(id);
                await guildConnection.PlayAsync(track);
                
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"Now playing {full}, use \"/nowplaying\" for more details"));

                // wait for completion
                while (guildConnection.CurrentState.CurrentTrack != null) await Task.Delay(2000);
                
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"Finished playing {full}"));
            }
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("The queue is now empty"));
        }
        else
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("The queue is empty"));
        }
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
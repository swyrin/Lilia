using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
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

    private TaskCompletionSource<bool> _playbackWaiter;

    public MusicModule(LiliaClient client)
    {
        this._client = client;
        this._dbCtx = client.Database.GetContext();
    }

    public override async Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx)
    {
        DiscordChannel channel = ctx.Member.VoiceState?.Channel;
        
        if (channel == null)
        {
            await ctx.Channel.SendMessageAsync("Join a voice channel please");
            return false;
        }
        
        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        if (connection is {IsConnected: true})
        {
            if (connection.Channel == channel) return true;
            
            await ctx.Channel.SendMessageAsync("Looks like we are not in the same voice channel");
            return false;
        }
        
        await node.ConnectAsync(channel);
        await (await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id)).SetDeafAsync(true, "Self deaf");
        return true;
    }

    [SlashCommand("summon", "Jump to your new voice channel")]
    [SlashRequirePermissions(Permissions.MoveMembers)]
    public async Task SummonVoiceCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        DiscordChannel channel = ctx.Member.VoiceState?.Channel;

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

    [SlashCommand("play", "Play a single track")]
    public async Task PlayCommand(InteractionContext ctx,
        [Option("input", "Search query")] string input,
        [Choice("Youtube", "Youtube")]
        [Choice("SoundCloud", "SoundCloud")]
        [Choice("Plain", "Plain")]
        [Option("source", "Place to search")]
        string source = "Youtube")
    {
        await ctx.DeferAsync();

        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        LavalinkLoadResult loadResult;

        if (source.Equals("Plain"))
        {
            bool canConstructUri = Uri.TryCreate(input, UriKind.Absolute, out Uri result);
            if (canConstructUri) loadResult = await connection.GetTracksAsync(result);
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Invalid URL provided"));

                return;
            }
        }
        else
        {
            loadResult = await connection.GetTracksAsync(input, Enum.Parse<LavalinkSearchType>(source));    
        }
        
        if (!loadResult.Tracks.Any())
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I can not find a suitable track from your search"));

            return;
        }

        LavalinkTrack track = loadResult.Tracks.First();

        this._dbCtx.GetOrCreateGuildRecord(ctx.Guild).IsPlaying = true;
        await this._dbCtx.SaveChangesAsync();
        
        await this.GenericPlayMusicAsync(ctx, track.Uri, TimeSpan.Zero);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Now playing: {Formatter.Bold(track.Title)} by {Formatter.Bold(track.Author)}"));
    }

    [SlashCommand("playqueue", "Play whole queue")]
    public async Task PlayQueueCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        List<string> tracks; ;
        
        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        do
        {
            DbGuild guild = this._dbCtx.GetOrCreateGuildRecord(ctx.Guild);
            
            tracks = string.IsNullOrWhiteSpace(guild.QueueItem)
                ? new()
                : guild.QueueItem.Split("||").ToList();
            
            if (tracks.Any())
            {
                string[] item = tracks.First().Split("|");
                string name = item[0];
                string url = item[1];

                // In case we are at the end of the list
                // known issue I guess?
                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(url))
                {
                    guild.QueueItem = string.Empty;
                    await this._dbCtx.SaveChangesAsync();
                    continue;
                }

                tracks.RemoveAt(0);
                guild.QueueItem = string.Join("||", tracks);
                guild.IsPlaying = true;
                await this._dbCtx.SaveChangesAsync();

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"Now playing: {name}"));
                
                LavalinkTrack track = (await connection.GetTracksAsync(url, LavalinkSearchType.Plain)).Tracks.First();
                await this.GenericPlayMusicAsync(ctx, track.Uri, TimeSpan.Zero);
            }
        } while (tracks.Any());
    }

    [SlashCommand("nowplaying", "Check now playing track")]
    public async Task NowPlayingCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        LavalinkTrack track = connection.CurrentState.CurrentTrack;

        if (track == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I am not playing anything"));

            return;
        }

        if (track.IsStream)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"I am playing a stream from: {track.Uri}"));

            return;
        }

        StringBuilder text = new StringBuilder();
        text.AppendLine($"{Formatter.Bold("Title")}: {track.Title}");
        text.AppendLine($"{Formatter.Bold("Uploader")}: {track.Author}");
        text.AppendLine($"{Formatter.Bold("Position")}: {connection.CurrentState.PlaybackPosition:g}/{track.Length:g}");
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

        if (connection.CurrentState.CurrentTrack == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I am not playing anything"));

            return;
        }

        DiscordButtonComponent toggleBtn = new DiscordButtonComponent(ButtonStyle.Primary, "toggleBtn", "Play/Pause", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":play_pause:")));
        DiscordButtonComponent stopBtn = new DiscordButtonComponent(ButtonStyle.Primary, "stopBtn", "Stop", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":stop_button:")));
        DiscordButtonComponent loopBtn = new DiscordButtonComponent(ButtonStyle.Primary, "loopBtn", "Loop", true, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":arrows_counterclockwise:")));

        DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddComponents(toggleBtn, stopBtn, loopBtn)
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
            case "toggleBtn":
                DbGuild g = this._dbCtx.GetOrCreateGuildRecord(ctx.Guild);

                if (g.IsPlaying)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Pausing"));
                    
                    g.LastPlayedPosition = connection.CurrentState.PlaybackPosition;
                    g.LastPlayedTrack = connection.CurrentState.CurrentTrack.Uri.ToString();
                    g.IsPlaying = !g.IsPlaying;
                    
                    await connection.PauseAsync();
                    await this._dbCtx.SaveChangesAsync();
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Resuming"));

                    string track = g.LastPlayedTrack;
                    TimeSpan pos = g.LastPlayedPosition;
                    
                    g.LastPlayedPosition = TimeSpan.Zero;
                    g.LastPlayedTrack = string.Empty;
                    g.IsPlaying = !g.IsPlaying;
                    
                    await this._dbCtx.SaveChangesAsync();
                    await this.GenericPlayMusicAsync(ctx, new Uri(track), pos);
                }

                break;
            case "stopBtn":
                await connection.StopAsync();
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Stopping"));

                break;
        }
    }
    private async Task GenericPlayMusicAsync(InteractionContext ctx, Uri uri , TimeSpan start)
    {
        if (this._playbackWaiter == null || this._playbackWaiter.Task.IsCompleted)
            this._playbackWaiter = new TaskCompletionSource<bool>();
        
        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);

        LavalinkTrack track = (await connection.GetTracksAsync(uri)).Tracks.First();
        await connection.PlayPartialAsync(track, start, track.Length);
        
        this._playbackWaiter.Task.WaitAsync(track.Length.Subtract(start).Add(TimeSpan.FromSeconds(5))).ConfigureAwait(false).GetAwaiter().GetResult();
        this._playbackWaiter.SetResult(true);
        
        connection.PlaybackFinished += (_, _) =>
        {
            this._playbackWaiter.SetResult(false);
            this._playbackWaiter.SetCanceled();
            return Task.CompletedTask;
        };
    }
}
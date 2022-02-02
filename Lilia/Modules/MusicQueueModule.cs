using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Extensions;
using Lilia.Database.Models;
using Lilia.Services;
using MoreLinq;

namespace Lilia.Modules;

[SlashCommandGroup("queue", "Commands for music queue manipulation")]
public class MusicQueueModule : ApplicationCommandModule
{
    private LiliaDbContext _dbCtx;
    private LiliaClient _client;

    public MusicQueueModule(LiliaClient client)
    {
        this._client = client;
        this._dbCtx = client.Database.GetContext();
    }

    [SlashCommand("add", "Add a song to queue")]
    public async Task AddToQueueCommand(InteractionContext ctx,
        [Option("input", "Search input")] string input,
        [Choice("Youtube", "Youtube")]
        [Choice("SoundCloud", "SoundCloud")]
        [Option("source", "Place to search")]
        string source = "Youtube")
    {
        await ctx.DeferAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Processing"));
        
        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkLoadResult loadResult = await node.Rest.GetTracksAsync(input, Enum.Parse<LavalinkSearchType>(source));

        if (!loadResult.Tracks.Any())
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I can't find suitable tracks from your search"));

            return;
        }

        int index = 0;

        StringBuilder tracksStr = new();
        List<string> tracks = new();
        List<string> trackNames = new();

        foreach (var lavalinkTrack in loadResult.Tracks)
        {
            // ignore livestream, radio, etc.
            if (lavalinkTrack.IsStream) continue;
            
            ++index;
            string wot = $"{Formatter.Bold(lavalinkTrack.Title)} by {Formatter.Bold(lavalinkTrack.Author)}";
            tracksStr.AppendLine($"Track #{index}: {Formatter.MaskedUrl(wot, lavalinkTrack.Uri)}");
            tracks.Add(lavalinkTrack.Uri.ToString());
            trackNames.Add(wot);
        }

        if (string.IsNullOrWhiteSpace(tracksStr.ToString()))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Nothing found. Maybe you gave me the radio link and I ignored it."));

            return;
        }

        DiscordEmbedBuilder embedBuilder = ctx.Member.GetDefaultEmbedTemplateForMember()
            .WithTitle($"Found {index} tracks with query {input} from {source}");

        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        
        IEnumerable<Page> pages = interactivity.GeneratePagesInEmbed(tracksStr.ToString(), SplitType.Line, embedBuilder);
        await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.Member, pages);
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Type the position of the track you want to get (1 to {index}, inclusive)"));

        int i = 1;

        var res = await interactivity.WaitForMessageAsync(x =>
        {
            bool canConvert = int.TryParse(x.Content, out i);
            return canConvert && 1 <= i && i <= index;
        });

        if (res.TimedOut)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Timed out"));

            return;
        }

        DbGuild guild = this._dbCtx.GetOrCreateGuildRecord(ctx.Guild);

        guild.QueueItem += $"{trackNames[i - 1]}|{tracks[i - 1]}||";

        await this._dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Added track selection #{i} to queue."));
    }

    [SlashCommand("search", "Search a song, usually used to check if the input is correct")]
    public async Task SearchTaskCommand(InteractionContext ctx,
        [Option("input", "Search input")] string input,
        [Choice("Youtube", "Youtube")]
        [Choice("SoundCloud", "SoundCloud")]
        [Option("source", "Place to search")]
        string source = "Youtube")
    {
        await ctx.DeferAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Processing"));
        
        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkLoadResult loadResult = await node.Rest.GetTracksAsync(input, Enum.Parse<LavalinkSearchType>(source));

        if (!loadResult.Tracks.Any())
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I can't find suitable tracks from your search"));

            return;
        }

        int index = 0;
        StringBuilder tracksStr = new();

        foreach (var lavalinkTrack in loadResult.Tracks)
        {
            string wot = $"{Formatter.Bold(lavalinkTrack.Title)} by {Formatter.Bold(lavalinkTrack.Author)}";
            if (lavalinkTrack.IsStream) wot = "This is a stream";
            
            ++index;
            tracksStr.AppendLine($"Track #{index}: {Formatter.MaskedUrl(wot, lavalinkTrack.Uri)}");
        }

        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        
        DiscordEmbedBuilder embedBuilder = ctx.Member.GetDefaultEmbedTemplateForMember()
            .WithTitle($"Found {index} tracks with query {input} from {source}");

        IEnumerable<Page> pages = interactivity.GeneratePagesInEmbed(tracksStr.ToString(), SplitType.Line, embedBuilder);
        
        await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.Member, pages, asEditResponse: true);
    }

    [SlashCommand("addplaylist", "Add playlist tracks to queue")]
    public async Task AddPlaylistToQueueCommand(InteractionContext ctx,
        [Option("source", "Playlist source URL")] string source)
    {
        await ctx.DeferAsync();

        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkLoadResult loadResult = await node.Rest.GetTracksAsync(source, LavalinkSearchType.Plain);

        if (!loadResult.Tracks.Any())
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I am unable to find tracks from your playlist URL"));

            return;
        }

        DbGuild guild = this._dbCtx.GetOrCreateGuildRecord(ctx.Guild);

        StringBuilder tracksStr = new();
        StringBuilder queueItem = new();

        foreach (var track in loadResult.Tracks)
        {
            // ignore livestream, radio, etc.
            if (track.IsStream) continue;
            
            string wot = $"{Formatter.Bold(track.Title)} by {Formatter.Bold(track.Author)}";
            tracksStr.AppendLine(wot);
            queueItem.Append($"{wot}|{track.Uri}||");
        }
        
        if (string.IsNullOrWhiteSpace(tracksStr.ToString()))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Nothing found. Maybe you gave me the radio link and I ignored it."));

            return;
        }
        
        DiscordEmbedBuilder embedBuilder = ctx.Member.GetDefaultEmbedTemplateForMember()
            .WithTitle($"Found and added {loadResult.Tracks.Count()} tracks from provided playlist URL");
        
        guild.QueueItem += queueItem.ToString();
        await this._dbCtx.SaveChangesAsync();

        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        
        IEnumerable<Page> pages = interactivity.GeneratePagesInEmbed(tracksStr.ToString(), SplitType.Line, embedBuilder);
        await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.Member, pages, asEditResponse: true);
    }

    [SlashCommand("view", "View the queue of this server")]
    public async Task ViewQueueCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Processing"));

        DbGuild guild = this._dbCtx.GetOrCreateGuildRecord(ctx.Guild);
        
        List<string> tracks = string.IsNullOrWhiteSpace(guild.QueueItem)
            ? new()
            : guild.QueueItem.Split("||").ToList();

        if (tracks.Any())
        {
            StringBuilder names = new();

            int pos = 0;
            foreach (string track in tracks)
            {
                // should trigger at the end of the list
                if (string.IsNullOrWhiteSpace(track)) continue;
                
                string wot = track.Split('|').First();
                ++pos;
                names.AppendLine($"Track #{pos}: {wot}");
            }

            DiscordEmbedBuilder embedBuilder = ctx.Member.GetDefaultEmbedTemplateForMember()
                .WithTitle($"Queue of guild \"{ctx.Guild.Name}\" ({tracks.Count - 1} tracks)");

            InteractivityExtension interactivity = ctx.Client.GetInteractivity();

            IEnumerable<Page> pages = interactivity.GeneratePagesInEmbed(names.ToString(), SplitType.Line, embedBuilder);
            await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.Member, pages, asEditResponse: true);
        }
        else
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("The queue is empty"));
        }
    }

    [SlashCommand("remove", "Remove a track from the queue")]
    public async Task RemoveFromQueueCommand(InteractionContext ctx,
        [Option("position", "Position to delete. Use \"/queue view\" to check the position")] long position)
    {
        await ctx.DeferAsync();

        DbGuild guild = this._dbCtx.GetOrCreateGuildRecord(ctx.Guild);
        List<string> tracks = string.IsNullOrWhiteSpace(guild.QueueItem)
            ? new()
            : guild.QueueItem.Split("||").ToList();

        int pos = (int) position;
        
        if (tracks.Any())
        {
            string trackData = $"track #{position} -> {tracks[pos - 1].Split('|').First()}";
            
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Deleting {trackData}"));

            tracks.RemoveAt(pos - 1);
            guild.QueueItem = string.Join("||", tracks);
            await this._dbCtx.SaveChangesAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Done deleting {trackData}"));
        }
        else
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("The queue is empty"));
        }
    }

    [SlashCommand("clear", "Clear entire queue")]
    [SlashRequirePermissions(Permissions.ManageGuild)]
    public async Task ClearQueueCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        DbGuild guild = this._dbCtx.GetOrCreateGuildRecord(ctx.Guild);

        guild.QueueItem = string.Empty;

        await this._dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Done clearing the queue"));
    }

    [SlashCommand("shuffle", "Shuffle the queue")]
    [SlashRequirePermissions(Permissions.ManageGuild)]
    public async Task ShuffleQueueCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        DbGuild guild = this._dbCtx.GetOrCreateGuildRecord(ctx.Guild);

        List<string> tracks = string.IsNullOrWhiteSpace(guild.QueueItem)
            ? new()
            : guild.QueueItem.Split("||").ToList();

        guild.QueueItem = string.Join("||", tracks.Shuffle());

        await this._dbCtx.SaveChangesAsync();
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Done shuffling the queue"));
    }
}
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Extensions;
using Lilia.Database.Models;
using Lilia.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[SlashCommandGroup("queue", "Commands for queuing music")]
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
        [Option("input", "Search input")] string search,
        [Choice("Youtube", "Youtube")]
        [Choice("SoundCloud", "SoundCloud")]
        [Option("mode", "Determine place to search")] string mode = "Youtube")
    {
        await ctx.DeferAsync();

        LavalinkExtension lavalinkExtension = ctx.Client.GetLavalink();
        LavalinkNodeConnection node = lavalinkExtension.ConnectedNodes.Values.First();
        LavalinkLoadResult loadResult = await node.Rest.GetTracksAsync(search, Enum.Parse<LavalinkSearchType>(mode));

        if (!loadResult.Tracks.Any())
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I am unable to find suitable tracks from your search"));

            return;
        }
        else
        {
            int index = 0;

            StringBuilder tracksStr = new();
            List<string> tracks = new();
            List<string> trackNames = new();

            foreach (var lavalinkTrack in loadResult.Tracks.Take(10))
            {
                ++index;
                string wot = $"{Formatter.Bold(lavalinkTrack.Title)} by {Formatter.Bold(lavalinkTrack.Author)}";
                tracksStr.AppendLine($"Track #{index}: {Formatter.MaskedUrl(wot, lavalinkTrack.Uri)}");
                tracks.Add(lavalinkTrack.Identifier);
                trackNames.Add(wot);
            }

            DiscordEmbedBuilder embedBuilder = LiliaUtilities.GetDefaultEmbedTemplate(ctx.Member)
                .WithTitle($"Found {index} tracks with query {search} from {mode}");

            InteractivityExtension interactivity = ctx.Client.GetInteractivity();
            var p = interactivity.GeneratePagesInEmbed(tracksStr.ToString(), DSharpPlus.Interactivity.Enums.SplitType.Line, embedBuilder);

            DiscordWebhookBuilder whb = new DiscordWebhookBuilder();
            foreach (var page in p) whb.AddEmbed(page.Embed);

            await ctx.EditResponseAsync(whb);

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Enter the position of the track you want to get (1 to {index})"));

            int i = 1;

            var res = await interactivity.WaitForMessageAsync(msg =>
            {
                bool canConvert = int.TryParse(msg.Content, out i);
                return canConvert && 1 <= i && i <= index;
            });

            if (res.TimedOut)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Timed out"));

                return;
            }

            DbGuild guild = this._dbCtx.GetOrCreateGuildRecord(ctx.Member.Id);

            guild.Queue += $"{tracks[i - 1]}{(string.IsNullOrWhiteSpace(guild.Queue) ? "||" : string.Empty)}";
            guild.QueueWithNames += $"{trackNames[i - 1]}{(string.IsNullOrWhiteSpace(guild.QueueWithNames) ? "||" : string.Empty)}";

            await this._dbCtx.SaveChangesAsync();

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Added track selection #{i} to queue."));
        }
    }

    [SlashCommand("addplaylist", "Add songs in a playlist into queue")]
    public async Task AddPlaylistToQueueCommand(InteractionContext ctx,
        [Option("source", "Playlist URL")] string source)
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
        else
        {
            DbGuild guild = this._dbCtx.GetOrCreateGuildRecord(ctx.Member.Id);

            StringBuilder tracksStr = new();
            List<string> tracks = new();
            List<string> trackNames = new();

            foreach (var lavalinkTrack in loadResult.Tracks)
            {
                // don't you dare
                if (lavalinkTrack.IsStream) continue;

                string wot = $"{Formatter.Bold(lavalinkTrack.Title)} by {Formatter.Bold(lavalinkTrack.Author)}";
                tracks.Add(lavalinkTrack.Identifier);
                trackNames.Add(wot);
                tracksStr.AppendLine(wot);

                guild.Queue += $"{lavalinkTrack.Identifier}||";
                guild.QueueWithNames += $"{wot}||";
            }

            DiscordEmbedBuilder embedBuilder = LiliaUtilities.GetDefaultEmbedTemplate(ctx.Member)
                .WithTitle($"Found and added {loadResult.Tracks.Count()} tracks from provided playlist URL");

            InteractivityExtension interactivity = ctx.Client.GetInteractivity();
            var p = interactivity.GeneratePagesInEmbed(tracksStr.ToString(), DSharpPlus.Interactivity.Enums.SplitType.Line, embedBuilder);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("If you see \"This interaction failed\", just ignore it. I can not find a workaround :D"));

            await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, p, TimeSpan.FromMinutes(5), DSharpPlus.Interactivity.Enums.PaginationBehaviour.WrapAround);

            await this._dbCtx.SaveChangesAsync();
        }
    }

    [SlashCommand("view", "View this server's queue")]
    public async Task ViewQueueCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        DbGuild guild = this._dbCtx.GetOrCreateGuildRecord(ctx.Member.Id);
        List<string> tracks = string.IsNullOrWhiteSpace(guild.QueueWithNames) ? new() : guild.QueueWithNames.Split("||").ToList();

        if (tracks.Any())
        {
            StringBuilder names = new();

            int pos = 0;
            foreach (string track in tracks)
            {
                if (string.IsNullOrWhiteSpace(track)) continue;
                ++pos;
                names.AppendLine($"Track #{pos}: {track}");
            }

            DiscordEmbedBuilder embedBuilder = LiliaUtilities.GetDefaultEmbedTemplate(ctx.Member)
                .WithTitle($"Queue of guild \"{ctx.Guild.Name}\" ({tracks.Count - 1} tracks)");

            InteractivityExtension interactivity = ctx.Client.GetInteractivity();

            var p = interactivity.GeneratePagesInEmbed(names.ToString(), DSharpPlus.Interactivity.Enums.SplitType.Line, embedBuilder);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("If you see \"This interaction failed\", just ignore it. I can not find a workaround :D"));

            await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, p, TimeSpan.FromMinutes(5), DSharpPlus.Interactivity.Enums.PaginationBehaviour.WrapAround);
        } else
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("The queue is empty"));
        }
    }

    [SlashCommand("remove", "Delete a track from the queue")]
    public async Task RemoveFromQueueCommand(InteractionContext ctx,
        [Option("position", "Position to delete. Use \"/queue view\" to check")] long position)
    {
        await ctx.DeferAsync();

        int pos = (int) position;

        DbGuild guild = this._dbCtx.GetOrCreateGuildRecord(ctx.Member.Id);

        List<string> tracks = string.IsNullOrWhiteSpace(guild.Queue) ? new() : guild.Queue.Split("||").ToList();
        List<string> trackNames = string.IsNullOrWhiteSpace(guild.QueueWithNames) ? new() : guild.QueueWithNames.Split("||").ToList();

        if (tracks.Any() && trackNames.Any())
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Deleting track #{position} -> {trackNames[pos - 1]}"));

            tracks.RemoveAt(pos - 1);
            trackNames.RemoveAt(pos - 1);

            guild.Queue = string.Join("||", tracks);
            guild.QueueWithNames = string.Join("||", trackNames);

            await this._dbCtx.SaveChangesAsync();

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Done deleting track #{position}"));
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
        DbGuild guild = this._dbCtx.GetOrCreateGuildRecord(ctx.Member.Id);

        guild.Queue = string.Empty;
        guild.QueueWithNames = string.Empty;

        await this._dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Done clearing the queue"));
    }
}
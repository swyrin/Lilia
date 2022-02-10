using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Extensions;
using Lilia.Services;
using OsuSharp.Domain;
using OsuSharp.Exceptions;
using OsuSharp.Interfaces;

namespace Lilia.Modules;

public enum UserSearchChoice
{
    [ChoiceName("profile")] Profile,

    [ChoiceName("best_plays")] Best,

    [ChoiceName("first_place_plays")] First,

    [ChoiceName("recent_plays")] Recent
}

public enum ModeSearchChoice
{
    [ChoiceName("linked")] Linked,

    [ChoiceName("standard")] Osu,

    [ChoiceName("mania")] Mania,

    [ChoiceName("catch_the_beat")] Fruits,

    [ChoiceName("taiko")] Taiko
}

[SlashCommandGroup("osu", "osu! commands for Bancho server")]
public class OsuModule : ApplicationCommandModule
{
    private LiliaClient _client;
    private readonly LiliaDatabaseContext _dbCtx;
    private readonly IOsuClient _osuClient;

    public OsuModule(LiliaClient client, IOsuClient osuClient)
    {
        _client = client;
        _dbCtx = client.Database.GetContext();
        _osuClient = osuClient;
    }

    [SlashCommand("update", "Update your osu! profile information in my database")]
    public async Task SetOsuUsernameCommand(InteractionContext ctx,
        [Option("username", "Your osu! username")]
        string username,
        [Choice("standard", "Osu")]
        [Choice("taiko", "Taiko")]
        [Choice("catch", "Fruits")]
        [Choice("mania", "Mania")]
        [Option("mode", "osu! mode to link")]
        string mode = "Osu")
    {
        await ctx.DeferAsync(true);

        var dbUser = _dbCtx.GetOrCreateUserRecord(ctx.Member);

        dbUser.OsuUsername = username;
        dbUser.OsuMode = mode;

        _dbCtx.Update(dbUser);
        await _dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent(
                $"Successfully set your osu! username to {Formatter.Bold(username)} and osu! mode to {Formatter.Bold(mode)}"));
    }

    [SlashCommand("myinfo", "Get your linked data with me")]
    public async Task CheckMyProfileCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);
        var dbUser = _dbCtx.GetOrCreateUserRecord(ctx.Member);

        var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForMember()
            .AddField("Username",
                !string.IsNullOrWhiteSpace(dbUser.OsuUsername) ? dbUser.OsuUsername : "Not linked yet", true)
            .AddField("Default mode", !string.IsNullOrWhiteSpace(dbUser.OsuMode) ? dbUser.OsuMode : "Not linked yet",
                true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Here is your linked data with me")
            .AddEmbed(embedBuilder.Build()));
    }

    [SlashCommand("lookup", "Get a member's osu! profile information")]
    public async Task GetOsuProfileMentionCommand(InteractionContext ctx,
        [Option("user", "Someone in this Discord server")]
        DiscordUser discordUser,
        [Option("type", "Search type")] UserSearchChoice searchType = UserSearchChoice.Profile,
        [Option("mode", "Search mode")] ModeSearchChoice searchMode = ModeSearchChoice.Linked)
    {
        await ctx.DeferAsync();

        var member = (DiscordMember) discordUser;
        var dbUser = _dbCtx.GetOrCreateUserRecord(member);

        if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("That user doesn't exist in my database"));

            return;
        }

        GameMode omode;
        if (searchMode == ModeSearchChoice.Linked) Enum.TryParse(dbUser.OsuMode, out omode);
        else omode = FromModeChoice(searchMode);

        await GenericProcessing(ctx, dbUser.OsuUsername, searchType, omode);
    }

    [SlashCommand("profile", "Get osu! profile from provided username")]
    public async Task GetOsuProfileStringCommand(InteractionContext ctx,
        [Option("username", "Someone's osu! username")]
        string username,
        [Option("type", "Search type")] UserSearchChoice searchType = UserSearchChoice.Profile,
        [Option("mode", "Search mode")] ModeSearchChoice searchMode = ModeSearchChoice.Osu)
    {
        await ctx.DeferAsync();

        if (searchMode == ModeSearchChoice.Linked)
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Invalid mode"));

        await GenericProcessing(ctx, username, searchType, FromModeChoice(searchMode));
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "[osu!] View profile of this user")]
    public async Task GetOsuProfileContextMenu(ContextMenuContext ctx)
    {
        await ctx.DeferAsync(true);

        try
        {
            DiscordUser member = ctx.TargetMember;
            var dbUser = _dbCtx.GetOrCreateUserRecord(member);

            if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("That user doesn't exist in my database"));

                return;
            }

            Enum.TryParse(dbUser.OsuMode, out GameMode omode);

            var osuUser = await _osuClient.GetUserAsync(dbUser.OsuUsername, omode);
            var sb = new StringBuilder();

            sb
                .AppendLine($"{Formatter.Bold("Join Date")}: {osuUser.JoinDate:d}")
                .AppendLine(
                    $"{Formatter.Bold("Country")}: {osuUser.Country.Name} :flag_{osuUser.Country.Code.ToLower()}:")
                .AppendLine(
                    $"{Formatter.Bold("Total Score")}: {osuUser.Statistics.TotalScore} - {osuUser.Statistics.RankedScore} ranked score")
                .AppendLine(
                    $"{Formatter.Bold("PP")}: {osuUser.Statistics.Pp}pp (Country: #{osuUser.Statistics.CountryRank} - Global: #{osuUser.Statistics.GlobalRank})")
                .AppendLine($"{Formatter.Bold("Accuracy")}: {osuUser.Statistics.HitAccuracy}%")
                .AppendLine(
                    $"{Formatter.Bold("Level")}: {osuUser.Statistics.UserLevel.Current} ({osuUser.Statistics.UserLevel.Progress}%)")
                .AppendLine(
                    $"{Formatter.Bold("Play Count")}: {osuUser.Statistics.PlayCount} with {osuUser.Statistics.PlayTime:g} of play time")
                .AppendLine(
                    $"{Formatter.Bold("Current status")}: {(osuUser.IsOnline ? "Online" : "Offline/Invisible")}");

            var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForMember()
                .WithAuthor(
                    $"{osuUser.Username}'s osu! profile {(osuUser.IsSupporter ? DiscordEmoji.FromName(ctx.Client, ":heart:").ToString() : string.Empty)}",
                    $"https://osu.ppy.sh/users/{osuUser.Id}")
                .AddField("Basic Information", sb.ToString())
                .WithThumbnail(osuUser.AvatarUrl.ToString());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"osu! profile of user {osuUser.Username} with mode {Formatter.Bold(omode.ToString())}")
                .AddEmbed(embedBuilder.Build()));
        }
        catch (ApiException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong when sending the request"));
        }
        catch (OsuDeserializationException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong when parsing the response"));
        }
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "[osu!] View most recent scores of this user")]
    public async Task GetOsuRecentContextMenu(ContextMenuContext ctx)
    {
        await ctx.DeferAsync(true);

        try
        {
            DiscordUser member = ctx.TargetMember;
            var dbUser = _dbCtx.GetOrCreateUserRecord(member);

            if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("That user doesn't exist in my database"));

                return;
            }

            Enum.TryParse(dbUser.OsuMode, out GameMode omode);

            var scores =
                await GetOsuScoresAsync(dbUser.OsuUsername, omode, UserSearchChoice.Recent);
            var osuUser = await _osuClient.GetUserAsync(dbUser.OsuUsername, omode);

            if (!scores.Any())
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("I found nothing, sorry"));
            }
            else
            {
                var builder = new DiscordFollowupMessageBuilder();
                var sb = new StringBuilder();
                var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForMember()
                    .WithAuthor($"Most recent score of {osuUser.Username} in mode {omode}",
                        $"https://osu.ppy.sh/users/{osuUser.Id}", osuUser.AvatarUrl.ToString());

                foreach (var score in scores)
                {
                    var beatmap = await _osuClient.GetBeatmapAsync(score.Beatmap.Id);

                    sb
                        .AppendLine($"{Formatter.Bold("Map link")}: {beatmap.Url}")
                        .AppendLine(
                            $"{Formatter.Bold("Score")}: {score.TotalScore} - Global rank #{score.GlobalRank.GetValueOrDefault()}, Country rank #{score.CountryRank.GetValueOrDefault()}")
                        .AppendLine($"{Formatter.Bold("Ranking")}: {score.Rank}")
                        .AppendLine(
                            $"{Formatter.Bold("Accuracy")}: {Math.Round(score.Accuracy * 100, 2, MidpointRounding.ToEven)}%")
                        .AppendLine($"{Formatter.Bold("Combo")}: {score.MaxCombo}x/{beatmap.MaxCombo}x")
                        .AppendLine(
                            $"{Formatter.Bold("Hit Count")}: [{score.Statistics.Count300}/{score.Statistics.Count100}/{score.Statistics.Count50}/{score.Statistics.CountMiss}]")
                        .AppendLine(
                            $"{Formatter.Bold("PP")}: {score.PerformancePoints}pp -> {Math.Round(score.Weight.PerformancePoints, 2, MidpointRounding.ToEven)}pp {Math.Round(score.Weight.Percentage, 2, MidpointRounding.ToEven)}% weighted")
                        .AppendLine($"{Formatter.Bold("Submission Time")}: {score.CreatedAt:f}");

                    embedBuilder
                        .AddField(
                            $"{score.Beatmapset.Artist} - {score.Beatmapset.Title} [{beatmap.Version}]{(score.Mods.Any() ? $" +{Formatter.Bold(string.Join(string.Empty, score.Mods))}" : string.Empty)}",
                            sb.ToString());

                    sb.Clear();
                }

                builder.AddEmbed(embedBuilder.Build());
                await ctx.FollowUpAsync(builder);
            }
        }
        catch (ApiException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong when sending the request"));
        }
        catch (OsuDeserializationException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong when parsing the response"));
        }
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "[osu!] View the best play of this user")]
    public async Task GetOsuBestContextMenu(ContextMenuContext ctx)
    {
        await ctx.DeferAsync(true);

        try
        {
            DiscordUser member = ctx.TargetMember;
            var dbUser = _dbCtx.GetOrCreateUserRecord(member);

            if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("That user doesn't exist in my database"));

                return;
            }

            Enum.TryParse(dbUser.OsuMode, out GameMode omode);

            var scores =
                await GetOsuScoresAsync(dbUser.OsuUsername, omode, UserSearchChoice.Best);
            var osuUser = await _osuClient.GetUserAsync(dbUser.OsuUsername, omode);

            if (!scores.Any())
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("I found nothing, sorry"));
            }
            else
            {
                var builder = new DiscordFollowupMessageBuilder();
                var sb = new StringBuilder();
                var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForMember()
                    .WithAuthor($"Best score of {osuUser.Username} in mode {omode}",
                        $"https://osu.ppy.sh/users/{osuUser.Id}", osuUser.AvatarUrl.ToString());

                foreach (var score in scores)
                {
                    var beatmap = await _osuClient.GetBeatmapAsync(score.Beatmap.Id);

                    sb
                        .AppendLine($"{Formatter.Bold("Map link")}: {beatmap.Url}")
                        .AppendLine(
                            $"{Formatter.Bold("Score")}: {score.TotalScore} - Global rank #{score.GlobalRank.GetValueOrDefault()}, Country rank #{score.CountryRank.GetValueOrDefault()}")
                        .AppendLine($"{Formatter.Bold("Ranking")}: {score.Rank}")
                        .AppendLine(
                            $"{Formatter.Bold("Accuracy")}: {Math.Round(score.Accuracy * 100, 2, MidpointRounding.ToEven)}%")
                        .AppendLine($"{Formatter.Bold("Combo")}: {score.MaxCombo}x/{beatmap.MaxCombo}x")
                        .AppendLine(
                            $"{Formatter.Bold("Hit Count")}: [{score.Statistics.Count300}/{score.Statistics.Count100}/{score.Statistics.Count50}/{score.Statistics.CountMiss}]")
                        .AppendLine(
                            $"{Formatter.Bold("PP")}: {score.PerformancePoints}pp -> {Math.Round(score.Weight.PerformancePoints, 2, MidpointRounding.ToEven)}pp {Math.Round(score.Weight.Percentage, 2, MidpointRounding.ToEven)}% weighted")
                        .AppendLine($"{Formatter.Bold("Submission Time")}: {score.CreatedAt:f}");

                    embedBuilder
                        .AddField(
                            $"{score.Beatmapset.Artist} - {score.Beatmapset.Title} [{beatmap.Version}]{(score.Mods.Any() ? $" +{Formatter.Bold(string.Join(string.Empty, score.Mods))}" : string.Empty)}",
                            sb.ToString());

                    sb.Clear();
                }

                builder.AddEmbed(embedBuilder.Build());
                await ctx.FollowUpAsync(builder);
            }
        }
        catch (ApiException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong when sending the request"));
        }
        catch (OsuDeserializationException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong when parsing the response"));
        }
    }

    private static GameMode FromModeChoice(ModeSearchChoice choice)
    {
        return choice switch
        {
            ModeSearchChoice.Fruits => GameMode.Fruits,
            ModeSearchChoice.Mania => GameMode.Mania,
            ModeSearchChoice.Taiko => GameMode.Taiko,
            ModeSearchChoice.Osu => GameMode.Osu,
            _ => GameMode.Osu
        };
    }

    private async Task<IReadOnlyList<IScore>> GetOsuScoresAsync(string username, GameMode omode,
        UserSearchChoice searchType, bool includeFails = false, int count = 1)
    {
        var osuUser = await _osuClient.GetUserAsync(username, omode);
        IReadOnlyList<IScore> scores = new List<IScore>();

        switch (searchType)
        {
            case UserSearchChoice.Best:
                scores = await _osuClient.GetUserScoresAsync(osuUser.Id, ScoreType.Best, false, omode, count);
                break;
            case UserSearchChoice.Recent:
                scores = await _osuClient.GetUserScoresAsync(osuUser.Id, ScoreType.Recent, includeFails, omode,
                    count);
                break;
            case UserSearchChoice.First:
                scores = await _osuClient.GetUserScoresAsync(osuUser.Id, ScoreType.Firsts, false, omode, count);
                break;
            case UserSearchChoice.Profile:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(searchType), searchType, null);
        }

        return scores;
    }

    private async Task GenericProcessing(InteractionContext ctx, string username, UserSearchChoice searchType,
        GameMode omode)
    {
        try
        {
            var osuUser = await _osuClient.GetUserAsync(username, omode);

            if (searchType == UserSearchChoice.Profile)
            {
                var sb = new StringBuilder();

                sb
                    .AppendLine($"{Formatter.Bold("Join Date")}: {osuUser.JoinDate:d}")
                    .AppendLine(
                        $"{Formatter.Bold("Country")}: {osuUser.Country.Name} :flag_{osuUser.Country.Code.ToLower()}:")
                    .AppendLine(
                        $"{Formatter.Bold("Total Score")}: {osuUser.Statistics.TotalScore} - {osuUser.Statistics.RankedScore} ranked score")
                    .AppendLine(
                        $"{Formatter.Bold("PP")}: {osuUser.Statistics.Pp}pp (Country: #{osuUser.Statistics.CountryRank} - Global: #{osuUser.Statistics.GlobalRank})")
                    .AppendLine($"{Formatter.Bold("Accuracy")}: {osuUser.Statistics.HitAccuracy}%")
                    .AppendLine(
                        $"{Formatter.Bold("Level")}: {osuUser.Statistics.UserLevel.Current} ({osuUser.Statistics.UserLevel.Progress}%)")
                    .AppendLine(
                        $"{Formatter.Bold("Play Count")}: {osuUser.Statistics.PlayCount} with {osuUser.Statistics.PlayTime:g} of play time")
                    .AppendLine(
                        $"{Formatter.Bold("Current status")}: {(osuUser.IsOnline ? "Online" : "Offline/Invisible")}");

                var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForMember()
                    .WithAuthor(
                        $"{osuUser.Username}'s osu! profile {(osuUser.IsSupporter ? DiscordEmoji.FromName(ctx.Client, ":heart:").ToString() : string.Empty)}",
                        $"https://osu.ppy.sh/users/{osuUser.Id}")
                    .AddField("Basic Information", sb.ToString())
                    .WithThumbnail(osuUser.AvatarUrl.ToString());

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent(
                        $"osu! profile of user {osuUser.Username} with mode {Formatter.Bold(omode.ToString())}")
                    .AddEmbed(embedBuilder.Build()));
            }
            else
            {
                var interactivity = ctx.Client.GetInteractivity();

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("How many scores do you want to get?"));

                var r = 1;

                var res = await interactivity.WaitForMessageAsync(m =>
                {
                    var canDo = int.TryParse(m.Content, out r);
                    return r is >= 1 and <= 100;
                });

                if (res.TimedOut)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Timed out"));

                    return;
                }

                var includeFails = false;

                if (searchType == UserSearchChoice.Recent)
                {
                    var yesBtn = new DiscordButtonComponent(ButtonStyle.Success, "yesBtn", "Yes please!");
                    var noBtn = new DiscordButtonComponent(ButtonStyle.Danger, "noBtn", "Probably not!");

                    var failAsk = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Do you want to include fail scores?")
                        .AddComponents(yesBtn, noBtn));

                    var btnRes = await failAsk.WaitForButtonAsync(ctx.Member);

                    if (btnRes.TimedOut)
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                            .WithContent("Timed out"));

                        return;
                    }

                    includeFails = btnRes.Result.Id == "yesBtn";
                }

                var scores = await GetOsuScoresAsync(username, omode, searchType, includeFails, r);

                if (!scores.Any())
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent("I found nothing, sorry"));
                }
                else
                {
                    var builder = new DiscordFollowupMessageBuilder();
                    var sb = new StringBuilder();
                    var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForMember()
                        .WithAuthor(
                            $"{(searchType == UserSearchChoice.Best ? "Best" : "Recent")} score(s) of {osuUser.Username} in mode {omode}",
                            $"https://osu.ppy.sh/users/{osuUser.Id}", osuUser.AvatarUrl.ToString());

                    foreach (var score in scores)
                    {
                        var beatmap = await _osuClient.GetBeatmapAsync(score.Beatmap.Id);

                        sb
                            .AppendLine($"{Formatter.Bold("Map link")}: {beatmap.Url}")
                            .AppendLine(
                                $"{Formatter.Bold("Score")}: {score.TotalScore} - Global rank #{score.GlobalRank.GetValueOrDefault()}, Country rank #{score.CountryRank.GetValueOrDefault()}")
                            .AppendLine($"{Formatter.Bold("Ranking")}: {score.Rank}")
                            .AppendLine(
                                $"{Formatter.Bold("Accuracy")}: {Math.Round(score.Accuracy * 100, 2, MidpointRounding.ToEven)}%")
                            .AppendLine($"{Formatter.Bold("Combo")}: {score.MaxCombo}x/{beatmap.MaxCombo}x")
                            .AppendLine(
                                $"{Formatter.Bold("Hit Count")}: [{score.Statistics.Count300}/{score.Statistics.Count100}/{score.Statistics.Count50}/{score.Statistics.CountMiss}]")
                            .AppendLine(
                                $"{Formatter.Bold("PP")}: {score.PerformancePoints}pp -> {Math.Round(score.Weight.PerformancePoints, 2, MidpointRounding.ToEven)}pp {Math.Round(score.Weight.Percentage, 2, MidpointRounding.ToEven)}% weighted")
                            .AppendLine($"{Formatter.Bold("Submission Time")}: {score.CreatedAt:f}");

                        embedBuilder
                            .AddField(
                                $"{score.Beatmapset.Artist} - {score.Beatmapset.Title} [{beatmap.Version}]{(score.Mods.Any() ? $" +{Formatter.Bold(string.Join(string.Empty, score.Mods))}" : string.Empty)}",
                                sb.ToString());

                        sb.Clear();
                    }

                    builder.AddEmbed(embedBuilder.Build());
                    builder.WithContent(
                        $"{(searchType == UserSearchChoice.Best ? "Best" : "Recent")} scores of user {osuUser.Username}");
                    await ctx.FollowUpAsync(builder);
                }
            }
        }
        catch (ApiException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong when sending the request"));
        }
        catch (OsuDeserializationException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong when parsing the response"));
        }
    }
}
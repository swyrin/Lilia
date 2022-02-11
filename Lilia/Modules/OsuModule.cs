using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Extensions;
using Lilia.Services;
using OsuSharp.Domain;
using OsuSharp.Exceptions;
using OsuSharp.Interfaces;

namespace Lilia.Modules;

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
    public async Task SetSelfOsuUsernameCommand(InteractionContext ctx,
        [Option("username", "Your osu! username")]
        string username,
        [Option("mode", "Mode")]
        UserProfileSearchMode searchUserProfileSearchMode = UserProfileSearchMode.Osu)
    {
        await ctx.DeferAsync(true);

        if (searchUserProfileSearchMode == UserProfileSearchMode.Linked)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Invalid mode"));

            return;
        }

        var dbUser = _dbCtx.GetOrCreateUserRecord(ctx.Member);
        dbUser.OsuUsername = username;
        dbUser.OsuMode = FromModeChoice(searchUserProfileSearchMode).ToString();

        _dbCtx.Update(dbUser);
        await _dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Successfully set your osu! username to {Formatter.Bold(username)} and osu! mode to {Formatter.Bold(searchUserProfileSearchMode.GetName())}"));
    }

    [SlashCommand("forceupdate", "Update an user's osu! profile information in my database")]
    [SlashRequirePermissions(Permissions.ManageGuild)]
    public async Task SetMemberOsuUsernameCommand(InteractionContext ctx,
        [Option("user", "User to update, should be an user from your guild")]
        DiscordUser user,
        [Option("username", "osu! username")]
        string username,
        [Option("mode", "Mode")]
        UserProfileSearchMode searchUserProfileSearchMode = UserProfileSearchMode.Osu)
    {
        await ctx.DeferAsync(true);

        if (searchUserProfileSearchMode == UserProfileSearchMode.Linked)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Invalid mode"));

            return;
        }

        var dbUser = _dbCtx.GetOrCreateUserRecord(user);
        dbUser.OsuUsername = username;
        dbUser.OsuMode = FromModeChoice(searchUserProfileSearchMode).ToString();

        _dbCtx.Update(dbUser);
        await _dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Successfully set your osu! username to {Formatter.Bold(username)} and osu! mode to {Formatter.Bold(searchUserProfileSearchMode.GetName())}"));
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
        DiscordUser user,
        [Option("type", "Search type")]
        UserProfileSearchType profileSearchType = UserProfileSearchType.Profile,
        [Option("mode", "Search mode")]
        UserProfileSearchMode searchUserProfileSearchMode = UserProfileSearchMode.Linked)
    {
        await ctx.DeferAsync();

        var dbUser = _dbCtx.GetOrCreateUserRecord(user);

        if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("That user doesn't exist in my database"));

            return;
        }

        GameMode omode;
        if (searchUserProfileSearchMode == UserProfileSearchMode.Linked) Enum.TryParse(dbUser.OsuMode, out omode);
        else omode = FromModeChoice(searchUserProfileSearchMode);

        await GenericOsuProcessing(ctx, dbUser.OsuUsername, profileSearchType, omode);
    }

    [SlashCommand("profile", "Get osu! profile from provided username")]
    public async Task GetOsuProfileStringCommand(InteractionContext ctx,
        [Option("username", "Someone's osu! username")]
        string username,
        [Option("type", "Search type")]
        UserProfileSearchType profileSearchType = UserProfileSearchType.Profile,
        [Option("mode", "Search mode")]
        UserProfileSearchMode searchUserProfileSearchMode = UserProfileSearchMode.Osu)
    {
        await ctx.DeferAsync();

        if (searchUserProfileSearchMode == UserProfileSearchMode.Linked)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Invalid mode"));

            return;
        }

        await GenericOsuProcessing(ctx, username, profileSearchType, FromModeChoice(searchUserProfileSearchMode));
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "osu! - Get profile")]
    public async Task GetOsuProfileContextMenu(ContextMenuContext ctx)
    {
        await ctx.DeferAsync(true);

        DiscordUser member = ctx.TargetMember;
        var dbUser = _dbCtx.GetOrCreateUserRecord(member);

        if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("That user doesn't exist in my database"));

            return;
        }

        Enum.TryParse(dbUser.OsuMode, out GameMode omode);

        await GenericOsuProcessing(ctx, dbUser.OsuUsername, UserProfileSearchType.Profile, omode);
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "osu! - Get latest score")]
    public async Task GetOsuRecentContextMenu(ContextMenuContext ctx)
    {
        await ctx.DeferAsync(true);

        DiscordUser member = ctx.TargetMember;
        var dbUser = _dbCtx.GetOrCreateUserRecord(member);

        if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("That user doesn't exist in my database"));

            return;
        }

        Enum.TryParse(dbUser.OsuMode, out GameMode omode);

        await GenericOsuProcessing(ctx, dbUser.OsuUsername, UserProfileSearchType.Recent, omode);
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "osu! - Get best play")]
    public async Task GetOsuBestContextMenu(ContextMenuContext ctx)
    {
        await ctx.DeferAsync(true);

        DiscordUser member = ctx.TargetMember;
        var dbUser = _dbCtx.GetOrCreateUserRecord(member);

        if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("That user doesn't exist in my database"));

            return;
        }

        Enum.TryParse(dbUser.OsuMode, out GameMode omode);

        await GenericOsuProcessing(ctx, dbUser.OsuUsername, UserProfileSearchType.Best, omode); 
    }

    private static GameMode FromModeChoice(UserProfileSearchMode choice)
    {
        return choice switch
        {
            UserProfileSearchMode.Fruits => GameMode.Fruits,
            UserProfileSearchMode.Mania => GameMode.Mania,
            UserProfileSearchMode.Taiko => GameMode.Taiko,
            UserProfileSearchMode.Osu => GameMode.Osu,
            _ => GameMode.Osu
        };
    }

    private async Task<IReadOnlyList<IScore>> GetOsuScoresAsync(string username, GameMode omode, UserProfileSearchType profileSearchType, bool includeFails = false, int count = 1)
    {
        var osuUser = await _osuClient.GetUserAsync(username, omode);
        IReadOnlyList<IScore> scores = new List<IScore>();

        switch (profileSearchType)
        {
            case UserProfileSearchType.Best:
                scores = await _osuClient.GetUserScoresAsync(osuUser.Id, ScoreType.Best, false, omode, count);
                break;
            case UserProfileSearchType.Recent:
                scores = await _osuClient.GetUserScoresAsync(osuUser.Id, ScoreType.Recent, includeFails, omode,
                    count);
                break;
            case UserProfileSearchType.First:
                scores = await _osuClient.GetUserScoresAsync(osuUser.Id, ScoreType.Firsts, false, omode, count);
                break;
            case UserProfileSearchType.Profile:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(profileSearchType), profileSearchType, null);
        }

        return scores;
    }

    private async Task GenericOsuProcessing(BaseContext ctx, string username, UserProfileSearchType profileSearchType, GameMode omode)
    {
        try
        {
            var isContextMenu = ctx is ContextMenuContext;
            var osuUser = await _osuClient.GetUserAsync(username, omode);

            if (profileSearchType == UserProfileSearchType.Profile)
            {
                StringBuilder sb = new StringBuilder();

                sb
                    .AppendLine($"{Formatter.Bold("Join Date")}: {osuUser.JoinDate:d}")
                    .AppendLine($"{Formatter.Bold("Country")}: {osuUser.Country.Name} :flag_{osuUser.Country.Code.ToLower()}:")
                    .AppendLine($"{Formatter.Bold("Total Score")}: {osuUser.Statistics.TotalScore} - {osuUser.Statistics.RankedScore} ranked score")
                    .AppendLine($"{Formatter.Bold("PP")}: {osuUser.Statistics.Pp}pp (Country: #{osuUser.Statistics.CountryRank} - Global: #{osuUser.Statistics.GlobalRank})")
                    .AppendLine($"{Formatter.Bold("Accuracy")}: {osuUser.Statistics.HitAccuracy}%")
                    .AppendLine($"{Formatter.Bold("Level")}: {osuUser.Statistics.UserLevel.Current} ({osuUser.Statistics.UserLevel.Progress}%)")
                    .AppendLine($"{Formatter.Bold("Play Count")}: {osuUser.Statistics.PlayCount} with {osuUser.Statistics.PlayTime:g} of play time")
                    .AppendLine($"{Formatter.Bold("Current status")}: {(osuUser.IsOnline ? "Online" : "Offline/Invisible")}");

                var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForMember()
                    .WithAuthor($"{osuUser.Username}'s osu! profile {(osuUser.IsSupporter ? DiscordEmoji.FromName(ctx.Client, ":heart:").ToString() : string.Empty)}", $"https://osu.ppy.sh/users/{osuUser.Id}")
                    .AddField("Basic Information", sb.ToString())
                    .WithThumbnail(osuUser.AvatarUrl.ToString());

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"osu! profile of user {osuUser.Username} in mode {Formatter.Bold(omode.ToString())}")
                    .AddEmbed(embedBuilder.Build()));
            }
            else
            {
                var r = 1;

                if (!isContextMenu)
                {
                    var interactivity = ctx.Client.GetInteractivity();

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("How many scores do you want to get? (1 to 100)"));

                    InteractivityResult<DiscordMessage> res = await interactivity.WaitForMessageAsync(m =>
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
                }
                
                var includeFails = false;

                if (profileSearchType == UserProfileSearchType.Recent && !isContextMenu)
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

                var scores = await GetOsuScoresAsync(username, omode, profileSearchType, includeFails, r);

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
                        .WithAuthor($"{(profileSearchType == UserProfileSearchType.Best ? "Best" : "Recent")} score(s) of {osuUser.Username} in mode {omode}", $"https://osu.ppy.sh/users/{osuUser.Id}", osuUser.AvatarUrl.ToString());

                    foreach (var score in scores)
                    {
                        var beatmap = await _osuClient.GetBeatmapAsync(score.Beatmap.Id);

                        sb
                            .AppendLine($"{Formatter.Bold("Map link")}: {beatmap.Url}")
                            .AppendLine($"{Formatter.Bold("Score")}: {score.TotalScore}")
                            .AppendLine($"{Formatter.Bold("Ranking")}: {score.Rank}")
                            .AppendLine($"{Formatter.Bold("Accuracy")}: {Math.Round(score.Accuracy * 100, 2, MidpointRounding.ToEven)}%")
                            .AppendLine($"{Formatter.Bold("Combo")}: {score.MaxCombo}x/{beatmap.MaxCombo}x")
                            .AppendLine($"{Formatter.Bold("Hit Count")}: [{score.Statistics.Count300}/{score.Statistics.Count100}/{score.Statistics.Count50}/{score.Statistics.CountMiss}]")
                            .AppendLine($"{Formatter.Bold("PP")}: {score.PerformancePoints}pp -> {Math.Round(score.Weight.PerformancePoints, 2, MidpointRounding.ToEven)}pp {Math.Round(score.Weight.Percentage, 2, MidpointRounding.ToEven)}% weighted")
                            .AppendLine($"{Formatter.Bold("Submission Time")}: {score.CreatedAt:f}");

                        embedBuilder
                            .WithThumbnail(score.Beatmapset.Covers.Card2x)
                            .AddField($"{score.Beatmapset.Artist} - {score.Beatmapset.Title} [{beatmap.Version}]{(score.Mods.Any() ? $" +{Formatter.Bold(string.Join(string.Empty, score.Mods))}" : string.Empty)}", sb.ToString());

                        sb.Clear();
                    }

                    builder.AddEmbed(embedBuilder.Build());
                    builder.WithContent($"{(profileSearchType == UserProfileSearchType.Best ? "Best" : "Recent")} score(s) of {osuUser.Username}");
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

public enum UserProfileSearchType
{
    [ChoiceName("profile")]
    Profile,

    [ChoiceName("best_plays")]
    Best,

    [ChoiceName("first_place_plays")]
    First,

    [ChoiceName("recent_plays")]
    Recent
}

public enum UserProfileSearchMode
{
    [ChoiceName("linked")]
    Linked,

    [ChoiceName("standard")]
    Osu,

    [ChoiceName("mania")]
    Mania,

    [ChoiceName("catch_the_beat")]
    Fruits,

    [ChoiceName("taiko")]
    Taiko
}
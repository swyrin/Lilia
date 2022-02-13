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

[SlashCommandGroup("osu", "osu! related commands")]
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
        UserProfileSearchMode searchMode = UserProfileSearchMode.Osu)
    {
        await ctx.DeferAsync(true);

        if (searchMode == UserProfileSearchMode.Default) searchMode = UserProfileSearchMode.Osu;

        var dbUser = _dbCtx.GetUserRecord(ctx.Member);
        dbUser.OsuUsername = username;
        dbUser.OsuMode = ToGameMode(searchMode).ToString();

        _dbCtx.Update(dbUser);
        await _dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Successfully set your osu! username to {Formatter.Bold(username)} and osu! mode to {Formatter.Bold(searchMode.GetName())}"));
    }

    [SlashCommand("forceupdate", "Update an user's osu! profile information in my database")]
    [SlashRequireUserPermissions(Permissions.ManageGuild)]
    public async Task SetMemberOsuUsernameCommand(InteractionContext ctx,
        [Option("user", "User to update, should be an user from your guild")]
        DiscordUser user,
        [Option("username", "osu! username")]
        string username,
        [Option("mode", "Mode")]
        UserProfileSearchMode searchMode = UserProfileSearchMode.Osu)
    {
        await ctx.DeferAsync(true);

        if (searchMode == UserProfileSearchMode.Default) searchMode = UserProfileSearchMode.Osu;

        var dbUser = _dbCtx.GetUserRecord(user);
        dbUser.OsuUsername = username;
        dbUser.OsuMode = ToGameMode(searchMode).ToString();

        _dbCtx.Update(dbUser);
        await _dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Successfully the user's osu! username to {Formatter.Bold(username)} and osu! mode to {Formatter.Bold(searchMode.GetName())}"));
    }

    [SlashCommand("info", "Get your linked data with me")]
    public async Task CheckMyProfileCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);
        var dbUser = _dbCtx.GetUserRecord(ctx.Member);

        var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForUser()
            .AddField("Username",
                !string.IsNullOrWhiteSpace(dbUser.OsuUsername) ? dbUser.OsuUsername : "Not linked yet", true)
            .AddField("Default mode", !string.IsNullOrWhiteSpace(dbUser.OsuMode) ? dbUser.OsuMode : "Not linked yet",
                true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Here is your linked data with me")
            .AddEmbed(embedBuilder.Build()));
    }

    [SlashCommand("lookup", "Get a member's osu! profile")]
    public async Task GetOsuProfileMentionCommand(InteractionContext ctx,
        [Option("user", "Someone in this Discord server")]
        DiscordUser user,
        [Option("type", "Search type")]
        UserProfileSearchType profileSearchType = UserProfileSearchType.Profile,
        [Option("mode", "Search mode")]
        UserProfileSearchMode searchUserProfileSearchMode = UserProfileSearchMode.Default)
    {
        await ctx.DeferAsync();

        var dbUser = _dbCtx.GetUserRecord(user);

        if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("The requested user doesn't exist in my database"));

            return;
        }

        GameMode omode;
        if (searchUserProfileSearchMode == UserProfileSearchMode.Default) Enum.TryParse(dbUser.OsuMode, out omode);
        else omode = ToGameMode(searchUserProfileSearchMode);

        await GenericOsuProfileProcessing(ctx, dbUser.OsuUsername, profileSearchType, FromGameMode(omode));
    }

    [SlashCommand("profile", "Get osu! profile from provided username")]
    public async Task GetOsuProfileStringCommand(InteractionContext ctx,
        [Option("username", "osu! username")]
        string username,
        [Option("type", "Search type")]
        UserProfileSearchType profileSearchType = UserProfileSearchType.Profile,
        [Option("mode", "Search mode")]
        UserProfileSearchMode searchUserProfileSearchMode = UserProfileSearchMode.Default)
    {
        await ctx.DeferAsync();
        await GenericOsuProfileProcessing(ctx, username, profileSearchType, searchUserProfileSearchMode);
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "osu! - Get profile")]
    public async Task GetOsuProfileContextMenu(ContextMenuContext ctx)
    {
        await ctx.DeferAsync(true);

        DiscordUser member = ctx.TargetMember;
        var dbUser = _dbCtx.GetUserRecord(member);

        if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("The requested user doesn't exist in my database"));

            return;
        }

        Enum.TryParse(dbUser.OsuMode, out GameMode omode);

        await GenericOsuProfileProcessing(ctx, dbUser.OsuUsername, UserProfileSearchType.Profile, FromGameMode(omode));
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "osu! - Get latest score")]
    public async Task GetOsuRecentContextMenu(ContextMenuContext ctx)
    {
        await ctx.DeferAsync(true);

        DiscordUser member = ctx.TargetMember;
        var dbUser = _dbCtx.GetUserRecord(member);

        if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("The requested user doesn't exist in my database"));

            return;
        }

        Enum.TryParse(dbUser.OsuMode, out GameMode omode);

        await GenericOsuProfileProcessing(ctx, dbUser.OsuUsername, UserProfileSearchType.Recent, FromGameMode(omode));
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "osu! - Get best play")]
    public async Task GetOsuBestContextMenu(ContextMenuContext ctx)
    {
        await ctx.DeferAsync(true);

        DiscordUser member = ctx.TargetMember;
        var dbUser = _dbCtx.GetUserRecord(member);

        if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("The requested user doesn't exist in my database"));

            return;
        }

        Enum.TryParse(dbUser.OsuMode, out GameMode omode);
        await GenericOsuProfileProcessing(ctx, dbUser.OsuUsername, UserProfileSearchType.Best, FromGameMode(omode)); 
    }

    private static GameMode ToGameMode(UserProfileSearchMode choice)
    {
        return choice switch
        {
            UserProfileSearchMode.Fruits => GameMode.Fruits,
            UserProfileSearchMode.Mania => GameMode.Mania,
            UserProfileSearchMode.Taiko => GameMode.Taiko,
            UserProfileSearchMode.Osu => GameMode.Osu,
            UserProfileSearchMode.Default => GameMode.Osu,
            _ => GameMode.Osu
        };
    }

    private static UserProfileSearchMode FromGameMode(GameMode mode)
    {
        return mode switch
        {
            GameMode.Fruits => UserProfileSearchMode.Fruits,
            GameMode.Mania => UserProfileSearchMode.Mania,
            GameMode.Osu => UserProfileSearchMode.Osu,
            GameMode.Taiko => UserProfileSearchMode.Taiko,
            _ => UserProfileSearchMode.Default
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

    private async Task GenericOsuProfileProcessing(BaseContext ctx, string username, UserProfileSearchType profileSearchType, UserProfileSearchMode profileSearchMode)
    {
        try
        {
            var isContextMenu = ctx is ContextMenuContext;

            IUser osuUser;
            GameMode omode;

            if (profileSearchMode == UserProfileSearchMode.Default)
            {
                osuUser = await _osuClient.GetUserAsync(username);
                omode = osuUser.GameMode;
            }
            else {
                osuUser = await _osuClient.GetUserAsync(username, ToGameMode(profileSearchMode));
                omode = ToGameMode(profileSearchMode);
            }

            if (profileSearchType == UserProfileSearchType.Profile)
            {
                var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForUser()
                    .WithAuthor(
                        $"{osuUser.Username}'s osu! profile in {omode.ToString()} ({(osuUser.IsSupporter ? $"{DiscordEmoji.FromName(ctx.Client, ":heart:")})" : $"{DiscordEmoji.FromName(ctx.Client, ":black_heart:")})")}",
                        $"https://osu.ppy.sh/users/{osuUser.Id}",
                        $"https://flagcdn.com/h20/{osuUser.Country.Code.ToLower()}.jpg")
                    .WithThumbnail(osuUser.AvatarUrl.ToString())
                    .WithDescription($"This user is currently {(osuUser.IsOnline ? "online" : "offline/invisible")}")
                    .AddField("Join date", osuUser.JoinDate.ToString("d"), true)
                    .AddField("Play count - play time as (dd.hh\\:mm\\:ss)",
                        $"{osuUser.Statistics.PlayCount} with {TimeSpan.FromSeconds(osuUser.Statistics.PlayTime).ToString()} of playtime",
                        true)
                    .AddField("Total PP",
                        $"{osuUser.Statistics.Pp} ({osuUser.Country.Name}: #{osuUser.Statistics.CountryRank} - GLB: #{osuUser.Statistics.GlobalRank})")
                    .AddField("Accuracy", $"{osuUser.Statistics.HitAccuracy}%", true)
                    .AddField("Level",
                        $"{osuUser.Statistics.UserLevel.Current} (at {osuUser.Statistics.UserLevel.Progress}%)", true)
                    .AddField("Max combo", $"{osuUser.Statistics.MaximumCombo}", true);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .AddEmbed(embedBuilder.Build()));
            }
            else
            {
                // do a test run before actual stuffs
                var testRun = await GetOsuScoresAsync(username, omode, profileSearchType, true, 2);
                if (!testRun.Any())
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("No scores found. Possible reasons:\n" +
                                     $"1. The user {Formatter.Bold(username)} doesn't play the mode {Formatter.Bold(profileSearchMode.ToString())}, or the old plays are ignored by the server\n" +
                                     "2. Internal issue, contact the owner(s) if the issue persists"));
                    return;
                }

                var r = 1;
                var includeFails = false;

                var interactivity = ctx.Client.GetInteractivity();
                
                // ask for fail inclusion
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
                
                // ask for score count
                if (!isContextMenu)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("How many scores do you want to get? (1 to 100)"));

                    InteractivityResult<DiscordMessage> res = await interactivity.WaitForMessageAsync(m =>
                    {
                        bool isNumber = int.TryParse(m.Content, out r);
                        return isNumber && r is >= 1 and <= 100;
                    });

                    if (res.TimedOut)
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                            .WithContent("Timed out"));

                        return;
                    }

                    // delete the input for the sake of beauty
                    await res.Result.DeleteAsync();
                }
                
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Processing, please wait (this might take a while)"));
                
                var scores = await GetOsuScoresAsync(username, omode, profileSearchType, includeFails, r);
                List<Page> pages = new();
                
                var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForUser();

                var pos = 0;
                foreach (var score in scores)
                {
                    ++pos;
                    var map = await _osuClient.GetBeatmapAsync(score.Beatmap.Id);
                    var isPpExists = score.PerformancePoints != null && score.Weight != null;

                    embedBuilder
                        .WithThumbnail(score.Beatmapset.Covers.Card2x)
                        .WithAuthor(
                            $"{score.Beatmapset.Artist} - {score.Beatmapset.Title} [{map.Version}]{(score.Mods.Any() ? $" +{string.Join(string.Empty, score.Mods)}" : string.Empty)}",
                            $"{map.Url}", $"{osuUser.AvatarUrl}")
                        .WithDescription($"Score position: {pos}\n" +
                                         "If you see 2 0's at the score part, it's also a known issue")
                        .AddField("Total score", $"{score.TotalScore} ({score.User.CountryCode}: #{score.CountryRank.GetValueOrDefault()} - GLB: #{score.GlobalRank.GetValueOrDefault()})")
                        .AddField("Ranking", $"{score.Rank}", true)
                        .AddField("Accuracy", $"{Math.Round(score.Accuracy * 100, 2)}%", true)
                        .AddField("Max combo", $"{score.MaxCombo}x/{map.MaxCombo}x", true)
                        .AddField("Hit count",
                            $"{score.Statistics.Count300}/{score.Statistics.Count100}/{score.Statistics.Count50}/{score.Statistics.CountMiss}",
                            true)
                        .AddField("PP",
                            isPpExists
                                ? $"{score.PerformancePoints} * {Math.Round(score.Weight.Percentage, 2)}% -> {Math.Round(score.Weight.PerformancePoints, 2)}"
                                : "0",
                            true)
                        .AddField("Submission time", $"{score.CreatedAt:g}", true);
                    
                    pages.Add(new Page("If you see a \"This interaction failed\" at the first page, don't worry, it's a known issue", embedBuilder));
                    embedBuilder.ClearFields();
                }
                
                await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.Member, pages, asEditResponse: true);
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
                .WithContent("Something went wrong when parsing the response. Possible reasons:\n" +
                             $"1. The user {Formatter.Bold(username)} doesn't play the mode {Formatter.Bold(profileSearchMode.ToString())}, or the old plays are ignored by the server\n" +
                             "2. Internal issue, contact the owner(s) if the issue persists"));
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
    [ChoiceName("default")]
    Default,

    [ChoiceName("standard")]
    Osu,

    [ChoiceName("mania")]
    Mania,

    [ChoiceName("catch_the_beat")]
    Fruits,

    [ChoiceName("taiko")]
    Taiko
}
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Extensions;
using OsuSharp.Domain;
using OsuSharp.Exceptions;
using OsuSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lilia.Enums;
using Lilia.Services;

namespace Lilia.Modules;

[SlashCommandGroup("osu", "osu! related commands")]
public class OsuModule : ApplicationCommandModule
{
    private readonly LiliaDatabaseContext _dbCtx;
    private readonly IOsuClient _osuClient;

    public OsuModule(LiliaDatabase database, IOsuClient osuClient)
    {
        _dbCtx = database.GetContext();
        _osuClient = osuClient;
    }

    [SlashCommand("self_update", "Update your osu! profile information in my database")]
    public async Task OsuSelfUpdateCommand(InteractionContext ctx,
        [Option("username", "Your osu! username")]
        string username,
        [Option("mode", "Mode")]
        OsuUserProfileSearchMode searchMode = OsuUserProfileSearchMode.Osu)
    {
        await ctx.DeferAsync(true);

        if (searchMode == OsuUserProfileSearchMode.Default) searchMode = OsuUserProfileSearchMode.Osu;

        var dbUser = _dbCtx.GetUserRecord(ctx.Member);
        dbUser.OsuUsername = username;
        dbUser.OsuMode = ToGameMode(searchMode).ToString();

        _dbCtx.Update(dbUser);
        await _dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Successfully set your osu! username to {Formatter.Bold(username)} and osu! mode to {Formatter.Bold(searchMode.GetName())}"));
    }

    [SlashCommand("force_update", "Update an user's osu! profile information in my database")]
    [SlashRequireUserPermissions(Permissions.ManageGuild)]
    public async Task OsuForceUpdateCommand(InteractionContext ctx,
        [Option("user", "User to update, should be an user from your guild")]
        DiscordUser user,
        [Option("username", "osu! username")]
        string username,
        [Option("mode", "Mode")]
        OsuUserProfileSearchMode searchMode = OsuUserProfileSearchMode.Osu)
    {
        await ctx.DeferAsync(true);

        if (searchMode == OsuUserProfileSearchMode.Default) searchMode = OsuUserProfileSearchMode.Osu;

        var dbUser = _dbCtx.GetUserRecord(user);
        dbUser.OsuUsername = username;
        dbUser.OsuMode = ToGameMode(searchMode).ToString();

        _dbCtx.Update(dbUser);
        await _dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Successfully the user's osu! username to {Formatter.Bold(username)} and osu! mode to {Formatter.Bold(searchMode.GetName())}"));
    }

    [SlashCommand("info", "Get your linked data with me")]
    public async Task OsuInfoCommand(InteractionContext ctx)
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
    public async Task OsuLookupCommand(InteractionContext ctx,
        [Option("user", "Someone in this Discord server")]
        DiscordUser user,
        [Option("type", "Search type")]
        OsuUserProfileSearchType profileSearchType = OsuUserProfileSearchType.Profile,
        [Option("mode", "Search mode")]
        OsuUserProfileSearchMode osuUserProfileSearchMode = OsuUserProfileSearchMode.Default)
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
        if (osuUserProfileSearchMode == OsuUserProfileSearchMode.Default) Enum.TryParse(dbUser.OsuMode, out omode);
        else omode = ToGameMode(osuUserProfileSearchMode);

        await GenericOsuProcessing(ctx, dbUser.OsuUsername, profileSearchType, FromGameMode(omode));
    }

    [SlashCommand("profile", "Get osu! profile from username")]
    public async Task OsuProfileCommand(InteractionContext ctx,
        [Option("username", "osu! username")]
        string username,
        [Option("type", "Search type")]
        OsuUserProfileSearchType profileSearchType = OsuUserProfileSearchType.Profile,
        [Option("mode", "Search mode")]
        OsuUserProfileSearchMode osuUserProfileSearchMode = OsuUserProfileSearchMode.Default)
    {
        await ctx.DeferAsync();
        await GenericOsuProcessing(ctx, username, profileSearchType, osuUserProfileSearchMode);
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "osu! - Get profile")]
    public async Task OsuProfileContextMenu(ContextMenuContext ctx)
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

        await GenericOsuProcessing(ctx, dbUser.OsuUsername, OsuUserProfileSearchType.Profile, FromGameMode(omode));
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "osu! - Get latest score")]
    public async Task OsuLatestContextMenu(ContextMenuContext ctx)
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

        await GenericOsuProcessing(ctx, dbUser.OsuUsername, OsuUserProfileSearchType.Recent, FromGameMode(omode));
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "osu! - Get best play")]
    public async Task OsuBestContextMenu(ContextMenuContext ctx)
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
        await GenericOsuProcessing(ctx, dbUser.OsuUsername, OsuUserProfileSearchType.Best, FromGameMode(omode));
    }

    private static GameMode ToGameMode(OsuUserProfileSearchMode choice)
    {
        return choice switch
        {
            OsuUserProfileSearchMode.Fruits => GameMode.Fruits,
            OsuUserProfileSearchMode.Mania => GameMode.Mania,
            OsuUserProfileSearchMode.Taiko => GameMode.Taiko,
            OsuUserProfileSearchMode.Osu => GameMode.Osu,
            OsuUserProfileSearchMode.Default => GameMode.Osu,
            _ => GameMode.Osu
        };
    }

    private static OsuUserProfileSearchMode FromGameMode(GameMode mode)
    {
        return mode switch
        {
            GameMode.Fruits => OsuUserProfileSearchMode.Fruits,
            GameMode.Mania => OsuUserProfileSearchMode.Mania,
            GameMode.Osu => OsuUserProfileSearchMode.Osu,
            GameMode.Taiko => OsuUserProfileSearchMode.Taiko,
            _ => OsuUserProfileSearchMode.Default
        };
    }

    private async Task<IReadOnlyList<IScore>> GetOsuScoresAsync(string username, GameMode omode,
        OsuUserProfileSearchType profileSearchType, bool includeFails = false, int count = 1)
    {
        var osuUser = await _osuClient.GetUserAsync(username, omode);
        IReadOnlyList<IScore> scores = new List<IScore>();

        switch (profileSearchType)
        {
            case OsuUserProfileSearchType.Best:
                scores = await _osuClient.GetUserScoresAsync(osuUser.Id, ScoreType.Best, false, omode, count);
                break;

            case OsuUserProfileSearchType.Recent:
                scores = await _osuClient.GetUserScoresAsync(osuUser.Id, ScoreType.Recent, includeFails, omode, count);
                break;

            case OsuUserProfileSearchType.First:
                scores = await _osuClient.GetUserScoresAsync(osuUser.Id, ScoreType.Firsts, false, omode, count);
                break;

            case OsuUserProfileSearchType.Profile:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(profileSearchType), profileSearchType, null);
        }

        return scores;
    }

    private async Task OsuProfileProcessAsync(BaseContext ctx, IUser osuUser, GameMode omode)
    {
        var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForUser()
            .WithAuthor(
                $"{osuUser.Username}'s osu! profile in {omode} ({(osuUser.IsSupporter ? $"{DiscordEmoji.FromName(ctx.Client, ":heart:")})" : $"{DiscordEmoji.FromName(ctx.Client, ":black_heart:")})")}",
                $"https://osu.ppy.sh/users/{osuUser.Id}",
                $"https://flagcdn.com/h20/{osuUser.Country.Code.ToLower()}.jpg")
            .WithThumbnail($"{osuUser.AvatarUrl}")
            .WithDescription($"This user is currently {(osuUser.IsOnline ? "online" : "offline/invisible")}")
            .AddField("Join date", $"{osuUser.JoinDate:g}", true)
            .AddField("Play count - play time",
                $"{osuUser.Statistics.PlayCount} with {TimeSpan.FromSeconds(osuUser.Statistics.PlayTime).ToShortReadableTimeSpan()} of playtime",
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

    private async Task OsuScoresProcessAsync(BaseContext ctx, string username, GameMode omode,
        OsuUserProfileSearchType profileSearchType, OsuUserProfileSearchMode profileSearchMode, bool isContextMenu)
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

        #region Fail inclusion prompt
        
        // sneaky
        var interactivity = ctx.Client.GetInteractivity();

        if (profileSearchType == OsuUserProfileSearchType.Recent && !isContextMenu)
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

        #endregion
        
        #region Score count prompt (command only)
        
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
        
        #endregion

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
                    $"{map.Url}", $"{score.User.AvatarUrl}")
                .WithDescription($"Score position: {pos}\n")
                .AddField("Known issue", "If you see 2 0's at the score part, it's fine")
                .AddField("Total score",
                    $"{score.TotalScore} ({score.User.CountryCode}: #{score.CountryRank} - GLB: #{score.GlobalRank})")
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

            pages.Add(new Page(
                "If you see a \"This interaction failed\" at the first page, don't worry, it's a known issue",
                embedBuilder));
            
            embedBuilder.ClearFields();
        }

        await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.Member, pages, asEditResponse: true);
    }

    private async Task GenericOsuProcessing(BaseContext ctx, string username, OsuUserProfileSearchType profileSearchType, OsuUserProfileSearchMode profileSearchMode)
    {
        try
        {
            var isContextMenu = ctx is ContextMenuContext;

            IUser osuUser;
            GameMode omode;

            if (profileSearchMode == OsuUserProfileSearchMode.Default)
            {
                osuUser = await _osuClient.GetUserAsync(username);
                omode = osuUser.GameMode;
            }
            else
            {
                osuUser = await _osuClient.GetUserAsync(username, ToGameMode(profileSearchMode));
                omode = ToGameMode(profileSearchMode);
            }

            if (profileSearchType == OsuUserProfileSearchType.Profile)
            {
                await OsuProfileProcessAsync(ctx, osuUser, omode);
            }
            else
            {
                await OsuScoresProcessAsync(ctx, username, omode, profileSearchType, profileSearchMode, isContextMenu);
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
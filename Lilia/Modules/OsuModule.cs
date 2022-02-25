using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Interactors;
using Lilia.Enums;
using Lilia.Services;
using OsuSharp.Domain;
using OsuSharp.Exceptions;
using OsuSharp.Interfaces;

namespace Lilia.Modules;

[Group("osu", "osu! related commands")]
public class OsuModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IOsuClient _osuClient;
    private readonly LiliaClient _liliaClient;
    private readonly LiliaDatabaseContext _dbCtx;

    public OsuModule(LiliaClient liliaClient, LiliaDatabase database, IOsuClient osuClient)
    {
        _liliaClient = liliaClient;
        _dbCtx = database.GetContext();
        _osuClient = osuClient;
    }

    [SlashCommand("self_update", "Update your osu! profile information in my database", runMode: RunMode.Async)]
    public async Task OsuSelfUpdateCommand(
        [Summary(description: "Your osu! username")]
        string username,
        [Summary("mode", "Your osu! preferred mode")]
        OsuUserProfileSearchMode searchMode = OsuUserProfileSearchMode.Default)
    {
        await Context.Interaction.DeferAsync();
        
        if (searchMode == OsuUserProfileSearchMode.Default) searchMode = OsuUserProfileSearchMode.Osu;

        var dbUser = _dbCtx.GetUserRecord(Context.User);
        dbUser.OsuUsername = username;
        dbUser.OsuMode = ToGameMode(searchMode).ToString();

        _dbCtx.Update(dbUser);
        await _dbCtx.SaveChangesAsync();

        await Context.Interaction.ModifyOriginalResponseAsync(x =>
            x.Content = $"Set your osu! username to {Format.Bold(username)} and mode to {Format.Bold($"{searchMode}")}");
    }

    [SlashCommand("force_update", "Update a member's osu! profile information in my database", runMode: RunMode.Async)]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task OsuForceUpdateCommand(
        [Summary(description: "Member to update")]
        SocketGuildUser member,
        [Summary("username", "osu! username")]
        string username,
        [Summary("mode", "Mode")]
        OsuUserProfileSearchMode searchMode = OsuUserProfileSearchMode.Osu)
    {
        await Context.Interaction.DeferAsync(true);

        if (searchMode == OsuUserProfileSearchMode.Default) searchMode = OsuUserProfileSearchMode.Osu;

        var dbUser = _dbCtx.GetUserRecord(member);
        dbUser.OsuUsername = username;
        dbUser.OsuMode = ToGameMode(searchMode).ToString();

        _dbCtx.Update(dbUser);
        await _dbCtx.SaveChangesAsync();

        await Context.Interaction.ModifyOriginalResponseAsync(x =>
            x.Content = $"Set the member's osu! username to {Format.Bold(username)} and mode to {Format.Bold($"{searchMode}")}");
    }

    [SlashCommand("info", "Get your linked data with me")]
    public async Task OsuInfoCommand()
    {
        await Context.Interaction.DeferAsync(true);
        var dbUser = _dbCtx.GetUserRecord(Context.Interaction.User);

        var embedBuilder = Context.Interaction.User.CreateEmbedWithUserData()
            .AddField("Username", !string.IsNullOrWhiteSpace(dbUser.OsuUsername) ? dbUser.OsuUsername : "Not linked yet", true)
            .AddField("Mode", !string.IsNullOrWhiteSpace(dbUser.OsuMode) ? dbUser.OsuMode : "Not linked yet", true);

        await Context.Interaction.ModifyOriginalResponseAsync(x =>
        {
            x.Content = "Here is your linked data with me";
            x.Embed = embedBuilder.Build();
        });
    }

    [SlashCommand("lookup", "Get a member's osu! profile")]
    public async Task OsuLookupCommand(
        [Summary("user", "Someone in this Discord server")]
        SocketUser user,
        [Summary("type", "Search type")]
        OsuUserProfileSearchType profileSearchType = OsuUserProfileSearchType.Profile,
        [Summary("mode", "Search mode")]
        OsuUserProfileSearchMode osuUserProfileSearchMode = OsuUserProfileSearchMode.Default)
    {
        await Context.Interaction.DeferAsync();

        var dbUser = _dbCtx.GetUserRecord(user);

        if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
        {
            await Context.Interaction.ModifyOriginalResponseAsync(x => 
                x.Content = "The requested user doesn't exist in my database");

            return;
        }

        GameMode omode;
        
        if (osuUserProfileSearchMode == OsuUserProfileSearchMode.Default) Enum.TryParse(dbUser.OsuMode, out omode);
        else omode = ToGameMode(osuUserProfileSearchMode);

        await GenericOsuProcessing(dbUser.OsuUsername, profileSearchType, FromGameMode(omode));
    }

    [SlashCommand("profile", "Get osu! profile from username")]
    public async Task OsuProfileCommand(
        [Summary("username", "osu! username")]
        string username,
        [Summary("type", "Search type")]
        OsuUserProfileSearchType profileSearchType = OsuUserProfileSearchType.Profile,
        [Summary("mode", "Search mode")]
        OsuUserProfileSearchMode osuUserProfileSearchMode = OsuUserProfileSearchMode.Default)
    {
        await Context.Interaction.DeferAsync();
        await GenericOsuProcessing(username, profileSearchType, osuUserProfileSearchMode);
    }
    
    [UserCommand("osu! - Get profile")]
    public async Task OsuProfileContextMenu(SocketUser user)
    {
        await Context.Interaction.DeferAsync();

        var member = (SocketGuildUser) user;
        var dbUser = _dbCtx.GetUserRecord(member);

        if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
        {
            await Context.Interaction.ModifyOriginalResponseAsync(x =>
                x.Content = $"The requested user {Format.Bold(Format.UsernameAndDiscriminator(user))} doesn't exist in my database");

            return;
        }

        Enum.TryParse(dbUser.OsuMode, out GameMode omode);
        await GenericOsuProcessing(dbUser.OsuUsername, OsuUserProfileSearchType.Profile, FromGameMode(omode), true);
    }

    [UserCommand("osu! - Get latest score")]
    public async Task OsuLatestContextMenu(SocketUser user)
    {
        await Context.Interaction.DeferAsync();

        var member = (SocketGuildUser) user;
        var dbUser = _dbCtx.GetUserRecord(member);

        if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
        {
            await Context.Interaction.ModifyOriginalResponseAsync(x =>
                x.Content = $"The requested user {Format.Bold(Format.UsernameAndDiscriminator(user))} doesn't exist in my database");

            return;
        }
        
        Enum.TryParse(dbUser.OsuMode, out GameMode omode);
        await GenericOsuProcessing(dbUser.OsuUsername, OsuUserProfileSearchType.Recent, FromGameMode(omode), true);
    }

    [UserCommand("osu! - Get best play")]
    public async Task OsuBestContextMenu(SocketUser user)
    {
        await Context.Interaction.DeferAsync();

        var member = (SocketGuildUser) user;
        var dbUser = _dbCtx.GetUserRecord(member);

        if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
        {
            await Context.Interaction.ModifyOriginalResponseAsync(x =>
                x.Content = $"The requested user {Format.Bold(Format.UsernameAndDiscriminator(user))} doesn't exist in my database");

            return;
        }

        Enum.TryParse(dbUser.OsuMode, out GameMode omode);
        await GenericOsuProcessing(dbUser.OsuUsername, OsuUserProfileSearchType.Best, FromGameMode(omode), true);
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

    private async Task OsuProfileProcessAsync(OsuSharp.Interfaces.IUser osuUser, GameMode omode)
    {
        var embedBuilder = Context.Interaction.User.CreateEmbedWithUserData()
            .WithAuthor(
                $"{osuUser.Username}'s osu! profile in {omode} ( {(osuUser.IsSupporter ? ":heart:" : ":black_heart:")} )",
                $"https://osu.ppy.sh/users/{osuUser.Id}",
                $"https://flagcdn.com/h20/{osuUser.Country.Code.ToLower()}.jpg")
            .WithThumbnailUrl($"{osuUser.AvatarUrl}")
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

        await Context.Interaction.ModifyOriginalResponseAsync(x => 
            x.Embed = embedBuilder.Build());
    }

    private async Task OsuScoresProcessAsync(string username, GameMode omode,
        OsuUserProfileSearchType profileSearchType, OsuUserProfileSearchMode profileSearchMode, bool isContextMenu)
    {
        // do a test run before actual stuffs
        var testRun = await GetOsuScoresAsync(username, omode, profileSearchType, true, 2);
        if (!testRun.Any())
        {
            await Context.Interaction.ModifyOriginalResponseAsync(x => 
                x.Content = "No scores found. Possible reasons:\n" +
                             $"1. The user {Format.Bold(username)} does not play the mode {Format.Bold(profileSearchMode.ToString())}, or the old plays are ignored by the server\n" +
                             "2. Internal issue, contact the owner(s) if the issue persists");
            return;
        }

        var r = 1;
        var includeFails = false;

        #region Fail inclusion prompt
        
        var componentBuilder = new ComponentBuilder()
            .WithButton("Yes, please!", "yesBtn", ButtonStyle.Success)
            .WithButton("Probably not!", "noBtn", ButtonStyle.Danger);

        if (profileSearchType == OsuUserProfileSearchType.Recent && !isContextMenu)
        {
            var failAsk = await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "Do you want to include fail scores?";
                x.Components = componentBuilder.Build();
            });

            var res = await InteractionUtility.WaitForMessageComponentAsync(Context.Client, failAsk, TimeSpan.FromMinutes(1));
            var selectedBtnId = ((SocketMessageComponentData) res.Data).CustomId;

            if (res.HasResponded)
            {
                includeFails = selectedBtnId == "yesBtn";
                await res.RespondAsync("Please wait...");    
            }
            else
            {
                await res.RespondAsync("Timed out");
                return;
            }
        }

        #endregion
        
        #region Score count prompt (command only)
        
        if (!isContextMenu)
        {
            await Context.Interaction.ModifyOriginalResponseAsync(x => 
                x.Content = "How many scores do you want to get? (1 to 100)");

            var nextMessage =
                await _liliaClient.InteractiveService.NextMessageAsync(x =>
                    x.Channel.Id == Context.Channel.Id &&
                    x.Author.Id == Context.User.Id &&
                    int.TryParse(x.Content, out var v) &&
                    v is >= 1 and <= 100);

            if (nextMessage.IsTimeout)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(x => x.Content = "Timed out");
                return;
            }

            if (nextMessage.IsSuccess)
            {
                r = Convert.ToInt32(nextMessage.Value?.Content ?? "1");
            }
            else
            {
                await Context.Interaction.ModifyOriginalResponseAsync(x => x.Content = "An unknown issue found");
                return;
            }
        }
        
        #endregion

        await Context.Interaction.ModifyOriginalResponseAsync(x => 
            x.Content = "Processing, please wait (this might take a while)");

        var pages = new List<PageBuilder>();

        var scores = await GetOsuScoresAsync(username, omode, profileSearchType, includeFails, r);
        var defEmbed = Context.User.CreateEmbedWithUserData();
        
        var pos = 0;
        foreach (var score in scores)
        {
            ++pos;
            var map = await _osuClient.GetBeatmapAsync(score.Beatmap.Id);
            var isPpExists = score.PerformancePoints != null && score.Weight != null;
            
            pages.Add(new PageBuilder()
                .WithColor(defEmbed.Color ?? Color.Red)
                .WithFooter(defEmbed.Footer)
                .WithTimestamp(defEmbed.Timestamp)
                .WithAuthor($"{score.Beatmapset.Artist} - {score.Beatmapset.Title} [{map.Version}]{(score.Mods.Any() ? $" +{string.Join(string.Empty, score.Mods)}" : string.Empty)}",
                    map.Url)
                .WithThumbnailUrl(score.Beatmapset.Covers.Card2x)
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
                .AddField("Submission time", $"{score.CreatedAt:g}", true));
        }

        var staticPageBuilder = new StaticPaginatorBuilder()
            .AddUser(Context.User)
            .WithPages(pages)
            .Build();
        
        await _liliaClient.InteractiveService.SendPaginatorAsync(staticPageBuilder, Context.Channel, resetTimeoutOnInput: true);
    }

    private async Task GenericOsuProcessing(string username, OsuUserProfileSearchType profileSearchType, OsuUserProfileSearchMode profileSearchMode, bool isContextMenu = false)
    {
        try
        {
            OsuSharp.Interfaces.IUser osuUser;
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
                await OsuProfileProcessAsync(osuUser, omode);
            }
            else
            {
                await OsuScoresProcessAsync(username, omode, profileSearchType, profileSearchMode, isContextMenu);
            }
        }
        catch (ApiException)
        {
            await Context.Interaction.ModifyOriginalResponseAsync(x => 
                x.Content = "Something went wrong when sending a request");
        }
        catch (OsuDeserializationException)
        {
            await Context.Interaction.ModifyOriginalResponseAsync(x => 
                x.Content = "No scores found. Possible reasons:\n" +
                            $"1. The user {Format.Bold(username)} does not play the mode {Format.Bold(profileSearchMode.ToString())}, or the old plays are ignored by the server\n" +
                            "2. Internal issue, contact the owner(s) if the issue persists");
        }
    }
}
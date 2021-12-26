using DSharpPlus;
using DSharpPlus.Entities;
using Lilia.Database;
using Lilia.Database.Models;
using Lilia.Services;
using OsuSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Lilia.Database.Extensions;

namespace Lilia.Modules;

[SlashCommandGroup("osu", "osu! commands for Bancho server")]
public class OsuModule : ApplicationCommandModule
{
    private LiliaClient _client;
    private LiliaDbContext _dbCtx;
    private OsuClient _osuApiClient;

    private DiscordColor OsuEmbedColor => DiscordColor.HotPink;

    public OsuModule(LiliaClient client)
    {
        this._client = client;
        this._dbCtx = client.Database.GetContext();
        this._osuApiClient = new OsuClient(new OsuSharpConfiguration
        {
            ApiKey = this._client.Configurations.Credentials.OsuApiKey,
            ModeSeparator = string.Empty
        });
    }

    [SlashCommand("link", "Update your osu! profile in my database for future searches")]
    public async Task SetOsuUsernameCommand(InteractionContext ctx,
        [Option("username", "Your osu! username")]
        string username,
        [Choice("standard", 0)]
        [Choice("taiko", 1)]
        [Choice("catch", 2)]
        [Choice("mania", 3)]
        [Option("mode", "osu! mode to retrieve data")]
        long mode = 0)
    {
        await ctx.DeferAsync();

        DbUser user = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);

        user.OsuUsername = username;
        user.OsuMode = (int) mode;

        this._dbCtx.Update(user);
        await this._dbCtx.SaveChangesAsync();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent(
                $"Successfully set your osu! username to {Formatter.Bold(username)} and osu! mode to {Formatter.Bold(((GameMode) mode).ToString())}"));
    }

    [SlashCommand("me", "Get your linked data with me")]
    public async Task CheckMyProfileCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);
        DbUser user = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);

        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
            .AddField("Username", user.OsuUsername ?? "Not linked yet", true)
            .AddField("Default mode", user.OsuMode == -1 ? $"{(GameMode) user.OsuMode}" : "Not linked yet", true)
            .WithTimestamp(DateTime.Now)
            .WithFooter($"Requested by {ctx.User.Username}#{ctx.User.Discriminator}", ctx.User.AvatarUrl)
            .WithColor(this.OsuEmbedColor);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Here is your linked data with me")
            .AddEmbed(embedBuilder.Build()));
    }

    [SlashCommand("lookup", "Get a member's osu! profile information")]
    public async Task GetOsuProfileMentionCommand(InteractionContext ctx,
        [Option("user", "Someone in this Discord server")]
        DiscordUser discordUser,
        [Choice("profile", "profile")]
        [Choice("recent", "recent")]
        [Choice("best", "best")]
        [Option("type", "Request type")]
        string type = "profile",
        [Choice("standard", 0)]
        [Choice("taiko", 1)]
        [Choice("catch", 2)]
        [Choice("mania", 3)]
        [Option("mode", "Overridden mode if NOT that user didn't link")]
        long mode = 0)
    {
        DiscordMember member = (DiscordMember) discordUser;
        await ctx.DeferAsync();
        DbUser user = this._dbCtx.GetOrCreateUserRecord(member.Id);

        if (string.IsNullOrWhiteSpace(user.OsuUsername))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("That user doesn't exist in my database"));

            return;
        }

        long officialMode = user.OsuMode == -1 ? mode : user.OsuMode;
        User osuUser = await this._osuApiClient.GetUserByUsernameAsync(user.OsuUsername, (GameMode) officialMode);

        if (osuUser == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("That user linked an invalid account"));

            return;
        }

        if (type == "profile")
        {
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTimestamp(DateTime.Now)
                .WithFooter($"Requested by {ctx.Member.DisplayName}#{ctx.Member.Discriminator}", ctx.Member.AvatarUrl)
                .WithAuthor($"{osuUser.Username}'s osu! profile", $"https://osu.ppy.sh/users/{osuUser.UserId}")
                .WithColor(this.OsuEmbedColor)
                .AddField(":information_source: Basic Information",
                    $"**Country** : :flag_{osuUser.Country.Name.ToLower()}: {osuUser.Country.EnglishName}\n" +
                    $"**PP** : {Math.Round(osuUser.PerformancePoints.GetValueOrDefault(), 2, MidpointRounding.AwayFromZero)}pp ( :flag_{osuUser.Country.Name.ToLower()}: : #{osuUser.CountryRank} - :globe_with_meridians: : #{osuUser.Rank})\n" +
                    $"**Level** : {Convert.ToInt32(osuUser.Level)}\n" +
                    $"**Play Count** : {osuUser.PlayCount}\n" +
                    $"**Accuracy** : {Math.Round(osuUser.Accuracy.GetValueOrDefault(), 2, MidpointRounding.AwayFromZero)}%")
                .WithThumbnail($"https://a.ppy.sh/{osuUser.UserId}");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(
                    $"osu! profile of user {Formatter.Bold($"{member.DisplayName}#{member.Discriminator}")} with mode {Formatter.Bold(((GameMode) officialMode).ToString())}")
                .AddEmbed(embedBuilder.Build()));
        }
        else
        {
            InteractivityExtension interactivity = ctx.Client.GetInteractivity();

            DiscordMessage msg = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("How many scores do you want to get?"));

            int r = 1;

            var res = await interactivity.WaitForMessageAsync(m =>
            {
                bool canDo = int.TryParse(m.Content, out r);
                return canDo && r is >= 1 and <= 100;
            });

            if (res.TimedOut)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Time exceeded"));

                return;
            }

            IReadOnlyList<Score> scores = new List<Score>();

            if (type == "best")
                scores = await this._osuApiClient.GetUserBestsByUsernameAsync(user.OsuUsername, (GameMode) officialMode,
                    r);
            else if (type == "recent")
                scores = await this._osuApiClient.GetUserRecentsByUsernameAsync(user.OsuUsername,
                    (GameMode) officialMode, r);

            if (!scores.Any())
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I found nothing, sorry"));
            else
            {
                DiscordFollowupMessageBuilder builder = new DiscordFollowupMessageBuilder();

                foreach (Score score in scores)
                {
                    Beatmap beatmap =
                        await this._osuApiClient.GetBeatmapByIdAsync(score.BeatmapId, (GameMode) officialMode);
                    DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                        .WithTimestamp(DateTime.Now)
                        .WithFooter($"Requested by {ctx.User.Username}#{ctx.User.Discriminator}", ctx.User.AvatarUrl)
                        .WithAuthor(
                            $"{(type == "best" ? "Best" : "Recent")} score of {osuUser.Username} in mode {(GameMode) officialMode}",
                            $"https://osu.ppy.sh/users/{osuUser.UserId}", $"https://a.ppy.sh/{osuUser.UserId}")
                        .WithColor(this.OsuEmbedColor)
                        .WithDescription(
                            $"Map: {Formatter.MaskedUrl($"{beatmap.Artist} - {beatmap.Title} [{beatmap.Difficulty}] **+{score.Mods.ToModeString(this._osuApiClient)}**", beatmap.BeatmapUri)}")
                        .AddField("Score data",
                            $"**Ranking** : {score.Rank}\n" +
                            $"**Accuracy** : {Math.Round(score.Accuracy, 2, MidpointRounding.AwayFromZero)}%\n" +
                            $"**Max Combo** : {score.MaxCombo}x/{beatmap.MaxCombo}x\n" +
                            $"**Hit Count** : [{score.Count300}/{score.Count100}/{score.Count50}/{score.Miss}]\n" +
                            "**PP** : implement soon:tm:")
                        .WithThumbnail(beatmap.CoverUri);

                    builder.AddEmbed(embedBuilder.Build());
                }

                builder.WithContent(
                    $"{(type == "best" ? "Best" : "Recent")} scores of user {member.Username}#{member.Discriminator}");
                await ctx.FollowUpAsync(builder);
            }
        }
    }

    [SlashCommand("profile", "Get osu! profile from provided username")]
    public async Task GetOsuProfileStringCommand(InteractionContext ctx,
        [Option("username", "Someone's osu! username")]
        string username,
        [Choice("profile", "profile")]
        [Choice("recent", "recent")]
        [Choice("best", "best")]
        [Option("type", "Request type")]
        string type = "profile",
        [Choice("standard", 0)]
        [Choice("taiko", 1)]
        [Choice("catch", 2)]
        [Choice("mania", 3)]
        [Option("mode", "osu! mode to retrieve data")]
        long mode = 0)
    {
        await ctx.DeferAsync();
        User osuUser = await this._osuApiClient.GetUserByUsernameAsync(username, (GameMode) mode);

        if (osuUser == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Nothing found"));

            return;
        }

        if (type == "profile")
        {
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTimestamp(DateTime.Now)
                .WithFooter($"Requested by {ctx.Member.DisplayName}#{ctx.Member.Discriminator}", ctx.Member.AvatarUrl)
                .WithAuthor($"{osuUser.Username}'s osu! profile", $"https://osu.ppy.sh/users/{osuUser.UserId}")
                .WithColor(this.OsuEmbedColor)
                .AddField(":information_source: Basic Information",
                    $"**Country** : :flag_{osuUser.Country.Name.ToLower()}: {osuUser.Country.EnglishName}\n" +
                    $"**PP** : {Math.Round(osuUser.PerformancePoints.GetValueOrDefault(), 2, MidpointRounding.AwayFromZero)}pp ( :flag_{osuUser.Country.Name.ToLower()}: : #{osuUser.CountryRank} - :globe_with_meridians: : #{osuUser.Rank})\n" +
                    $"**Level** : {Convert.ToInt32(osuUser.Level)}\n" +
                    $"**Play Count** : {osuUser.PlayCount}\n" +
                    $"**Accuracy** : {Math.Round(osuUser.Accuracy.GetValueOrDefault(), 2, MidpointRounding.AwayFromZero)}%")
                .WithThumbnail($"https://a.ppy.sh/{osuUser.UserId}");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(
                    $"osu! profile of user {username} with mode {Formatter.Bold(((GameMode) mode).ToString())}")
                .AddEmbed(embedBuilder.Build()));
        }
        else
        {
            InteractivityExtension interactivity = ctx.Client.GetInteractivity();

            DiscordMessage msg = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("How many scores do you want to get?"));

            int r = 1;

            var res = await interactivity.WaitForMessageAsync(m =>
            {
                bool canDo = int.TryParse(m.Content, out r);
                return canDo && r is >= 1 and <= 100;
            });

            if (res.TimedOut)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Time exceeded"));

                return;
            }

            IReadOnlyList<Score> scores = new List<Score>();

            if (type == "best")
                scores = await this._osuApiClient.GetUserBestsByUsernameAsync(username, (GameMode) mode, r);
            else if (type == "recent")
                scores = await this._osuApiClient.GetUserRecentsByUsernameAsync(username, (GameMode) mode, r);

            if (!scores.Any())
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I found nothing, sorry"));
            else
            {
                DiscordFollowupMessageBuilder builder = new DiscordFollowupMessageBuilder();

                foreach (Score score in scores)
                {
                    Beatmap beatmap = await this._osuApiClient.GetBeatmapByIdAsync(score.BeatmapId, (GameMode) mode);
                    DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                        .WithTimestamp(DateTime.Now)
                        .WithFooter($"Requested by {ctx.User.Username}#{ctx.User.Discriminator}", ctx.User.AvatarUrl)
                        .WithAuthor(
                            $"{(type == "best" ? "Best" : "Recent")} score of {osuUser.Username} in mode {(GameMode) mode}",
                            $"https://osu.ppy.sh/users/{osuUser.UserId}", $"https://a.ppy.sh/{osuUser.UserId}")
                        .WithColor(this.OsuEmbedColor)
                        .WithDescription(
                            $"Map: {Formatter.MaskedUrl($"{beatmap.Artist} - {beatmap.Title} [{beatmap.Difficulty}] **+{score.Mods.ToModeString(this._osuApiClient)}**", beatmap.BeatmapUri)}")
                        .AddField("Score data",
                            $"**Ranking** : {score.Rank}\n" +
                            $"**Accuracy** : {Math.Round(score.Accuracy, 2, MidpointRounding.AwayFromZero)}%\n" +
                            $"**Max Combo** : {score.MaxCombo}x/{beatmap.MaxCombo}x\n" +
                            $"**Hit Count** : [{score.Count300}/{score.Count100}/{score.Count50}/{score.Miss}]\n" +
                            "**PP** : implement soon:tm:")
                        .WithThumbnail(beatmap.CoverUri);

                    builder.AddEmbed(embedBuilder.Build());
                }

                builder.WithContent($"{(type == "best" ? "Best" : "Recent")} scores of user {osuUser.Username}");
                await ctx.FollowUpAsync(builder);
            }
        }
    }
}
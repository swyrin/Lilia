using DSharpPlus;
using DSharpPlus.Entities;
using Lilia.Database;
using Lilia.Database.Models;
using Lilia.Services;
using OsuSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        [Choice("standard", "Standard")]
        [Choice("taiko", "Taiko")]
        [Choice("catch", "Catch")]
        [Choice("mania", "Mania")]
        [Option("mode", "osu! mode to retrieve data")]
        string mode = "Standard")
    {
        await ctx.DeferAsync();

        DbUser user = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);

        user.OsuUsername = username;
        user.OsuMode = mode;

        this._dbCtx.Update(user);
        await this._dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Successfully set your osu! username to {Formatter.Bold(username)} and osu! mode to {Formatter.Bold(mode)}"));
    }

    [SlashCommand("me", "Get your linked data with me")]
    public async Task CheckMyProfileCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);
        DbUser user = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);

        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
            .AddField("Username", !string.IsNullOrWhiteSpace(user.OsuUsername) ? user.OsuUsername : "Not linked yet", true)
            .AddField("Default mode", !string.IsNullOrWhiteSpace(user.OsuMode) ? user.OsuMode : "Not linked yet", true)
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
        [Choice("linked", "Linked")]
        [Choice("standard", "Standard")]
        [Choice("taiko", "Taiko")]
        [Choice("catch", "Catch")]
        [Choice("mania", "Mania")]
        [Option("mode", "osu! mode to retrieve data")]
        string mode = "Linked")
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

        if (mode == "Linked") mode = user.OsuMode;

        Enum.TryParse(mode, out GameMode omode);

        User osuUser = await this._osuApiClient.GetUserByUsernameAsync(user.OsuUsername, omode);

        if (osuUser == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("That user linked an invalid account"));

            return;
        }

        if (type == "profile")
        {
            StringBuilder sb = new StringBuilder();

            sb
                .AppendLine($"{Formatter.Bold("Join Date")}: {osuUser.JoinDate:d}")
                .AppendLine($"{Formatter.Bold("Country")}: {osuUser.Country.EnglishName} :flag_{osuUser.Country.Name.ToLower()}:")
                .AppendLine($"{Formatter.Bold("Total Score")}: {osuUser.Score} - {osuUser.RankedScore} ranked score")
                .AppendLine($"{Formatter.Bold("PP")}: {osuUser.PerformancePoints.GetValueOrDefault()}pp (Country: #{osuUser.CountryRank} - Global: #{osuUser.Rank})")
                .AppendLine($"{Formatter.Bold("Accuracy")}: {osuUser.Accuracy.GetValueOrDefault()}%")
                .AppendLine($"{Formatter.Bold("Level")}: {Convert.ToInt32(osuUser.Level)}")
                .AppendLine($"{Formatter.Bold("Play Count")}: {osuUser.PlayCount} with {osuUser.TimePlayed:g} of play time");

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTimestamp(DateTime.Now)
                .WithFooter($"Requested by {ctx.Member.DisplayName}#{ctx.Member.Discriminator}", ctx.Member.AvatarUrl)
                .WithAuthor($"{osuUser.Username}'s osu! profile", osuUser.ProfileUri.ToString())
                .WithColor(this.OsuEmbedColor)
                .AddField("Basic Information", sb.ToString())
                .AddField("Click the link below to spectate this user, if they are playing", osuUser.SpectateUri.ToString())
                .WithThumbnail($"https://a.ppy.sh/{osuUser.UserId}");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"osu! profile of user {member.DisplayName}#{member.Discriminator} with mode {Formatter.Bold(omode.ToString())}")
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
                scores = await this._osuApiClient.GetUserBestsByUsernameAsync(user.OsuUsername, omode, r);
            else if (type == "recent")
                scores = await this._osuApiClient.GetUserRecentsByUsernameAsync(user.OsuUsername, omode, r);

            if (!scores.Any())
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("I found nothing, sorry"));
            else
            {
                DiscordFollowupMessageBuilder builder = new DiscordFollowupMessageBuilder();
                StringBuilder sb = new StringBuilder();

                foreach (Score score in scores)
                {
                    Beatmap beatmap = await score.GetBeatmapAsync();

                    sb
                        .AppendLine($"{Formatter.Bold("Score")}: {score.TotalScore}")
                        .AppendLine($"{Formatter.Bold("Ranking")}: {score.Rank}")
                        .AppendLine($"{Formatter.Bold("Accuracy")}: {score.Accuracy}%")
                        .AppendLine($"{Formatter.Bold("Combo")}: {score.MaxCombo}x/{beatmap.MaxCombo}x")
                        .AppendLine($"{Formatter.Bold("Hit Count")}: [{score.Count300}/{score.Count100}/{score.Count50}/{score.Miss}]")
                        .AppendLine($"{Formatter.Bold("PP")}: {score.PerformancePoints.GetValueOrDefault()}")
                        .AppendLine($"{Formatter.Bold("Submission Time")}: {score.Date:f}");
                    
                    DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                        .WithTimestamp(DateTime.Now)
                        .WithFooter($"Requested by {ctx.User.Username}#{ctx.User.Discriminator}", ctx.User.AvatarUrl)
                        .WithAuthor(
                            $"{(type == "best" ? "Best" : "Recent")} score of {osuUser.Username} in mode {omode}", osuUser.ProfileUri.ToString(), $"https://a.ppy.sh/{osuUser.UserId}")
                        .WithColor(this.OsuEmbedColor)
                        .WithDescription($"Map: {Formatter.MaskedUrl($"{beatmap.Artist} - {beatmap.Title} [{beatmap.Difficulty}] +{Formatter.Bold(score.Mods.ToModeString(this._osuApiClient))}", beatmap.BeatmapUri)}")
                        .AddField("Score data", sb.ToString())
                        .WithThumbnail(beatmap.CoverUri);

                    builder.AddEmbed(embedBuilder.Build());
                    sb.Clear();
                }

                builder.WithContent($"{(type == "best" ? "Best" : "Recent")} scores of user {osuUser.Username}");
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
        [Choice("standard", "Standard")]
        [Choice("taiko", "Taiko")]
        [Choice("catch", "Catch")]
        [Choice("mania", "Mania")]
        [Option("mode", "osu! mode to retrieve data")]
        string mode = "Standard")
    {
        await ctx.DeferAsync();
        Enum.TryParse(mode, out GameMode omode);

        User osuUser = await this._osuApiClient.GetUserByUsernameAsync(username, omode);

        if (osuUser == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Nothing found"));

            return;
        }

        if (type == "profile")
        {
            StringBuilder sb = new StringBuilder();

            sb
                .AppendLine($"{Formatter.Bold("Join Date")}: {osuUser.JoinDate:d}")
                .AppendLine($"{Formatter.Bold("Country")}: {osuUser.Country.EnglishName} :flag_{osuUser.Country.Name.ToLower()}:")
                .AppendLine($"{Formatter.Bold("Total Score")}: {osuUser.Score} - {osuUser.RankedScore} ranked score")
                .AppendLine($"{Formatter.Bold("PP")}: {osuUser.PerformancePoints.GetValueOrDefault()}pp (Country: #{osuUser.CountryRank} - Global: #{osuUser.Rank})")
                .AppendLine($"{Formatter.Bold("Accuracy")}: {osuUser.Accuracy.GetValueOrDefault()}%")
                .AppendLine($"{Formatter.Bold("Level")}: {Convert.ToInt32(osuUser.Level)}")
                .AppendLine($"{Formatter.Bold("Play Count")}: {osuUser.PlayCount} with {osuUser.TimePlayed:g} of play time");

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTimestamp(DateTime.Now)
                .WithFooter($"Requested by {ctx.Member.DisplayName}#{ctx.Member.Discriminator}", ctx.Member.AvatarUrl)
                .WithAuthor($"{osuUser.Username}'s osu! profile", osuUser.ProfileUri.ToString())
                .WithColor(this.OsuEmbedColor)
                .AddField("Basic Information", sb.ToString())
                .WithThumbnail($"https://a.ppy.sh/{osuUser.UserId}");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"osu! profile of user {username} with mode {Formatter.Bold(omode.ToString())}")
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
                scores = await this._osuApiClient.GetUserBestsByUsernameAsync(username, omode, r);
            else if (type == "recent")
                scores = await this._osuApiClient.GetUserRecentsByUsernameAsync(username, omode, r);

            if (!scores.Any())
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("I found nothing, sorry"));
            else
            {
                DiscordFollowupMessageBuilder builder = new DiscordFollowupMessageBuilder();
                StringBuilder sb = new StringBuilder();

                foreach (Score score in scores)
                {
                    Beatmap beatmap = await score.GetBeatmapAsync();

                    sb
                        .AppendLine($"{Formatter.Bold("Score")}: {score.TotalScore}")
                        .AppendLine($"{Formatter.Bold("Ranking")}: {score.Rank}")
                        .AppendLine($"{Formatter.Bold("Accuracy")}: {score.Accuracy}%")
                        .AppendLine($"{Formatter.Bold("Combo")}: {score.MaxCombo}x/{beatmap.MaxCombo}x")
                        .AppendLine($"{Formatter.Bold("Hit Count")}: [{score.Count300}/{score.Count100}/{score.Count50}/{score.Miss}]")
                        .AppendLine($"{Formatter.Bold("PP")}: {score.PerformancePoints.GetValueOrDefault()}")
                        .AppendLine($"{Formatter.Bold("Submission Time")}: {score.Date:f}");
                    
                    DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                        .WithTimestamp(DateTime.Now)
                        .WithFooter($"Requested by {ctx.User.Username}#{ctx.User.Discriminator}", ctx.User.AvatarUrl)
                        .WithAuthor($"{(type == "best" ? "Best" : "Recent")} score of {osuUser.Username} in mode {omode}", osuUser.ProfileUri.ToString(), $"https://a.ppy.sh/{osuUser.UserId}")
                        .WithColor(this.OsuEmbedColor)
                        .WithDescription($"Map: {Formatter.MaskedUrl($"{beatmap.Artist} - {beatmap.Title} [{beatmap.Difficulty}] +{Formatter.Bold(score.Mods.ToModeString(this._osuApiClient))}", beatmap.BeatmapUri)}")
                        .AddField("Score data", sb.ToString())
                        .WithThumbnail(beatmap.CoverUri);

                    builder.AddEmbed(embedBuilder.Build());
                    sb.Clear();
                }

                builder.WithContent($"{(type == "best" ? "Best" : "Recent")} scores of user {osuUser.Username}");
                await ctx.FollowUpAsync(builder);
            }
        }
    }
}
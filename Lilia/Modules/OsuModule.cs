using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Lilia.Database;
using Lilia.Database.Models;
using Lilia.Services;
using OsuSharp;
using OsuSharp.Oppai;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lilia.Modules
{
    [Group("osu")]
    [Description("Represents for osu! commands. Only works in official server.")]
    public class OsuModule : BaseCommandModule
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

        [Command("setuser")]
        [Description("Link your osu! profile username to my database for future searches.")]
        public async Task SetOsuUsernameCommand(CommandContext ctx,
            [Description("Your osu! username.")] [RemainingText] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                await ctx.RespondAsync("You didn't give me your username.");
                return;
            }

            DbUser user = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);
            user.OsuUsername = username;
            this._dbCtx.Update(user);
            this._dbCtx.SaveChanges();

            await ctx.RespondAsync($"Successfully set your osu! username to **{username}.**");
        }

        [Command("setmode")]
        [Description("Link your osu! profile default mode to my database for future searches.")]
        public async Task SetOsuDefaultModeCommand(CommandContext ctx,
            [Description("Mode number: 0 - Standard; 1 - Taiko; 2 - Catch; 3 - Mania.")] int mode = 0)
        {
            if (!(0 <= mode && mode <= 3))
            {
                await ctx.RespondAsync("Invalid mode required. Available modes are: `0 - Standard; 1 - Taiko; 2 - Catch; 3 - Mania.`");
                return;
            }

            DbUser user = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);
            user.OsuMode = mode;
            this._dbCtx.Update(user);
            this._dbCtx.SaveChanges();

            await ctx.RespondAsync($"Successfully set your osu! mode to **{(GameMode) mode}**.");
        }

        [Command("linked")]
        [Description("See your linked data with me.")]
        public async Task GetLinkedInfoCommand(CommandContext ctx)
        {
            DbUser user = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .AddField("Username", user.OsuUsername ?? "Not linked yet.", true)
                .AddField("Default mode", $"{(GameMode) user.OsuMode}", true)
                .WithTimestamp(DateTime.Now)
                .WithFooter($"Requested by {ctx.User.Username}#{ctx.User.Discriminator}", ctx.User.AvatarUrl)
                .WithColor(this.OsuEmbedColor);

            await ctx.RespondAsync(embed: embedBuilder.Build());
        }

        #region "user"/"profile" command overloads

        [Command("user")]
        [Aliases("profile")]
        [Description("Get osu! detailed profile information.")]
        public async Task GetOsuUserCommand(CommandContext ctx)
        {
            DbUser user = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);

            if (string.IsNullOrWhiteSpace(user.OsuUsername))
                await ctx.RespondAsync("You have not linked your osu! account yet.");
            else
                await this.GetOsuUserCommand(ctx, user.OsuUsername, user.OsuMode);
        }

        [Command("user")]
        public async Task GetOsuUserCommand(CommandContext ctx,
            [Description("Discord user mention to get data. Might be annoying.")] DiscordMember mentionedMember)
        {
            DbUser user = this._dbCtx.GetOrCreateUserRecord(mentionedMember.Id);

            if (string.IsNullOrWhiteSpace(user.OsuUsername))
                await ctx.RespondAsync("That user has not linked their osu! account yet.");
            else
                await this.GetOsuUserCommand(ctx, user.OsuUsername, user.OsuMode);
        }

        [Command("user")]
        public async Task GetOsuUserCommand(CommandContext ctx,
            [Description("Discord user ID to get data.")] ulong userId)
        {
            DbUser user = this._dbCtx.GetOrCreateUserRecord(userId);

            if (string.IsNullOrWhiteSpace(user.OsuUsername))
                await ctx.RespondAsync("That user has not linked their osu! account yet.");
            else
                await this.GetOsuUserCommand(ctx, user.OsuUsername, user.OsuMode);
        }

        [Command("user")]
        public async Task GetOsuUserCommand(CommandContext ctx,
            [Description("Username to get data, IN QUOTES.")] string username,
            [Description("Mode number to get data: 0 - Standard; 1 - Taiko; 2 - Catch; 3 - Mania.")] int mode = 0)
        {
            User user = await this._osuApiClient.GetUserByUsernameAsync(username.Replace("\"", ""), (GameMode) mode);

            if (user == null)
            {
                await ctx.RespondAsync("User not found.");
            }
            else
            {
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                    .WithTimestamp(DateTime.Now)
                    .WithFooter($"Requested by {ctx.User.Username}#{ctx.User.Discriminator}", ctx.User.AvatarUrl)
                    .WithAuthor($"{user.Username}'s osu! profile", $"https://osu.ppy.sh/users/{user.UserId}")
                    .WithColor(this.OsuEmbedColor)
                    .AddField(":information_source: Basic Information",
                        $"**Country** : :flag_{user.Country.Name.ToLower()}: {user.Country.EnglishName}\n" +
                        $"**PP** : {Math.Round(user.PerformancePoints.GetValueOrDefault(), 2, MidpointRounding.AwayFromZero)}pp ( :flag_{user.Country.Name.ToLower()}: : #{user.CountryRank} - :globe_with_meridians: : #{user.Rank})\n" +
                        $"**Level** : {Convert.ToInt32(user.Level)}\n" +
                        $"**Play Count** : {user.PlayCount}\n" +
                        $"**Accuracy** : {Math.Round(user.Accuracy.GetValueOrDefault(), 2, MidpointRounding.AwayFromZero)}%")
                    .WithThumbnail($"https://a.ppy.sh/{user.UserId}");

                await ctx.RespondAsync(embed: embedBuilder.Build());
            }
        }

        #endregion "user"/"profile" command overloads

        #region "recent"/"r" command overloads

        [Command("recent")]
        [Aliases("r")]
        [Description("Get most recent score of an user.")]
        public async Task GetRecentScoreCommand(CommandContext ctx)
        {
            DbUser user = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);

            if (string.IsNullOrWhiteSpace(user.OsuUsername))
                await ctx.RespondAsync("You have not linked your osu! account yet.");
            else
                await this.GetRecentScoreCommand(ctx, user.OsuUsername, user.OsuMode);
        }

        [Command("recent")]
        public async Task GetRecentScoreCommand(CommandContext ctx,
            [Description("Discord user mention to get data. Might be annoying.")] DiscordMember mentionedMember)
        {
            DbUser user = this._dbCtx.GetOrCreateUserRecord(mentionedMember.Id);

            if (string.IsNullOrWhiteSpace(user.OsuUsername))
                await ctx.RespondAsync("That user has not linked their osu! account yet.");
            else
                await this.GetRecentScoreCommand(ctx, user.OsuUsername, user.OsuMode);
        }

        [Command("recent")]
        public async Task GetRecentScoreCommand(CommandContext ctx,
            [Description("Discord user ID to get data.")] ulong userId)
        {
            DbUser user = this._dbCtx.GetOrCreateUserRecord(userId);

            if (string.IsNullOrWhiteSpace(user.OsuUsername))
                await ctx.RespondAsync("That user has not linked their osu! account yet.");
            else
                await this.GetRecentScoreCommand(ctx, user.OsuUsername, user.OsuMode);
        }

        [Command("recent")]
        public async Task GetRecentScoreCommand(CommandContext ctx,
            [Description("Username to get data, IN QUOTES.")] string username,
            [Description("Mode number to get data: 0 - Standard; 1 - Taiko; 2 - Catch; 3 - Mania.")] int mode = 0)
        {
            User user = await this._osuApiClient.GetUserByUsernameAsync(username, (GameMode) mode);

            if (user == null)
                await ctx.RespondAsync("User not found.");
            else
            {
                Score recentScore = (await this._osuApiClient.GetUserRecentsByUsernameAsync(username, (GameMode)mode, 1)).FirstOrDefault();

                if (recentScore == null)
                    await ctx.RespondAsync("This user have not played anything recently.");
                else
                {
                    Beatmap beatmap = await this._osuApiClient.GetBeatmapByIdAsync(recentScore.BeatmapId, (GameMode)mode);
                    PerformanceData fc = await beatmap.GetPPAsync(recentScore.Mods, (float)Math.Round(recentScore.Accuracy, 2, MidpointRounding.AwayFromZero));
                    PerformanceData currentProgress = await recentScore.GetPPAsync();

                    DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                        .WithTimestamp(DateTime.Now)
                        .WithFooter($"Requested by {ctx.User.Username}#{ctx.User.Discriminator}", ctx.User.AvatarUrl)
                        .WithAuthor($"Recent play of {user.Username} in mode {(GameMode)mode}", $"https://osu.ppy.sh/users/{user.UserId}", $"https://a.ppy.sh/{user.UserId}")
                        .WithColor(this.OsuEmbedColor)
                        .WithDescription($"Map: {Formatter.MaskedUrl($"{beatmap.Artist} - {beatmap.Title} [{beatmap.Difficulty}] **+{recentScore.Mods.ToModeString(this._osuApiClient)}**", beatmap.BeatmapUri)}")
                        .AddField("Record data",
                            $"**Ranking** : {recentScore.Rank}\n" +
                            $"**Accuracy** : {Math.Round(recentScore.Accuracy, 2, MidpointRounding.AwayFromZero)}%\n" +
                            $"**Max Combo** : {recentScore.MaxCombo}x/{beatmap.MaxCombo}x\n" +
                            $"**Hit Count** : [{recentScore.Count300}/{recentScore.Count100}/{recentScore.Count50}/{recentScore.Miss}]\n" +
                            $"**PP** : {Math.Round(currentProgress.Pp, 2, MidpointRounding.AwayFromZero)}pp - **{Math.Round(fc.Pp, 2, MidpointRounding.AwayFromZero)}pp** for {Math.Round(score.Accuracy)}% FC")
                        .WithThumbnail(beatmap.CoverUri);

                    await ctx.RespondAsync(embed: embedBuilder.Build());
                }
            }
        }

        #endregion "recent"/"r" command overloads

        #region "best"/"top" command overloads

        [Command("best")]
        [Aliases("top")]
        [Description("Get best scores of an user.")]
        public async Task GetBestScoresCommand(CommandContext ctx,
            [Description("Number of best scores to get data.")] int amount = 1)
        {
            DbUser dbUser = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);

            if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
                await ctx.RespondAsync("You have not linked your osu! account yet.");
            else
                await this.GetBestRecordsCommand(ctx, dbUser.OsuUsername, dbUser.OsuMode, amount);
        }

        [Command("best")]
        public async Task GetBestScoresCommand(CommandContext ctx,
            [Description("Discord user mention to get data. Might be annoying.")] DiscordMember mentionedMember,
            [Description("Number of best scores to get.")] int amount = 1)
        {
            DbUser dbUser = this._dbCtx.GetOrCreateUserRecord(mentionedMember.Id);

            if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
                await ctx.RespondAsync("You have not linked your osu! account yet.");
            else
                await this.GetBestRecordsCommand(ctx, dbUser.OsuUsername, dbUser.OsuMode, amount);
        }

        [Command("best")]
        public async Task GetBestScoresCommand(CommandContext ctx,
            [Description("Discord user ID to get data.")] ulong userId,
            [Description("Number of best scores to get.")] int amount = 1)
        {
            DbUser dbUser = this._dbCtx.GetOrCreateUserRecord(userId);

            if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
                await ctx.RespondAsync("That user has not linked their osu! account yet.");
            else
                await this.GetBestRecordsCommand(ctx, dbUser.OsuUsername, dbUser.OsuMode, amount);
        }

        [Command("best")]
        public async Task GetBestScoresCommand(CommandContext ctx,
            [Description("Username to get best scores, IN QUOTES.")] string username,
            [Description("Mode number to get data: 0 - Standard; 1 - Taiko; 2 - Catch; 3 - Mania.")] int mode = 0,
            [Description("Number of best scores to get.")] int amount = 1)
        {
            User user = await this._osuApiClient.GetUserByUsernameAsync(username, (GameMode) mode);

            if (user == null)
                await ctx.RespondAsync("User not found.");
            else
            {
                IReadOnlyList<Score> scores = await this._osuApiClient.GetUserBestsByUsernameAsync(username, (GameMode) mode, amount);

                if (!scores.Any())
                    await ctx.RespondAsync("This user have not played something yet.");
                else
                {
                    foreach (Score score in scores)
                    {
                        Beatmap beatmap = await this._osuApiClient.GetBeatmapByIdAsync(score.BeatmapId, (GameMode) mode);
                        PerformanceData fc = await beatmap.GetPPAsync(score.Mods, (float)Math.Round(score.Accuracy, 2, MidpointRounding.AwayFromZero));
                        PerformanceData currentProgress = await score.GetPPAsync();

                        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                            .WithTimestamp(DateTime.Now)
                            .WithFooter($"Requested by {ctx.User.Username}#{ctx.User.Discriminator}", ctx.User.AvatarUrl)
                            .WithAuthor($"Best score of {user.Username} in mode {(GameMode) mode}", $"https://osu.ppy.sh/users/{user.UserId}", $"https://a.ppy.sh/{user.UserId}")
                            .WithColor(this.OsuEmbedColor)
                            .WithDescription($"Map: {Formatter.MaskedUrl($"{beatmap.Artist} - {beatmap.Title} [{beatmap.Difficulty}] **+{score.Mods.ToModeString(this._osuApiClient)}**", beatmap.BeatmapUri)}")
                            .AddField("Score data",
                                $"**Ranking** : {score.Rank}\n" +
                                $"**Accuracy** : {Math.Round(score.Accuracy, 2, MidpointRounding.AwayFromZero)}%\n" +
                                $"**Max Combo** : {score.MaxCombo}x/{beatmap.MaxCombo}x\n" +
                                $"**Hit Count** : [{score.Count300}/{score.Count100}/{score.Count50}/{score.Miss}]\n" +
                                $"**PP** : {Math.Round(currentProgress.Pp, 2, MidpointRounding.AwayFromZero)}pp - **{Math.Round(fc.Pp, 2, MidpointRounding.AwayFromZero)}pp** for {Math.Round(score.Accuracy)}% FC")
                            .WithThumbnail(beatmap.CoverUri);

                        await ctx.RespondAsync(embed: embedBuilder.Build());
                    }
                }
            }
        }

        #endregion "best"/"top" command overloads
    }
}
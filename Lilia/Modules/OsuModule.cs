﻿using DSharpPlus;
using DSharpPlus.Entities;
using Lilia.Database;
using Lilia.Database.Models;
using Lilia.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Lilia.Database.Extensions;
using OsuSharp.Domain;
using OsuSharp.Exceptions;
using OsuSharp.Interfaces;

namespace Lilia.Modules;

[SlashCommandGroup("osu", "osu! commands for Bancho server")]
public class OsuModule : ApplicationCommandModule
{
    private LiliaClient _client;
    private LiliaDbContext _dbCtx;
    private IOsuClient _osuClient;

    private DiscordColor OsuEmbedColor => DiscordColor.HotPink;

    public OsuModule(LiliaClient client, IOsuClient osuClient)
    {
        this._client = client;
        this._dbCtx = client.Database.GetContext();
        this._osuClient = osuClient;
    }
    
    [SlashCommand("link", "Update your osu! profile in my database for future searches")]
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
        await ctx.DeferAsync();

        DbUser dbUser = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);

        dbUser.OsuUsername = username;
        dbUser.OsuMode = mode;

        this._dbCtx.Update(dbUser);
        await this._dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Successfully set your osu! username to {Formatter.Bold(username)} and osu! mode to {Formatter.Bold(mode)}"));
    }

    [SlashCommand("me", "Get your linked data with me")]
    public async Task CheckMyProfileCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);
        DbUser dbUser = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);

        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
            .AddField("Username", !string.IsNullOrWhiteSpace(dbUser.OsuUsername) ? dbUser.OsuUsername : "Not linked yet", true)
            .AddField("Default mode", !string.IsNullOrWhiteSpace(dbUser.OsuMode) ? dbUser.OsuMode : "Not linked yet", true)
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
        [Choice("firsts", "firsts")]
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

        DbUser dbUser = this._dbCtx.GetOrCreateUserRecord(member.Id);

        if (string.IsNullOrWhiteSpace(dbUser.OsuUsername))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("That user doesn't exist in my database"));

            return;
        }

        if (mode == "Linked") mode = dbUser.OsuMode;

        await this.GetOsuProfileStringCommand(ctx, dbUser.OsuUsername, type, mode);
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
        [Choice("standard", "Osu")]
        [Choice("taiko", "Taiko")]
        [Choice("catch", "Fruits")]
        [Choice("mania", "Mania")]
        [Option("mode", "osu! mode to retrieve data")]
        string mode = "Osu")
    {
        await ctx.DeferAsync();

        Enum.TryParse(mode, out GameMode omode);

        try
        {
            IUser osuUser = await this._osuClient.GetUserAsync(username, omode);

            if (type == "profile")
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

                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                    .WithTimestamp(DateTime.Now)
                    .WithFooter($"Requested by {ctx.Member.DisplayName}#{ctx.Member.Discriminator}", ctx.Member.AvatarUrl)
                    .WithAuthor($"{osuUser.Username}'s osu! profile {(osuUser.IsSupporter ? DiscordEmoji.FromName(ctx.Client, ":heart:").ToString() : string.Empty)}", $"https://osu.ppy.sh/users/{osuUser.Id}")
                    .WithColor(this.OsuEmbedColor)
                    .AddField("Basic Information", sb.ToString())
                    .WithThumbnail(osuUser.AvatarUrl.ToString());

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"osu! profile of user {osuUser.Username} with mode {Formatter.Bold(omode.ToString())}")
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
                    return r is >= 1 and <= 100;
                });

                if (res.TimedOut)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Time exceeded"));

                    return;
                }

                IReadOnlyList<IScore> scores = new List<Score>();

                switch (type)
                {
                    case "best":
                        scores = await this._osuClient.GetUserScoresAsync(osuUser.Id, ScoreType.Best, false, omode, r);
                        break;
                    case "recent":
                        DiscordButtonComponent yesBtn = new DiscordButtonComponent(ButtonStyle.Success, "yesBtn", "Yes please!");
                        DiscordButtonComponent noBtn = new DiscordButtonComponent(ButtonStyle.Danger, "noBtn", "Probably not");

                        DiscordMessage failAsk = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                            .WithContent("Do you want to include fail scores?")
                            .AddComponents(yesBtn, noBtn));

                        var btnRes = await failAsk.WaitForButtonAsync(ctx.Member);

                        if (btnRes.TimedOut)
                        {
                            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                                .WithContent("Time exceeded"));

                            return;
                        }

                        bool includeFails = btnRes.Result.Id == "yesBtn";

                        scores = await this._osuClient.GetUserScoresAsync(osuUser.Id, ScoreType.Recent, includeFails, omode, r);
                        break;
                    case "firsts":
                        scores = await this._osuClient.GetUserScoresAsync(osuUser.Id, ScoreType.Firsts, false, omode, r);
                        break;
                }

                if (!scores.Any())
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent("I found nothing, sorry"));
                else
                {
                    DiscordFollowupMessageBuilder builder = new DiscordFollowupMessageBuilder();
                    StringBuilder sb = new StringBuilder();

                    foreach (IScore score in scores)
                    {
                        IBeatmap beatmap = score.Beatmap;

                        sb
                            .AppendLine($"{Formatter.Bold("Score")}: {score.TotalScore} - Global rank #{score.GlobalRank.GetValueOrDefault()}, Country rank #{score.CountryRank.GetValueOrDefault()}")
                            .AppendLine($"{Formatter.Bold("Ranking")}: {score.Rank}")
                            .AppendLine($"{Formatter.Bold("Accuracy")}: {score.Accuracy}%")
                            .AppendLine($"{Formatter.Bold("Combo")}: {score.MaxCombo}x/{beatmap.MaxCombo}x")
                            .AppendLine($"{Formatter.Bold("Hit Count")}: [{score.Statistics.Count300}/{score.Statistics.Count100}/{score.Statistics.Count50}/{score.Statistics.CountMiss}]")
                            .AppendLine($"{Formatter.Bold("PP")}: {score.PerformancePoints.GetValueOrDefault()}")
                            .AppendLine($"{Formatter.Bold("Submission Time")}: {score.CreatedAt:f}");

                        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                            .WithTimestamp(DateTime.Now)
                            .WithFooter($"Requested by {ctx.User.Username}#{ctx.User.Discriminator}", ctx.User.AvatarUrl)
                            .WithAuthor(
                                $"{(type == "best" ? "Best" : "Recent")} score of {osuUser.Username} in mode {omode}",
                                $"https://osu.ppy.sh/users/{osuUser.Id}", osuUser.AvatarUrl.ToString())
                            .WithColor(this.OsuEmbedColor)
                            .WithDescription(
                                $"Map: {Formatter.MaskedUrl($"{beatmap.Beatmapset.Artist} - {beatmap.Beatmapset.Title} [{beatmap.Version}] +{Formatter.Bold(score.Mods.ToString())}", new Uri(beatmap.Url))}")
                            .AddField("Score data", sb.ToString())
                            .WithThumbnail(beatmap.Beatmapset.Covers.Cover);

                        builder.AddEmbed(embedBuilder.Build());
                        sb.Clear();
                    }

                    builder.WithContent($"{(type == "best" ? "Best" : "Recent")} scores of user {osuUser.Username}");
                    await ctx.FollowUpAsync(builder);
                }
            }
        }
        catch (ApiException _)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Possibly no one has username {Formatter.Bold(username)} on osu!"));
        }
        catch (OsuDeserializationException _)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"An account with username {Formatter.Bold(username)} found, but they just don't play the mode you provided: {Formatter.Bold(omode.ToString())}"));
        }
        catch (InvalidOperationException ex)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something very bad happened when running"));
        }
    }
}
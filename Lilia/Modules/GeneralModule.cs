using System;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Lilia.Commons;
using Lilia.Services;

namespace Lilia.Modules;

public class GeneralModule : ApplicationCommandModule
{
    private readonly LiliaClient _client;

    public GeneralModule(LiliaClient client)
    {
        _client = client;
    }

    [SlashCommand("uptime", "Check the uptime of the bot")]
    public async Task UptimeCheckCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);

        var timeDiff = DateTime.Now.Subtract(_client.StartTime);
        StringBuilder uptimeStr = new();
        if (timeDiff.Days > 0) uptimeStr.Append($"{timeDiff.Days} day{(timeDiff.Days >= 2 ? "s" : string.Empty)} ");
        if (timeDiff.Hours > 0) uptimeStr.Append($"{timeDiff.Hours} hour{(timeDiff.Hours >= 2 ? "s" : string.Empty)} ");
        if (timeDiff.Minutes > 0) uptimeStr.Append($"{timeDiff.Minutes} minute{(timeDiff.Minutes >= 2 ? "s" : string.Empty)} ");
        if (timeDiff.Seconds > 0) uptimeStr.Append($"{timeDiff.Seconds} second{(timeDiff.Seconds >= 2 ? "s" : string.Empty)}");

        var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForMember()
            .WithTitle("My uptime")
            .AddField("Uptime", uptimeStr.ToString())
            .AddField("Start since", $"{_client.StartTime.ToLongDateString()}, {_client.StartTime.ToLongTimeString()}");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(embedBuilder.Build()));
    }

    [SlashCommand("info", "Something about me")]
    public async Task BotInfoCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);

        StringBuilder owners = new();

        ulong memberCount = 0;

        _client.JoinedGuilds.ForEach(guild => memberCount += Convert.ToUInt64(guild.MemberCount));

        foreach (var owner in ctx.Client.CurrentApplication.Owners)
            owners.AppendLine($"{owner.Username}#{owner.Discriminator}");

        // a delicious slash command please :D
        var botInv = ctx.Client.CurrentApplication.GenerateBotOAuth(LiliaClient.RequiredPermissions).Replace("scope=bot", "scope=bot%20applications.commands");
        var guildInv =  _client.BotConfiguration.Client.SupportGuildInviteLink;

        // dodge 400
        if (string.IsNullOrWhiteSpace(guildInv)) guildInv = "https://placehold.er";
        var isValidGuildInviteLink = guildInv.IsDiscordValidGuildInvite();

        var inviteBtn = new DiscordLinkButtonComponent(botInv, "Interested in me?");
        var supportGuildBtn = new DiscordLinkButtonComponent(guildInv, "Need supports?", !isValidGuildInviteLink);
        var selfHostBtn = new DiscordLinkButtonComponent("https://github.com/Swyreee/Lilia", "Want to host your own bot?");

        var timeDiff = DateTime.Now.Subtract(_client.StartTime);
        StringBuilder uptimeStr = new();
        if (timeDiff.Days > 0) uptimeStr.Append($"{timeDiff.Days} day{(timeDiff.Days >= 2 ? "s" : string.Empty)} ");
        if (timeDiff.Hours > 0) uptimeStr.Append($"{timeDiff.Hours} hour{(timeDiff.Hours >= 2 ? "s" : string.Empty)} ");
        if (timeDiff.Minutes > 0) uptimeStr.Append($"{timeDiff.Minutes} minute{(timeDiff.Minutes >= 2 ? "s" : string.Empty)} ");
        if (timeDiff.Seconds > 0) uptimeStr.Append($"{timeDiff.Seconds} second{(timeDiff.Seconds >= 2 ? "s" : string.Empty)}");

        var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForMember()
            .WithTitle("Something about me :D")
            .WithThumbnail(ctx.Client.CurrentUser.AvatarUrl)
            .WithDescription($"Hi, I am {Formatter.Bold($"{ctx.Client.CurrentUser.Username}#{ctx.Client.CurrentUser.Discriminator}")}, a bot running on the source code of {Formatter.Bold("Lilia")} written by {Formatter.Bold("Swyrin#7193")}")
            .AddField("Server count", _client.JoinedGuilds.Count.ToString(), true)
            .AddField("Member count", memberCount.ToString(), true)
            .AddField("Owner(s)", owners.ToString())
            .AddField("Uptime", $"{Formatter.Bold(uptimeStr.ToString())} since {_client.StartTime.ToLongDateString()}, {_client.StartTime.ToLongTimeString()}")
            .AddField("How to invite me?", "Either click the \"Interested in me?\" button below or click on me, choose \"Add to Server\" if it exists");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(embedBuilder.Build())
            .AddComponents(inviteBtn, supportGuildBtn, selfHostBtn));
    }
}
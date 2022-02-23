using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Lilia.Commons;
using Lilia.Services;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lilia.Modules;

public class GeneralModule : ApplicationCommandModule
{
    private readonly LiliaClient _client;

    public GeneralModule(LiliaClient client)
    {
        _client = client;
    }

    [SlashCommand("uptime", "Check the uptime of the bot")]
    public async Task GeneralUptimeCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);

        var timeDiff = DateTime.Now.Subtract(_client.StartTime);

        var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForUser()
            .WithAuthor("My uptime", null, ctx.Client.CurrentUser.AvatarUrl)
            .AddField("Uptime", timeDiff.ToLongReadableTimeSpan(), true)
            .AddField("Start since", _client.StartTime.ToLongDateTime(), true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(embedBuilder.Build()));
    }

    [SlashCommand("bot", "Something about me")]
    public async Task GeneralBotCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);

        StringBuilder owners = new();

        ulong memberCount = 0;

        LiliaClient.JoinedGuilds.ForEach(guild => memberCount += Convert.ToUInt64(guild.MemberCount));

        foreach (var owner in ctx.Client.CurrentApplication.Owners)
            owners.Append(owner.Username).Append('#').AppendLine(owner.Discriminator);

        // a delicious slash command please :D
        var botInv = ctx.Client.CurrentApplication.GenerateBotOAuth(LiliaClient.RequiredPermissions).Replace("scope=bot", "scope=bot%20applications.commands");
        var guildInv = LiliaClient.BotConfiguration.Client.SupportGuildInviteLink;

        // dodge 400
        if (string.IsNullOrWhiteSpace(guildInv)) guildInv = "https://placehold.er";
        var isValidGuildInviteLink = guildInv.IsDiscordValidGuildInvite();

        var inviteBtn = new DiscordLinkButtonComponent(botInv, "Interested in me?", !ctx.Client.CurrentApplication.IsPublic.GetValueOrDefault());
        var supportGuildBtn = new DiscordLinkButtonComponent(guildInv, "Need supports?", !isValidGuildInviteLink);
        var selfHostBtn = new DiscordLinkButtonComponent("https://github.com/Lilia-Workshop/Lilia", "Want to host your own bot?");

        var timeDiff = DateTime.Now.Subtract(_client.StartTime);

        var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForUser()
            .WithTitle("Something about me :D")
            .WithThumbnail(ctx.Client.CurrentUser.AvatarUrl)
            .WithDescription(
                $"Hi, I am {Formatter.Bold($"{ctx.Client.CurrentUser.Username}#{ctx.Client.CurrentUser.Discriminator}")}, a bot running on the source code of {Formatter.Bold("Lilia")} written by {Formatter.Bold("Swyrin#7193")}")
            .AddField("Server count", LiliaClient.JoinedGuilds.Count.ToString(), true)
            .AddField("Member count", memberCount.ToString(), true)
            .AddField("Owner(s)", owners.ToString())
            .AddField("Uptime",
                $"{Formatter.Bold(timeDiff.ToLongReadableTimeSpan())} since {_client.StartTime.ToLongDateString()}, {_client.StartTime.ToLongTimeString()}")
            .AddField("Version", $"{Assembly.GetExecutingAssembly().GetName().Version}", true)
            .AddField("Command count", $"{(await ctx.Client.GetGlobalApplicationCommandsAsync()).Count}", true)
            .AddField("How to invite me?",
                $"Either click the {Formatter.Bold("Interested in me?")} button below or click on me, choose {Formatter.Bold("Add to Server")} if it exists");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(embedBuilder.Build())
            .AddComponents(inviteBtn, supportGuildBtn, selfHostBtn));;
    }

    [SlashCommand("user", "What I know about an user in this guild")]
    public async Task GeneralUserInfoCommand(InteractionContext ctx,
        [Option("user", "An user in this guild")]
        DiscordUser user)
    {
        await ctx.DeferAsync(true);

        var member = (DiscordMember) user;

        if (member == ctx.Client.CurrentUser)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"If you want to know about me, there is a {Formatter.InlineCode("/bot")} command for it"));
            
            return;
        }
        
        var creationDate = member.CreationTimestamp.DateTime;
        var joinDate = member.JoinedAt.DateTime;
        var accountAge = DateTimeOffset.Now.Subtract(creationDate);
        var membershipAge = DateTimeOffset.Now.Subtract(joinDate);

        var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForUser()
            .WithAuthor($"What I know about {member.DisplayName}#{member.Discriminator} :D", null,ctx.Client.CurrentUser.AvatarUrl)
            .WithThumbnail(member.GuildAvatarUrl ?? member.AvatarUrl)
            .WithDescription($"User ID: {member.Id}")
            .AddField("Account age", $"{accountAge.ToShortReadableTimeSpan()} (since {creationDate.ToShortDateTime()})")
            .AddField("Membership age", $"{membershipAge.ToShortReadableTimeSpan()} (since {joinDate.ToShortDateTime()})")
            .AddField("Is this guild owner?", member.IsOwner.ToString(), true)
            .AddField("Is a bot?", member.IsBot ? "Hello fellow bot :D" : "Probably not", true)
            .AddField("Badge list", member.Flags.ToString() ?? "None", true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(embedBuilder.Build()));
    }
    
    [SlashCommand("guild", "What I know about this guild")]
    public async Task GeneralGuildCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);

        var guild = ctx.Guild;
        
        // Discord includes only me and the bot
        // so I guess this is a good workaround?
        var members = await guild.GetAllMembersAsync();

        var creationDate = guild.CreationTimestamp.DateTime;
        var guildAge = DateTimeOffset.Now.Subtract(creationDate);
        var memberCount = members.Count;
        var botList = members.Where(member => member.IsBot).ToList();
        var botCount = botList.Count;
        var humanCount = memberCount - botCount;

        var isBoosted = guild.PremiumSubscriptionCount > 0;

        var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForUser()
            .WithAuthor(guild.Name, null, guild.IconUrl)
            .WithThumbnail(guild.BannerUrl)
            .WithDescription($"Guild ID: {guild.Id} - Owner {Formatter.Mention(guild.Owner)}")
            .AddField("Guild age", $"{guildAge.ToShortReadableTimeSpan()} (since {creationDate.ToShortDateTime()})")
            .AddField("Humans", $"{humanCount}", true)
            .AddField("Bots", $"{botCount}", true)
            .AddField("Total", $"{memberCount}", true)
            .AddField("Channels count",
                $"{guild.Channels.Count} with {guild.Threads.Count(x => !x.Value.IsPrivate)} public threads", true)
            .AddField("Roles", $"{guild.Roles.Count}", true)
            .AddField("Events", $"{guild.ScheduledEvents.Count} pending", true)
            .AddField("Boosts",
                isBoosted
                    ? $"{guild.PremiumSubscriptionCount ?? 0} boost(s) (lvl. {(int) guild.PremiumTier}) from {guild.PremiumSubscriptionCount ?? 0} booster(s)"
                    : "Not having any boosts");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(embedBuilder.Build()));
    }
}
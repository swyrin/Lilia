using Lilia.Commons;
using Lilia.Services;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Lilia.Modules;

public class GeneralModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LiliaClient _client;

    public GeneralModule(LiliaClient client)
    {
        _client = client;
    }

    [SlashCommand("uptime", "Check the uptime of the bot")]
    public async Task GeneralUptimeCommand()
    {
        await Context.Interaction.DeferAsync(true);

        var timeDiff = DateTime.Now.Subtract(_client.StartTime);

        var embedBuilder = Context.User.CreateEmbedWithUserData()
            .WithAuthor("My uptime", null, Context.Client.CurrentUser.GetAvatarUrl())
            .AddField("Uptime", timeDiff.ToLongReadableTimeSpan(), true)
            .AddField("Start since", _client.StartTime.ToLongDateTime(), true);

        await Context.Interaction.ModifyOriginalResponseAsync(x =>
            x.Embed = embedBuilder.Build());
    }

    [SlashCommand("bot", "Something about me")]
    public async Task GeneralBotCommand()
    {
        await Context.Interaction.DeferAsync(true);

        ulong memberCount = Context.Client.Guilds.Aggregate<SocketGuild, ulong>(0, (current, guild) => current + Convert.ToUInt64(guild.MemberCount));

        var botId = Context.Client.CurrentUser.Id;
        const GuildPermission perms = LiliaClient.RequiredPermissions;
        var botInv = $"https://discord.com/api/oauth2/authorize?client_id={botId}&permissions={perms}&scope=bot%20applications.commands";
        var guildInv = LiliaClient.BotConfiguration.Client.SupportGuildInviteLink;

        // dodge 400
        if (string.IsNullOrWhiteSpace(guildInv)) guildInv = "https://placehold.er";
        var isValidGuildInviteLink = guildInv.IsDiscordValidGuildInvite();

        var componentBuilder = new ComponentBuilder()
            .WithButton("Interested in me?", "inviteBtn", ButtonStyle.Link, url: botInv,
                disabled: !(await Context.Client.GetApplicationInfoAsync()).IsBotPublic)
            .WithButton("Need supports?", "supportGuildBtn", ButtonStyle.Link, url: guildInv,
                disabled: !isValidGuildInviteLink)
            .WithButton("Want to self host?", "selfHostBtn", ButtonStyle.Link,
                url: "https://github.com/Lilia-Workshop/Lilia");

        var timeDiff = DateTime.Now.Subtract(_client.StartTime);

        var embedBuilder = Context.User.CreateEmbedWithUserData()
            .WithTitle("Something about me :D")
            .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
            .WithDescription(
                $"Hi, I am {Format.Bold(Format.UsernameAndDiscriminator(Context.Client.CurrentUser))}, a bot running on the source code of {Format.Bold("Lilia")} written by {Format.Bold("Swyrin#7193")}")
            .AddField("Server count", $"{Context.Client.Guilds.Count}", true)
            .AddField("Member count", $"{memberCount}", true)
            .AddField("Uptime",
                $"{Format.Bold(timeDiff.ToLongReadableTimeSpan())} since {_client.StartTime.ToLongDateString()}, {_client.StartTime.ToLongTimeString()}")
            .AddField("Version", $"{Assembly.GetExecutingAssembly().GetName().Version}", true)
            .AddField("Command count", $"{(await Context.Client.GetGlobalApplicationCommandsAsync()).Count}", true)
            .AddField("How to invite me?",
                $"Either click the {Format.Bold("Interested in me?")} button below or click on me, choose {Format.Bold("Add to Server")} if it exists");

        await Context.Interaction.ModifyOriginalResponseAsync(x =>
        {
            x.Embed = embedBuilder.Build();
            x.Components = componentBuilder.Build();
        });
    }

    [SlashCommand("user", "What I know about an user in this guild")]
    public async Task GeneralUserCommand(
        [Summary("member", "A member in this guild")]
        SocketGuildUser member)
    {
        await Context.Interaction.DeferAsync(true);
        
        if (member.Id == Context.Client.CurrentUser.Id)
        {
            await Context.Interaction.ModifyOriginalResponseAsync(x =>
                x.Content = $"If you want to know about me, there is a {Format.Bold("/bot")} command for it");
            
            return;
        }
        
        var creationDate = member.CreatedAt.DateTime;
        var joinDate = member.JoinedAt.GetValueOrDefault().DateTime;
        var accountAge = DateTimeOffset.Now.Subtract(creationDate);
        var membershipAge = DateTimeOffset.Now.Subtract(joinDate);

        var embedBuilder = Context.User.CreateEmbedWithUserData()
            .WithAuthor($"What I know about {member.Username}#{member.Discriminator} :D", null,
                Context.Client.CurrentUser.GetDefaultAvatarUrl())
            .WithThumbnailUrl(member.GetGuildAvatarUrl())
            .WithDescription($"User ID: {member.Id}")
            .AddField("Account age", $"{accountAge.ToShortReadableTimeSpan()} (since {creationDate.ToShortDateTime()})")
            .AddField("Membership age",
                $"{membershipAge.ToShortReadableTimeSpan()} (since {joinDate.ToShortDateTime()})")
            .AddField("Is guild owner?", Context.Guild.Owner == member, true)
            .AddField("Is a bot?", member.IsBot ? "Hello fellow bot :D" : "Probably not", true)
            .AddField("Badge list", $"{member.PublicFlags}", true)
            .AddField("Mutual server count with me", member.MutualGuilds);

        await Context.Interaction.ModifyOriginalResponseAsync(x =>
            x.Embed = embedBuilder.Build());
    }
    
    [SlashCommand("guild", "What I know about this guild")]
    public async Task GeneralGuildCommand()
    {
        await Context.Interaction.DeferAsync(true);

        var guild = Context.Guild;
        await Context.Client.DownloadUsersAsync(new [] { guild });
        var members = Context.Guild.Users.ToList();
        
        var creationDate = guild.CreatedAt.DateTime;
        var guildAge = DateTimeOffset.Now.Subtract(creationDate);
        var memberCount = members.Count;
        var botList = members.Where(member => member.IsBot).ToList();
        var botCount = botList.Count;
        var humanCount = memberCount - botCount;

        var isBoosted = guild.PremiumSubscriptionCount > 0;
        var boosters = members.Where(x => x.PremiumSince != null);

        var embedBuilder = Context.User.CreateEmbedWithUserData()
            .WithAuthor(guild.Name, null, guild.IconUrl)
            .WithThumbnailUrl(guild.BannerUrl)
            .WithDescription($"Guild ID: {guild.Id} - Owner: <@{guild.Owner.Id}>")
            .AddField("Guild age", $"{guildAge.ToShortReadableTimeSpan()} (since {creationDate.ToShortDateTime()})")
            .AddField("Humans", $"{humanCount}", true)
            .AddField("Bots", $"{botCount}", true)
            .AddField("Total", $"{memberCount}", true)
            .AddField("Channels count", $"{guild.Channels.Count} with {guild.ThreadChannels.Count(x => !x.IsPrivateThread)} public threads", true)
            .AddField("Roles", $"{guild.Roles.Count}", true)
            .AddField("Events", $"{guild.Events.Count} pending", true)
            .AddField("Boosts",
                isBoosted
                    ? $"{guild.PremiumSubscriptionCount} boost(s) (lvl. {(int) guild.PremiumTier}) from {boosters.Count()} booster(s)"
                    : "Not having any boosts");

        await Context.Interaction.ModifyOriginalResponseAsync(x =>
            x.Embed = embedBuilder.Build());
    }
}
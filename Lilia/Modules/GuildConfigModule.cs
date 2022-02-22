using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Extensions;
using Lilia.Database.Models;
using Lilia.Services;

namespace Lilia.Modules;

[SlashCommandGroup("config", "Server configuration")]
public class GuildConfigModule : ApplicationCommandModule
{
    private readonly LiliaDatabaseContext _dbCtx;

    public GuildConfigModule(LiliaDatabase database)
    {
        _dbCtx = database.GetContext();
    }

    [SlashCommand("welcome_channel", "Set the welcome channel")]
    [SlashRequireUserPermissions(Permissions.ManageGuild)]
    public async Task SetWelcomeChannelCommand(InteractionContext ctx,
        [Option("channel", "Channel to dump all welcome messages")]
        [ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Store)]
        DiscordChannel channel)
    {
        await ctx.DeferAsync(true);

        DbGuild dbGuild = _dbCtx.GetGuildRecord(ctx.Guild);
        dbGuild.WelcomeChannelId = channel.Id;
        await _dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Set the welcome channel of this guild to {Formatter.Mention(channel)}"));
    }

    [SlashCommand("goodbye_channel", "Set the goodbye channel")]
    [SlashRequireUserPermissions(Permissions.ManageGuild)]
    public async Task SetGoodbyeChannelCommand(InteractionContext ctx,
        [Option("channel", "Channel to dump all goodbye messages")]
        [ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Store)]
        DiscordChannel channel)
    {
        await ctx.DeferAsync(true);

        DbGuild dbGuild = _dbCtx.GetGuildRecord(ctx.Guild);
        dbGuild.GoodbyeChannelId = channel.Id;
        await _dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Set the goodbye channel of this guild to {Formatter.Mention(channel)}"));
    }

    [SlashCommand("goodbye_message", "Set the goodbye message")]
    [SlashRequireUserPermissions(Permissions.ManageGuild)]
    public async Task SetGoodbyeMessageCommand(InteractionContext ctx,
        [Option("message", "Goodbye message, see \"/config placeholders\" for placeholders")]
        string message)
    {
        await ctx.DeferAsync(true);

        DbGuild dbGuild = _dbCtx.GetGuildRecord(ctx.Guild);

        if (dbGuild.GoodbyeChannelId == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You did not set a goodbye channel in this guild"));

            return;
        }

        dbGuild.GoodbyeMessage = message;
        await _dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Set the goodbye message of this guild: {Formatter.InlineCode(message)}"));
    }

    [SlashCommand("welcome_message", "Set the welcome message")]
    [SlashRequireUserPermissions(Permissions.ManageGuild)]
    public async Task SetWelcomeMessageCommand(InteractionContext ctx,
        [Option("message", "Welcome message, see \"/config placeholders\" for placeholders")]
        string message)
    {
        await ctx.DeferAsync(true);

        DbGuild dbGuild = _dbCtx.GetGuildRecord(ctx.Guild);

        if (dbGuild.WelcomeChannelId == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You did not set a welcome channel in this guild"));

            return;
        }

        dbGuild.WelcomeMessage = message;
        await _dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Set the goodbye message of this guild: {Formatter.InlineCode(message)}"));
    }

    [SlashCommand("welcome", "Toggle welcome message allowance in this guild")]
    [SlashRequireUserPermissions(Permissions.ManageGuild)]
    public async Task ToggleWelcomeCommand(InteractionContext ctx,
        [Option("toggle", "Whether to allow it or not")]
        bool toggle = true)
    {
        await ctx.DeferAsync(true);

        DbGuild dbGuild = _dbCtx.GetGuildRecord(ctx.Guild);

        if (dbGuild.WelcomeChannelId == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You did not set a welcome channel in this guild"));

            return;
        }

        if (string.IsNullOrWhiteSpace(dbGuild.WelcomeMessage))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You did not set a welcome message in this guild"));

            return;
        }

        dbGuild.IsWelcomeEnabled = toggle;
        await _dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent(
                $"{(dbGuild.IsWelcomeEnabled ? "Allowed" : "Blocked")} the delivery of welcome message in this guild"));
    }

    [SlashCommand("goodbye", "Toggle goodbye message allowance in this guild")]
    [SlashRequireUserPermissions(Permissions.ManageGuild)]
    public async Task ToggleGoodbyeCommand(InteractionContext ctx,
        [Option("toggle", "Whether to allow it or not")]
        bool toggle = true)
    {
        await ctx.DeferAsync(true);

        DbGuild dbGuild = _dbCtx.GetGuildRecord(ctx.Guild);

        if (dbGuild.GoodbyeChannelId == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You did not set a goodbye channel in this guild"));

            return;
        }

        if (string.IsNullOrWhiteSpace(dbGuild.GoodbyeMessage))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You did not set a goodbye message in this guild"));

            return;
        }

        dbGuild.IsGoodbyeEnabled = toggle;
        await _dbCtx.SaveChangesAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent(
                $"{(dbGuild.IsGoodbyeEnabled ? "Allowed" : "Blocked")} the delivery of goodbye message in this guild"));
    }
    
    [SlashCommand("placeholders", "Get all available configuration placeholders")]
    [SlashRequireUserPermissions(Permissions.ManageGuild)]
    public async Task GetPlaceholdersCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);

        var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForUser()
            .WithAuthor("Available placeholders", null, ctx.Client.CurrentUser.AvatarUrl)
            .AddField("{name} - The user's username", "Example: Swyrin#7193 -> {name} = Swyrin\n" +
                                                      "Restrictions: None")
            .AddField("{tag} - The user's username", "Example: Swyrin#7193 -> {tag} = 7193\n" +
                                                     "Restrictions: None")
            .AddField("{guild} - The guild's name", $"Example: {{guild}} = {ctx.Guild.Name}\n" +
                                                    "Restrictions: None")
            .AddField("{@user} - User mention",
                $"Example: Swyrin#7193 -> {{@user}} = {Formatter.Mention(ctx.Member)}\n" +
                "Restrictions: Welcome message only");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(embedBuilder));
    }

    [SlashCommand("check", "Check the configurations you have made")]
    public async Task GetConfigurationsCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);

        var dbGuild = _dbCtx.GetGuildRecord(ctx.Guild);

        var welcomeChn = ctx.Guild.GetChannel(dbGuild.WelcomeChannelId);
        var goodbyeChn = ctx.Guild.GetChannel(dbGuild.GoodbyeChannelId);

        var welcomeChnMention = welcomeChn == null ? "Channel not exist or not set" : Formatter.Mention(welcomeChn);
        var goodbyeChnMention = goodbyeChn == null ? "Channel not exist or not set" : Formatter.Mention(goodbyeChn);

        var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForUser()
            .WithAuthor("All configurations", null, ctx.Client.CurrentUser.AvatarUrl)
            .AddField("Welcome message",
                string.IsNullOrWhiteSpace(dbGuild.WelcomeMessage) ? "None" : dbGuild.WelcomeMessage, true)
            .AddField("Welcome channel", welcomeChnMention, true)
            .AddField("Welcome message allowed", $"{dbGuild.IsWelcomeEnabled}", true)
            .AddField("Goodbye message",
                string.IsNullOrWhiteSpace(dbGuild.GoodbyeMessage) ? "None" : dbGuild.GoodbyeMessage, true)
            .AddField("Goodbye channel", goodbyeChnMention, true)
            .AddField("Goodbye message allowed", $"{dbGuild.IsGoodbyeEnabled}", true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(embedBuilder));
    }
}

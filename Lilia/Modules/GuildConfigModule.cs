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
    [SlashCommandGroup("get", "Get a configuration property")]
    public class GuildConfigGetModule
    {
        private LiliaDatabaseContext _dbCtx;

        public GuildConfigGetModule(LiliaDatabase database)
        {
            _dbCtx = database.GetContext();
        }
        
        [SlashCommand("welcome_channel", "Get the configured welcome channel")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task GetWelcomeChannelCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync(true);

            var dbGuild = _dbCtx.GetGuildRecord(ctx.Guild);
            
            if (dbGuild.WelcomeChannelId == 0)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("You did not set a welcome channel in this guild"));

                return;
            }
            
            var channel = ctx.Guild.GetChannel(dbGuild.WelcomeChannelId);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"The welcome channel of this guild is {Formatter.Mention(channel)}"));
        }

        [SlashCommand("goodbye_channel", "Get the goodbye channel")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task GetGoodbyeChannelCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync(true);

            var dbGuild = _dbCtx.GetGuildRecord(ctx.Guild);
            
            if (dbGuild.GoodbyeChannelId == 0)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("You did not set a welcome channel in this guild"));

                return;
            }
            
            var channel = ctx.Guild.GetChannel(dbGuild.GoodbyeChannelId);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"The goodbye channel of this guild is {Formatter.Mention(channel)}"));
        }

        [SlashCommand("goodbye_message", "Get the goodbye message")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task GetGoodbyeMessageCommand(InteractionContext ctx)
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
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"The goodbye message of this guild: {Formatter.InlineCode(dbGuild.GoodbyeMessage)}"));
        }

        [SlashCommand("welcome_message", "Set the welcome message")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task GetWelcomeMessageCommand(InteractionContext ctx)
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
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"The welcome message of this guild: {Formatter.InlineCode(dbGuild.WelcomeMessage)}"));;
        }
    }
    
    [SlashCommandGroup("set", "Set a configuration property")]
    public class GuildConfigSetModule : ApplicationCommandModule
    {
        private LiliaDatabaseContext _dbCtx;

        public GuildConfigSetModule(LiliaDatabase database)
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
    }

    [SlashCommandGroup("toggle", "Toggle a configuration property")]
    public class GuildConfigToggleModule : ApplicationCommandModule
    {
        private LiliaDatabaseContext _dbCtx;

        public GuildConfigToggleModule(LiliaDatabase database)
        {
            _dbCtx = database.GetContext();
        }
        
        [SlashCommand("toggle_welcome", "Toggle my welcome message allowance in this guild")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task ToggleWelcomeCommand(InteractionContext ctx)
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

            dbGuild.IsWelcomeEnabled = !dbGuild.IsWelcomeEnabled;
            await _dbCtx.SaveChangesAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(
                    $"{(dbGuild.IsWelcomeEnabled ? "Enabled" : "Disabled")} the delivery of welcome message in this guild"));
        }

        [SlashCommand("toggle_goodbye", "Toggle my goodbye message allowance in this guild")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task ToggleGoodbyeCommand(InteractionContext ctx)
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

            dbGuild.IsGoodbyeEnabled = !dbGuild.IsGoodbyeEnabled;
            await _dbCtx.SaveChangesAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(
                    $"{(dbGuild.IsGoodbyeEnabled ? "Enabled" : "Disabled")} the delivery of goodbye message in this guild"));
        }
    }

    [SlashCommandGroup("util", "Some utilities")]
    public class GuildConfigUtilModule : ApplicationCommandModule
    {
        private LiliaDatabaseContext _dbCtx;

        public GuildConfigUtilModule(LiliaDatabase database)
        {
            _dbCtx = database.GetContext();
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
        public async Task GetConfiguraionsCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync(true);
            
            var dbGuild = _dbCtx.GetGuildRecord(ctx.Guild);

            var welcomeChn = ctx.Guild.GetChannel(dbGuild.WelcomeChannelId);
            var goodbyeChn = ctx.Guild.GetChannel(dbGuild.GoodbyeChannelId);

            var embedBuilder = ctx.Member.GetDefaultEmbedTemplateForUser()
                .WithAuthor("All configurations", null, ctx.Client.CurrentUser.AvatarUrl)
                .AddField("Welcome message",
                    string.IsNullOrWhiteSpace(dbGuild.WelcomeMessage) ? "None" : dbGuild.WelcomeMessage, true)
                .AddField("Welcome channel", Formatter.Mention(welcomeChn), true)
                .AddField("Welcome message allowed", $"{dbGuild.IsWelcomeEnabled}", true)
                .AddField("Goodbye message",
                    string.IsNullOrWhiteSpace(dbGuild.GoodbyeMessage) ? "None" : dbGuild.GoodbyeMessage, true)
                .AddField("Goodbye channel", Formatter.Mention(goodbyeChn), true)
                .AddField("Goodbye message allowed", $"{dbGuild.IsGoodbyeEnabled}", true);
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(embedBuilder));
        }
    }
}

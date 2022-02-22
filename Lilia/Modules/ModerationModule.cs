using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Extensions;
using Lilia.Services;
using System;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Interactivity.Enums;
using Lilia.Modules.Utils;

namespace Lilia.Modules;

[SlashCommandGroup("mod", "Moderation commands")]
public class ModerationModule : ApplicationCommandModule
{
    [SlashCommandGroup("general", "General command for moderating members")]
    public class ModerationGeneralModule : ApplicationCommandModule
    {
        private readonly LiliaDatabaseContext _dbCtx;
        private const string MuteRoleName = "Lilia-mute";

        public ModerationGeneralModule(LiliaDatabase database)
        {
            _dbCtx = database.GetContext();
        }

        [SlashCommand("ban", "Ban members, in batch")]
        [SlashRequirePermissions(Permissions.BanMembers)]
        public async Task ModerationGeneralBanCommand(InteractionContext ctx,
            [Option("reason", "Reason to execute")]
            string reason = "Rule violation")
        {
            await ctx.DeferAsync();
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"{Formatter.Bold("Mention")} all the users you want to ban with reason {Formatter.Bold(reason)}"));

            var mentionedUsers = await ModerationModuleUtil.GetMentionedUsersAsync(ctx);

            StringBuilder stringBuilder = new();

            foreach (var discordUser in mentionedUsers)
            {
                var mentionedMember = await ctx.Guild.GetMemberAsync(discordUser.Id);

                if (mentionedMember == ctx.Member || mentionedMember == ctx.Client.CurrentUser)
                {
                    stringBuilder.AppendLine("Skipped because it is either you or me");
                    continue;
                }
                
                var execLine = $"Banning {Formatter.Bold($"{mentionedMember.Username}#{mentionedMember.Discriminator}")}";
                stringBuilder.AppendLine(execLine);

                await mentionedMember.BanAsync(0, reason);
                var now = DateTime.Now;
                
                var embedBuilder = new DiscordEmbedBuilder()
                    .WithAuthor(iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                    .WithTitle($"You have been banned from guild \"{ctx.Guild.Name}\" (ID: {ctx.Guild.Id})")
                    .WithThumbnail(ctx.Guild.IconUrl)
                    .AddField("Reason", reason, true)
                    .AddField("Moderator", $"{ctx.Member.DisplayName}#{ctx.Member.Discriminator} (ID: {ctx.Member.Id})", true)
                    .AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
                    .AddField("What to do now?",$"If you believe this was a mistake, you can try sending an appeal using {Formatter.InlineCode("mod message appeal")} with provided IDs");
                
                if (!mentionedMember.IsBot)
                    await mentionedMember.SendMessageAsync(embedBuilder);                
            }

            var pages = ctx.Client.GetInteractivity().GeneratePagesInEmbed($"{stringBuilder}", SplitType.Line);
            await ctx.Interaction.SendPaginatedResponseAsync(false, ctx.Member, pages);
        }
        
        [SlashCommand("kick", "Kick members, in batch")]
        [SlashRequirePermissions(Permissions.BanMembers)]
        public async Task ModerationGeneralKickCommand(InteractionContext ctx,
            [Option("reason", "Reason to execute")]
            string reason = "Rule violation")
        {
            await ctx.DeferAsync();
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"{Formatter.Bold("Mention")} all the users you want to kick with reason {Formatter.Bold(reason)}"));

            var mentionedUsers = await ModerationModuleUtil.GetMentionedUsersAsync(ctx);

            StringBuilder stringBuilder = new();

            foreach (var discordUser in mentionedUsers)
            {
                var mentionedMember = await ctx.Guild.GetMemberAsync(discordUser.Id);

                if (mentionedMember == ctx.Member || mentionedMember == ctx.Client.CurrentUser)
                {
                    stringBuilder.AppendLine("Skipped because it is either you or me");
                    continue;
                }
                
                var execLine = $"Kicking {Formatter.Bold($"{mentionedMember.Username}#{mentionedMember.Discriminator}")}";
                stringBuilder.AppendLine(execLine);

                await mentionedMember.RemoveAsync(reason);
                var now = DateTime.Now;
                
                var embedBuilder = new DiscordEmbedBuilder()
                    .WithAuthor(iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                    .WithTitle($"You have been kicked from guild \"{ctx.Guild.Name}\" (ID: {ctx.Guild.Id})")
                    .WithThumbnail(ctx.Guild.IconUrl)
                    .AddField("Reason", reason, true)
                    .AddField("Moderator", $"{ctx.Member.DisplayName}#{ctx.Member.Discriminator} (ID: {ctx.Member.Id})", true)
                    .AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
                    .AddField("What to do now?",$"If you believe this was a mistake, you can try sending an appeal using {Formatter.InlineCode("mod message appeal")} with provided IDs");

                if (!mentionedMember.IsBot)
                    await mentionedMember.SendMessageAsync(embedBuilder);                
            }

            var pages = ctx.Client.GetInteractivity().GeneratePagesInEmbed($"{stringBuilder}", SplitType.Line);
            await ctx.Interaction.SendPaginatedResponseAsync(false, ctx.Member, pages);
        }

        [SlashCommand("warn_add", "Add a warn to an user")]
        public async Task ModerationGeneralWarnAddCommand(InteractionContext ctx,
            [Option("user", "The target user to add a warn")]
            DiscordUser user,
            [Option("reason", "Reason to execute")]
            string reason = "Rule violation")
        {
            await ctx.DeferAsync();
            
            var mentionedMember = (DiscordMember) user;

            if (mentionedMember == ctx.Member || mentionedMember == ctx.Client.CurrentUser)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Skipped because it is either you or me"));

                return;
            }

            var dbUser = _dbCtx.GetUserRecord(mentionedMember);

            if (dbUser.WarnCount == 3)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"{Formatter.Mention(mentionedMember)} has been warned and muted earlier"));
                
                return;
            }

            dbUser.WarnCount += 1;

            var (isExistedInPast, discordRole) = await ModerationModuleUtil.GetOrCreateRoleAsync(ctx, MuteRoleName, "Mute role creation", Permissions.None);

            if (!isExistedInPast)
            {
                foreach (var channel in ctx.Guild.Channels)
                {
                    await channel.Value.AddOverwriteAsync(discordRole, Permissions.None, Permissions.SendMessages, "Muted");
                }    
            }
            
            if (dbUser.WarnCount == 3) await mentionedMember.GrantRoleAsync(discordRole);
            
            var now = DateTime.Now;
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Added a warn of {Formatter.Mention(mentionedMember)}\nNow they have {dbUser.WarnCount} warn(s)"));

            var embedBuilder = new DiscordEmbedBuilder()
                .WithAuthor(iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                .WithTitle($"You were warned in guild \"{ctx.Guild.Name}\" (ID: {ctx.Guild.Id})")
                .WithThumbnail(ctx.Guild.IconUrl)
                .AddField("Reason", reason, true)
                .AddField("Moderator", $"{ctx.Member.DisplayName}#{ctx.Member.Discriminator} (ID: {ctx.Member.Id})",
                    true)
                .AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
                .AddField("What to do now?",
                    $"If you believe this was a mistake, you can try sending an appeal using {Formatter.InlineCode("mod message appeal")} with provided IDs");

            if (!mentionedMember.IsBot)
                await mentionedMember.SendMessageAsync(embedBuilder);
            
            await _dbCtx.SaveChangesAsync();
        }
        
        [SlashCommand("warn_remove", "Remove a warn from an user")]
        public async Task ModerationGeneralWarnRemoveCommand(InteractionContext ctx,
            [Option("user", "The target user to remove a warn")]
            DiscordUser user,
            [Option("reason", "Reason to execute")]
            string reason = "False warn")
        {
            await ctx.DeferAsync();
            
            var mentionedMember = (DiscordMember) user;

            if (mentionedMember == ctx.Member || mentionedMember == ctx.Client.CurrentUser)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Skipped because it is either you or me"));

                return;
            }

            var dbUser = _dbCtx.GetUserRecord(mentionedMember);

            switch (dbUser.WarnCount)
            {
                case 3:
                {
                    var (isExistedInPast, liliaMuteRole) = await ModerationModuleUtil.GetOrCreateRoleAsync(ctx, MuteRoleName, "Mute role creation", Permissions.None);

                    if (!isExistedInPast)
                    {
                        foreach (var channel in ctx.Guild.Channels)
                        {
                            await channel.Value.AddOverwriteAsync(liliaMuteRole, Permissions.None, Permissions.SendMessages, "Muted");
                        }    
                    }

                    await mentionedMember.RevokeRoleAsync(liliaMuteRole);
                    break;
                }
                case 0:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("No warn to remove"));

                    return;
            }

            dbUser.WarnCount -= 1;

            var now = DateTime.Now;
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Removed a warn of {Formatter.Mention(mentionedMember)}\nNow they have {dbUser.WarnCount} warn(s)"));

            var embedBuilder = new DiscordEmbedBuilder()
                .WithAuthor(iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                .WithTitle($"You have been removed a warn in guild \"{ctx.Guild.Name}\" (ID: {ctx.Guild.Id})")
                .WithThumbnail(ctx.Guild.IconUrl)
                .AddField("Reason", reason, true)
                .AddField("Moderator", $"{ctx.Member.DisplayName}#{ctx.Member.Discriminator} (ID: {ctx.Member.Id})", true)
                .AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
                .AddField("What to do now?", "Nothing");

            if (!mentionedMember.IsBot)
                await mentionedMember.SendMessageAsync(embedBuilder);
            
            await _dbCtx.SaveChangesAsync();
        }
    }

    [SlashCommandGroup("notice", "Commands for sending global notices")]
    public class ModerationNoticeModule : ApplicationCommandModule
    {
        [SlashCommand("send", "Send a notification to a channel")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task SendNoticeCommand(InteractionContext ctx,
            [Option("target_message_jump_link", "Target message jump link, must be a message in this guild")]
            string msgJump,
            [ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Store, ChannelType.NewsThread, ChannelType.PublicThread)]
            [Option("target_channel", "Target channel to send")]
            DiscordChannel channel)
        {
            await ctx.DeferAsync(true);

            try
            {
                Tuple<ulong, ulong, ulong> resolved = msgJump.ResolveDiscordMessageJumpLink();

                var guild = await ctx.Client.GetGuildAsync(resolved.Item1);

                if (guild != ctx.Guild)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("The target message must be from this guild"));

                    return;
                }

                var chn = guild.GetChannel(resolved.Item2);
                var msg = await chn.GetMessageAsync(resolved.Item3);
                var noticeMsg = await channel.SendMessageAsync(msg.Content);

                var jumpToMessageBtn = new DiscordLinkButtonComponent(noticeMsg.JumpLink.ToString(), "Jump to message");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"I sent the notice to the target channel: #{chn.Name}")
                    .AddComponents(jumpToMessageBtn));
            }
            catch (Exception)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Invalid jump link provided"));
            }
        }

        [SlashCommand("override", "Override an existing notification sent by me")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task EditNoticeCommand(InteractionContext ctx,
            [Option("old_message_jump_link", "Old message jump link, must be a message in this guild")]
            string msgJumpOld,
            [Option("new_message_jump_link", "New message jump link, must be a message in this guild")]
            string msgJumpNew
        )
        {
            await ctx.DeferAsync(true);

            try
            {
                Tuple<ulong, ulong, ulong> resolved1 = msgJumpOld.ResolveDiscordMessageJumpLink();
                Tuple<ulong, ulong, ulong> resolved2 = msgJumpNew.ResolveDiscordMessageJumpLink();

                var g1 = await ctx.Client.GetGuildAsync(resolved1.Item1);
                var g2 = await ctx.Client.GetGuildAsync(resolved2.Item1);

                if (g1 != g2)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("These two message must originate from the same guild"));

                    return;
                }

                if (g1 != ctx.Guild)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("The old one must be from this guild"));

                    return;
                }

                if (g2 != ctx.Guild)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("The new one must be from this guild"));

                    return;
                }

                var c1 = g1.GetChannel(resolved1.Item2);
                var c2 = g2.GetChannel(resolved2.Item2);
                var oldMsg = await c1.GetMessageAsync(resolved1.Item3);
                var newMsg = await c2.GetMessageAsync(resolved2.Item3);

                await oldMsg.ModifyAsync(newMsg.Content);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Edited the notification"));
            }
            catch (Exception)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Invalid jump links provided"));
            }
        }

        [SlashCommand("copy", "Copy a notification sent by me (with formats)")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task CopyNoticeCommand(InteractionContext ctx,
            [Option("target_message_jump_link", "Target message jump link, must be a message in this guild")]
            string msgJump)
        {
            await ctx.DeferAsync(true);

            try
            {
                Tuple<ulong, ulong, ulong> resolved = msgJump.ResolveDiscordMessageJumpLink();

                var guild = await ctx.Client.GetGuildAsync(resolved.Item1);

                if (guild != ctx.Guild)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("The target message must be from this guild"));

                    return;
                }

                var chn = guild.GetChannel(resolved.Item2);
                var msg = await chn.GetMessageAsync(resolved.Item3);

                DiscordLinkButtonComponent jumpBtn = new DiscordLinkButtonComponent(msg.JumpLink.ToString(), "Source message");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .AddEmbed(ctx.Member.GetDefaultEmbedTemplateForUser()
                        .WithTitle("Notice copied")
                        .WithDescription(msg.Content))
                    .AddComponents(jumpBtn));
            }
            catch (Exception)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Invalid jump link provided"));
            }
        }
    }

    [SlashCommandGroup("message", "Commands for sending messages to moderator")]
    public class ModerationMessageModule : ApplicationCommandModule
    {
        [SlashCommand("appeal", "Send an appeal")]
        [SlashRequireDirectMessage]
        public async Task SendAppealAsync(InteractionContext ctx,
            [Option("guild_id", "The guild you want to appeal")] string guildId,
            [Option("receiver_id", "The receiver")] string receiverId)
        {
            await ctx.DeferAsync();

            var guildIdLong = Convert.ToUInt64(guildId);
            var receiverIdLong = Convert.ToUInt64(receiverId);

            try
            {
                var guild = await ctx.Client.GetGuildAsync(guildIdLong);
                var receiver = await guild.GetMemberAsync(receiverIdLong);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent(
                        "Please send your appeal in the following message (do not include attachments, save to your device if you want to)\n" +
                        "You have 5 minutes to send an appeal"));

                var res = await ctx.Client.GetInteractivity().WaitForMessageAsync(_ => true, TimeSpan.FromMinutes(5));

                if (res.TimedOut)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Timed out"));
                    
                    return;
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Sent the appeal, now it is time to be patient"));

                await receiver.SendMessageAsync(ctx.User.GetDefaultEmbedTemplateForUser()
                    .WithTitle("An appeal has been sent to you, content below")
                    .WithDescription(res.Result.Content)
                    .AddField("Sender", $"{ctx.User.Username}#{ctx.User.Discriminator} (ID: {ctx.User.Id})")
                    .AddField("Guild to appeal", $"{guild.Name} (ID: {guild.Id}"));
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Something wrong happened, either double check your input or wait for a while"));
            }
        }
    }
}
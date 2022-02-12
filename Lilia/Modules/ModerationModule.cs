using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Extensions;
using Lilia.Database.Models;
using Lilia.Services;

namespace Lilia.Modules;

[SlashCommandGroup("mod", "Moderation commands")]
public class ModerationModule : ApplicationCommandModule
{
    [SlashCommandGroup("general", "General command for moderating members")]
    public class ModerationGeneralModule : ApplicationCommandModule
    {
        private LiliaClient _client;
        private LiliaDatabaseContext _dbCtx;
        private const string MuteRoleName = "lilia-mute";

        public ModerationGeneralModule(LiliaClient client)
        {
            _client = client;
            _dbCtx = client.Database.GetContext();
        }

        [SlashCommand("ban", "Ban users in batch")]
        [SlashRequirePermissions(Permissions.BanMembers)]
        public async Task BanMembersCommand(InteractionContext ctx,
            [Option("reason", "Reason to ban")] string reason = "Rule violation")
        {
            await ctx.DeferAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"{Formatter.Bold("Mention")} all the people you want to ban"));

            var interactivity = ctx.Client.GetInteractivity();

            var res = await interactivity.WaitForMessageAsync(x => x.MentionedUsers.Any(), TimeSpan.FromMinutes(5));

            if (res.TimedOut)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("Timed out"));

                return;
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Banning mischievous people"));

            StringBuilder stringBuilder = new();

            foreach (var user in res.Result.MentionedUsers)
            {
                var member = (DiscordMember) user;

                if (member == ctx.Member)
                {
                    stringBuilder.AppendLine("Beaned you");
                }
                else
                {
                    await ctx.Guild.BanMemberAsync(member, 0, $"Banned by {ctx.Member.DisplayName}#{ctx.Member.Discriminator} - Reason: {reason}");
                    stringBuilder.AppendLine($"Banned {member.DisplayName}#{member.Discriminator}");
                }
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent(stringBuilder.ToString()));
        }

        [SlashCommand("kick", "Kick users in batch")]
        [SlashRequirePermissions(Permissions.KickMembers)]
        public async Task KickMembersCommand(InteractionContext ctx,
            [Option("reason", "Reason to kick")] string reason = "Rule violation")
        {
            await ctx.DeferAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"{Formatter.Bold("Mention")} all the people you want to kick"));

            var interactivity = ctx.Client.GetInteractivity();

            InteractivityResult<DiscordMessage> res = await interactivity.WaitForMessageAsync(x => x.MentionedUsers.Any(), TimeSpan.FromMinutes(5));

            if (res.TimedOut)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("Timed out"));

                return;
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Kicking mischievous people"));

            StringBuilder stringBuilder = new();

            foreach (var user in res.Result.MentionedUsers)
            {
                var member = (DiscordMember) user;

                if (member == ctx.Member)
                {
                    stringBuilder.AppendLine("Skipped you");
                }
                else
                {
                    await ctx.Guild.BanMemberAsync(member, 0, $"Kicked by {ctx.Member.DisplayName}#{ctx.Member.Discriminator} -> Reason: {reason}");
                    stringBuilder.AppendLine($"Kicked {member.DisplayName}#{member.Discriminator}");
                }
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent(stringBuilder.ToString()));
        }

        [SlashCommand("warnadd", "Add a warn to a user")]
        [SlashRequirePermissions(Permissions.KickMembers)]
        [SlashRequireBotPermissions(Permissions.ManageRoles)]
        public async Task AddWarnMemberCommand(InteractionContext ctx,
            [Option("user", "User to add warn, must be a user in your guild")]
            DiscordUser user,
            [Option("reason", "Reason to warn")]
            string reason = "Rule violation",
            [Choice("yes", 1)]
            [Choice("no", 0)]
            [Option("as_anon", "Decide whether to let others know you did that")]
            long asAnon = 1)
        {
            var isAnon = Convert.ToBoolean(asAnon);
            await ctx.DeferAsync(isAnon);
            
            var member = (DiscordMember) user;
            
            if (member == ctx.Member)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Imagine warning yourself"));
                
                return;
            }
            
            var dbUser = _dbCtx.GetOrCreateUserRecord(user);

            // mute role check
            var liliaMuteRole = ctx.Guild.Roles.ToList().Find(x => x.Value.Name == MuteRoleName).Value;

            if (liliaMuteRole == default)
            {
                liliaMuteRole = await ctx.Guild.CreateRoleAsync(MuteRoleName, reason: "Mute role creation", permissions: Permissions.None);
                foreach (var (_, channel) in ctx.Guild.Channels)
                {
                    await channel.AddOverwriteAsync(liliaMuteRole, deny: Permissions.SendMessages, reason: "Mute role channel addition");
                }
            }

            await member.RevokeRoleAsync(liliaMuteRole, "Warn removal");
            
            if (dbUser.WarnCount == 3)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"This user has been muted earlier, consider using {Formatter.InlineCode("/mod general warnremove")} to remove a warn"));

                return;
            }
            
            dbUser.WarnCount += 1;

            var isMuted = false;
            
            if (dbUser.WarnCount == 3)
            {
                await member.GrantRoleAsync(liliaMuteRole);
                isMuted = true;
            }
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Warned and informed the member about the warn{(isMuted ? " and muted the member" : string.Empty)}\n" +
                             $"Now they have {dbUser.WarnCount} warnings"));
            
            await member.SendMessageAsync($"You have been warned in guild {Formatter.Bold(ctx.Guild.Name)} by " +
                                          $"{(isAnon ? "a moderator" : $"{Formatter.Bold($"{ctx.Member.Username}#{ctx.Member.Discriminator}")}")} with reason: {Formatter.Bold(reason)}\n" +
                                          $"If you believe this is a mistake, you can try appealing using {Formatter.InlineCode("/mod message appeal")} {Formatter.Bold("in this DM thread")} " +
                                          $"with these values: {Formatter.InlineCode($"guild_id:{ctx.Guild.Id} receiver_id:{ctx.Member.Id}")}");

            await _dbCtx.SaveChangesAsync();
        }

        [SlashCommand("warnremove", "Remove a warn from a user")]
        [SlashRequirePermissions(Permissions.KickMembers)]
        public async Task RemoveWarnMemberCommand(InteractionContext ctx,
            [Option("user", "User to remove warn, must be a user in your guild")]
            DiscordUser user,
            [Option("reason", "Reason to remove warn")]
            string reason = "False warn",
            [Choice("yes", 1)]
            [Choice("no", 0)] 
            [Option("as_anon", "Decide whether to let others know you did that")]
            long asAnon = 1)
        {
            var isAnonBoolean = Convert.ToBoolean(asAnon);
            await ctx.DeferAsync(isAnonBoolean);
            
            var member = (DiscordMember) user;

            var dbUser = _dbCtx.GetOrCreateUserRecord(user);
            var isMuted = false;

            if (member == ctx.Member)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Imagine warning yourself"));
                
                return;
            }
            
            switch (dbUser.WarnCount)
            {
                case 0:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("This user does not have any warn"));

                    return;
                case 3:
                {
                    // mute role check
                    var liliaMuteRole = ctx.Guild.Roles.ToList().Find(x => x.Value.Name == MuteRoleName).Value;

                    if (liliaMuteRole == default)
                    {
                        liliaMuteRole = await ctx.Guild.CreateRoleAsync(MuteRoleName, reason: "Mute role creation", permissions: Permissions.None);
                        foreach (var (_, channel) in ctx.Guild.Channels)
                        {
                            await channel.AddOverwriteAsync(liliaMuteRole, deny: Permissions.SendMessages, reason: "Mute role channel addition");
                        }
                    }

                    await member.RevokeRoleAsync(liliaMuteRole, "Warn removal");
                    isMuted = true;
                    
                    break;
                }
            }

            dbUser.WarnCount -= 1;
            
            await member.SendMessageAsync($"You have been removed a warn in guild {Formatter.Bold(ctx.Guild.Name)} by " +
                                          $"{(isAnonBoolean ? "a moderator" : $"{Formatter.Bold($"{ctx.Member.Username}#{ctx.Member.Discriminator}")}")}" +
                                          $"with reason: {reason}");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Warn removed and informed the member about the warn removal{(isMuted ? " and removed the mute role" : string.Empty)}\n" +
                             $"Now the user has {dbUser.WarnCount} warning(s)"));
            
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

            var guild = await ctx.Client.GetGuildAsync(guildIdLong);
            var receiver = await guild.GetMemberAsync(receiverIdLong);

            var interactivity = ctx.Client.GetInteractivity();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Please send your appeal in the following message (do not include attachments, save to your device if you want to)\n" +
                             "You have 5 minutes to send an appeal"));

            InteractivityResult<DiscordMessage> res = await interactivity.WaitForMessageAsync(_ => true, TimeSpan.FromMinutes(5));

            if (res.TimedOut)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Timed out"));
            }
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Sent the appeal, now it is time to be patient"));

            await receiver.SendMessageAsync(ctx.User.GetDefaultEmbedTemplateForUser()
                .WithTitle("An appeal has been sent to you, content below")
                .WithDescription(res.Result.Content)
                .AddField("Sender", $"{ctx.User.Username}#{ctx.User.Discriminator}")
                .AddField("Guild to appeal", guild.Name)
                .WithFooter("Remember to respond when necessary"));
        }
    }
}
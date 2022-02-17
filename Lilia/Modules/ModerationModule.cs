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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lilia.Modules;

public enum ModerationAction
{
    [ChoiceName("ban")]
    Ban,

    [ChoiceName("kick")]
    Kick,

    [ChoiceName("warn_add")]
    WarnAdd,

    [ChoiceName("warn_remove")]
    WarnRemove
}

[SlashCommandGroup("mod", "Moderation commands")]
public class ModerationModule : ApplicationCommandModule
{
    [SlashCommandGroup("general", "General command for moderating members")]
    public class ModerationGeneralModule : ApplicationCommandModule
    {
        private LiliaClient _client;
        private LiliaDatabaseContext _dbCtx;
        private const string MuteRoleName = "Lilia-mute";

        public ModerationGeneralModule(LiliaClient client)
        {
            _client = client;
            _dbCtx = client.Database.GetContext();
        }

        [SlashCommand("execute", "Execute action on a batch of users")]
        public async Task ExecuteModActionOnUsersCommand(InteractionContext ctx,
            [Option("action", "The action to execute, defaults to kick")]
            ModerationAction action = ModerationAction.Kick,
            [Option("reason", "Reason to execute")]
            string reason = "Rule violation")
        {
            await ctx.DeferAsync();

            #region Precheck

            var requiresPerm = false;
            var requiredPerm = Permissions.None;

            switch (action)
            {
                case ModerationAction.Ban when !ctx.Member.Permissions.HasPermission(Permissions.BanMembers):
                    requiresPerm = true;
                    requiredPerm = Permissions.BanMembers;
                    break;

                case ModerationAction.Kick when !ctx.Member.Permissions.HasPermission(Permissions.KickMembers):
                    requiresPerm = true;
                    requiredPerm = Permissions.KickMembers;
                    break;

                case ModerationAction.WarnAdd or ModerationAction.WarnRemove
                    when !ctx.Member.Permissions.HasPermission(Permissions.ManageGuild):
                    requiresPerm = true;
                    requiredPerm = Permissions.ManageGuild;
                    break;
            }

            if (requiresPerm)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"You need {Formatter.Bold(requiredPerm.GetName())} permission in order to do this"));
            }

            #endregion Precheck

            reason = reason == "Rule violation"
                ? action == ModerationAction.WarnRemove ? "False warn" : "Rule violation"
                : reason;

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"{Formatter.Bold("Mention")} all the users you want to execute the action {Formatter.Bold(action.GetName())} with reason {Formatter.Bold(reason)}"));

            #region Get Members

            var interactivity = ctx.Client.GetInteractivity();
            var res = await interactivity.WaitForMessageAsync(x => x.MentionedUsers.Any(), TimeSpan.FromMinutes(5));
            if (res.TimedOut)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("Timed out"));

                return;
            }

            await res.Result.DeleteAsync();

            #endregion Get Members

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Executing..."));

            var stringBuilder = new StringBuilder();
            var reasonStr = $"Executed by {ctx.Member.DisplayName}#{ctx.Member.Discriminator} - Reason: {reason}";

            foreach (var discordUser in res.Result.MentionedUsers.Distinct())
            {
                var mentionedMember = await ctx.Guild.GetMemberAsync(discordUser.Id);
                var dbUser = _dbCtx.GetUserRecord(mentionedMember);

                var exec = $"Executing action {Formatter.Bold(action.GetName())} on {Formatter.Bold($"{mentionedMember.Username}#{mentionedMember.Discriminator}")}";
                stringBuilder.AppendLine(exec);

                if (mentionedMember == ctx.Member)
                {
                    stringBuilder.AppendLine("Skipped because it is you ---> Aborted");
                    continue;
                }

                if (mentionedMember == ctx.Client.CurrentUser)
                {
                    stringBuilder.AppendLine("Skipped because it is me ---> Aborted");
                    continue;
                }

                var now = DateTime.Now;
                var shouldDm = true;

                switch (action)
                {
                    case ModerationAction.Ban:
                        {
                            await mentionedMember.BanAsync(0, reasonStr);
                            break;
                        }
                    case ModerationAction.Kick:
                        {
                            await mentionedMember.RemoveAsync(reasonStr);
                            break;
                        }
                    case ModerationAction.WarnAdd:
                        {
                            if (dbUser.WarnCount == 3)
                            {
                                stringBuilder.AppendLine($"{Formatter.Mention(mentionedMember)} has been muted earlier --> Aborted");
                                shouldDm = false;
                                break;
                            }

                            dbUser.WarnCount += 1;

                            #region Mute role get or create

                            var liliaMuteRole = ctx.Guild.Roles.ToList().Find(x => x.Value.Name == MuteRoleName).Value;

                            if (liliaMuteRole == default)
                            {
                                liliaMuteRole = await ctx.Guild.CreateRoleAsync(MuteRoleName, reason: "Mute role creation",
                                    permissions: Permissions.None);
                                
                                foreach (var (_, channel) in ctx.Guild.Channels)
                                {
                                    await channel.AddOverwriteAsync(liliaMuteRole, deny: Permissions.SendMessages,
                                        reason: "Mute role channel addition");
                                }
                            }

                            #endregion Mute role get or create

                            if (dbUser.WarnCount == 3) await mentionedMember.GrantRoleAsync(liliaMuteRole);
                            stringBuilder.AppendLine($"Added a warn of {Formatter.Mention(mentionedMember)}. Now they have {dbUser.WarnCount} warn(s)");

                            break;
                        }
                    case ModerationAction.WarnRemove:
                        {
                            switch (dbUser.WarnCount)
                            {
                                case 0:
                                    stringBuilder.AppendLine("This user does not have any warn --> Aborted");
                                    shouldDm = false;
                                    break;

                                case 3:
                                    {
                                        #region Mute role get or create

                                        var liliaMuteRole = ctx.Guild.Roles.ToList().Find(x => x.Value.Name == MuteRoleName)
                                            .Value;

                                        if (liliaMuteRole == default)
                                        {
                                            liliaMuteRole = await ctx.Guild.CreateRoleAsync(MuteRoleName,
                                                reason: "Mute role creation", permissions: Permissions.None);
                                            foreach (var (_, channel) in ctx.Guild.Channels)
                                            {
                                                await channel.AddOverwriteAsync(liliaMuteRole, deny: Permissions.SendMessages,
                                                    reason: "Mute role channel addition");
                                            }
                                        }

                                        #endregion Mute role get or create

                                        await mentionedMember.RevokeRoleAsync(liliaMuteRole, "Warn removal");
                                        break;
                                    }
                                default:
                                    {
                                        dbUser.WarnCount -= 1;
                                        stringBuilder.AppendLine($"Removed a warn of {Formatter.Mention(mentionedMember)}. Now they have {dbUser.WarnCount} warn(s)");
                                        break;
                                    }
                            }

                            break;
                        }
                }

                if (shouldDm)
                {
                    var embedBuilder = new DiscordEmbedBuilder()
                        .WithAuthor(iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                        .WithTitle($"You were executed in guild \"{ctx.Guild.Name}\" (ID: {ctx.Guild.Id})")
                        .WithThumbnail(ctx.Guild.IconUrl)
                        .AddField("Action name", action.GetName(), true)
                        .AddField("Reason", reason, true)
                        .AddField("Moderator", $"{ctx.Member.DisplayName}#{ctx.Member.Discriminator} (ID: {ctx.Member.Id})",
                            true)
                        .AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
                        .AddField("What to do now?",
                            action != ModerationAction.WarnRemove
                                ? $"If you believe this was a mistake, you can try sending an appeal using {Formatter.InlineCode("mod message appeal")} with provided IDs"
                                : "Nothing");

                    await mentionedMember.SendMessageAsync(embedBuilder);
                }

                _dbCtx.Update(dbUser);
                await _dbCtx.SaveChangesAsync();
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent(stringBuilder.ToString()));
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
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

        public ModerationGeneralModule(LiliaClient client)
        {
            _client = client;
            _dbCtx = client.Database.GetContext();
        }

        [SlashCommand("ban", "Ban members in batch")]
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

        [SlashCommand("kick", "Kick members in batch")]
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
    }

    [SlashCommandGroup("notice", "Commands for sending global notices")]
    public class ModerationNoticeModule : ApplicationCommandModule
    {
        [SlashCommand("send", "Send a notification to a channel")]
        [SlashRequirePermissions(Permissions.ManageGuild)]
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
        [SlashRequirePermissions(Permissions.ManageGuild)]
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
        [SlashRequirePermissions(Permissions.ManageGuild)]
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
                    .AddEmbed(ctx.Member.GetDefaultEmbedTemplateForMember()
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
}
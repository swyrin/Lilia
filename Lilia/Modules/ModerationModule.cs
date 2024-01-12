using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Fergun.Interactive.Pagination;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Interactors;
using Lilia.Modules.Utils;
using Lilia.Services;

namespace Lilia.Modules
{
    [Group("mod", "Moderation commands")]
    public class ModerationModule : InteractionModuleBase<ShardedInteractionContext>
    {
        [Group("general", "General command for moderating members")]
        public class ModerationGeneralModule : InteractionModuleBase<ShardedInteractionContext>
        {
            private const string MuteRoleName = "Lilia-mute";
            private readonly LiliaClient _client;
            private readonly LiliaDatabaseContext _dbContext;

            public ModerationGeneralModule(LiliaClient client, LiliaDatabase database)
            {
                _dbContext = database.GetContext();
                _client = client;
            }

            [SlashCommand("ban", "Ban members in batch")]
            [RequireUserPermission(GuildPermission.BanMembers)]
            [RequireBotPermission(GuildPermission.BanMembers)]
            public async Task ModerationGeneralBanCommand(
                [Summary("reason", "Reason to execute")]
                string reason = "Rule violation")
            {
                await Context.Interaction.DeferAsync();

                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                    x.Content = $"{Format.Bold("Mention")} all the users you want to ban with reason {Format.Bold(reason)}");

                var mentionedUsers = await ModerationModuleUtils.GetMentionedUsersAsync(Context, _client.InteractiveService);

                StringBuilder stringBuilder = new();

                foreach (var discordUser in mentionedUsers)
                {
                    var mentionedMember = Context.Guild.GetUser(discordUser.Id);

                    if (mentionedMember == Context.User || mentionedMember.Id == Context.Client.CurrentUser.Id)
                    {
                        stringBuilder.AppendLine("Skipped because it is either you or me");
                        continue;
                    }

                    var execLine = $"Banning {Format.Bold($"{mentionedMember}")}";
                    stringBuilder.AppendLine(execLine);

                    try
                    {
                        await mentionedMember.BanAsync(0, reason);
                    }
                    catch
                    {
                        stringBuilder.AppendLine($"Missing permission to ban {Format.Bold($"{mentionedMember}")}");
                    }

                    var now = DateTime.Now;

                    var embedBuilder = new EmbedBuilder()
                        .WithAuthor(null, Context.Client.CurrentUser.GetAvatarUrl())
                        .WithTitle($"You have been banned from guild \"{Context.Guild.Name}\" (ID: {Context.Guild.Id})")
                        .WithThumbnailUrl(Context.Guild.IconUrl)
                        .AddField("Reason", reason, true)
                        .AddField("Moderator", $"{Context.Interaction.User} (ID: {Context.User.Id})", true)
                        .AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
                        .AddField("What to do now?",
                            $"If you believe this was a mistake, you can try sending an appeal using {Format.Code("/mod ticket appeal")} with provided IDs");

                    if (!mentionedMember.IsBot)
                        await mentionedMember.SendMessageAsync(embed: embedBuilder.Build());
                }

                var pages = LiliaUtilities.CreatePagesFromString($"{stringBuilder}");

                var paginator = new StaticPaginatorBuilder()
                    .AddUser(Context.User)
                    .WithPages(pages)
                    .Build();

                await _client.InteractiveService.SendPaginatorAsync(paginator, Context.Interaction,
                    responseType: InteractionResponseType.DeferredChannelMessageWithSource);
            }

            [SlashCommand("kick", "Kick members in batch")]
            [RequireUserPermission(GuildPermission.KickMembers)]
            [RequireBotPermission(GuildPermission.KickMembers)]
            public async Task ModerationGeneralKickCommand(
                [Summary("reason", "Reason to execute")]
                string reason = "Rule violation")
            {
                await Context.Interaction.DeferAsync();

                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                    x.Content = $"{Format.Bold("Mention")} all the users you want to kick with reason {Format.Bold(reason)}");

                var mentionedUsers = await ModerationModuleUtils.GetMentionedUsersAsync(Context, _client.InteractiveService);

                StringBuilder stringBuilder = new();

                foreach (var discordUser in mentionedUsers)
                {
                    var mentionedMember = Context.Guild.GetUser(discordUser.Id);

                    if (mentionedMember == Context.User || mentionedMember.Id == Context.Client.CurrentUser.Id)
                    {
                        stringBuilder.AppendLine("Skipped because it is either you or me");
                        continue;
                    }

                    var execLine = $"Kicking {Format.Bold($"{mentionedMember}")}";
                    stringBuilder.AppendLine(execLine);

                    try
                    {
                        await mentionedMember.KickAsync(reason);
                    }
                    catch
                    {
                        stringBuilder.AppendLine($"Missing permission to kick {Format.Bold($"{mentionedMember}")}");
                    }

                    var now = DateTime.Now;

                    var embedBuilder = new EmbedBuilder()
                        .WithAuthor(null, Context.Client.CurrentUser.GetAvatarUrl())
                        .WithTitle($"You have been kicked from guild \"{Context.Guild.Name}\" (ID: {Context.Guild.Id})")
                        .WithThumbnailUrl(Context.Guild.IconUrl)
                        .AddField("Reason", reason, true)
                        .AddField("Moderator", $"{Context.User} (ID: {Context.User.Id})", true)
                        .AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
                        .AddField("What to do now?",
                            $"If you believe this was a mistake, you can try sending an appeal using {Format.Code("/mod ticket appeal")} with provided IDs");

                    if (!mentionedMember.IsBot)
                        await mentionedMember.SendMessageAsync(embed: embedBuilder.Build());
                }

                var pages = LiliaUtilities.CreatePagesFromString($"{stringBuilder}");

                var paginator = new StaticPaginatorBuilder()
                    .AddUser(Context.User)
                    .WithPages(pages)
                    .Build();

                await _client.InteractiveService.SendPaginatorAsync(paginator, Context.Interaction,
                    responseType: InteractionResponseType.DeferredChannelMessageWithSource);
            }

            [SlashCommand("warn_add", "Add a warn to an user")]
            [RequireUserPermission(GuildPermission.ModerateMembers)]
            public async Task ModerationGeneralWarnAddCommand(
                [Summary("user", "The user to add a warn")]
                SocketGuildUser user,
                [Summary("reason", "Reason to execute")]
                string reason = "Rule violation")
            {
                await Context.Interaction.DeferAsync();

                if (user == Context.User || user.Id == Context.Client.CurrentUser.Id)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                        x.Content = "Skipped because it is either you or me");

                    return;
                }

                var dbUser = _dbContext.GetUserRecord(user);

                if (dbUser.WarnCount == 3)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                        x.Content = $"{user.Mention} has been warned and muted earlier");

                    return;
                }

                dbUser.WarnCount += 1;

                var (isExistedInPast, discordRole) =
                    await ModerationModuleUtils.GetOrCreateRoleAsync(Context, MuteRoleName, GuildPermissions.None, Color.Default);

                if (!isExistedInPast)
                    foreach (var channel in Context.Guild.Channels)
                        await channel.AddPermissionOverwriteAsync(discordRole,
                            new OverwritePermissions(sendMessages: PermValue.Deny, sendMessagesInThreads: PermValue.Deny));

                if (dbUser.WarnCount == 3) await user.AddRoleAsync(discordRole);

                var now = DateTime.Now;

                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                    x.Content = $"Added a warn to {user.Mention}\nNow they have {dbUser.WarnCount} warn(s)");

                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(null, Context.Client.CurrentUser.GetAvatarUrl())
                    .WithTitle($"You were warned in guild \"{Context.Guild.Name}\" (ID: {Context.Guild.Id})")
                    .WithThumbnailUrl(Context.Guild.IconUrl)
                    .AddField("Reason", reason, true)
                    .AddField("Moderator", $"{user} (ID: {Context.User.Id})", true)
                    .AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
                    .AddField("What to do now?",
                        $"If you believe this was a mistake, you can try sending an appeal using {Format.Code("/mod ticket appeal")} with provided IDs");

                if (!user.IsBot)
                    await user.SendMessageAsync(embed: embedBuilder.Build());

                await _dbContext.SaveChangesAsync();
            }

            [SlashCommand("warn_remove", "Remove a warn from an user")]
            [RequireUserPermission(GuildPermission.ModerateMembers)]
            public async Task ModerationGeneralWarnRemoveCommand(
                [Summary("user", "The user to remove a warn")]
                SocketGuildUser user,
                [Summary("reason", "Reason to execute")]
                string reason = "Good behavior")
            {
                await Context.Interaction.DeferAsync();

                if (user == Context.User || user.Id == Context.Client.CurrentUser.Id)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                        x.Content = "Skipped because it is either you or me");

                    return;
                }

                var dbUser = _dbContext.GetUserRecord(user);

                switch (dbUser.WarnCount)
                {
                    case 3:
                    {
                        var (isExistedInPast, discordRole) =
                            await ModerationModuleUtils.GetOrCreateRoleAsync(Context, MuteRoleName, GuildPermissions.None, Color.Default);

                        if (!isExistedInPast)
                            foreach (var channel in Context.Guild.Channels)
                                await channel.AddPermissionOverwriteAsync(discordRole,
                                    new OverwritePermissions(sendMessages: PermValue.Deny, sendMessagesInThreads: PermValue.Deny));

                        await user.RemoveRoleAsync(discordRole);
                        break;
                    }
                    case 0:
                        await Context.Interaction.ModifyOriginalResponseAsync(x =>
                            x.Content = "No warn to remove");

                        return;
                }

                dbUser.WarnCount -= 1;

                var now = DateTime.Now;

                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                    x.Content = $"Removed a warn of {user.Mention}\nNow they have {dbUser.WarnCount} warn(s)");

                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(null, Context.Client.CurrentUser.GetAvatarUrl())
                    .WithTitle($"You have been removed a warn in guild \"{Context.Guild.Name}\" (ID: {Context.Guild.Id})")
                    .WithThumbnailUrl(Context.Guild.IconUrl)
                    .AddField("Reason", reason, true)
                    .AddField("Moderator", $"{user} (ID: {Context.User.Id})", true)
                    .AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
                    .AddField("What to do now?", "Nothing");

                if (!user.IsBot)
                    await user.SendMessageAsync(embed: embedBuilder.Build());

                await _dbContext.SaveChangesAsync();
            }

            [SlashCommand("mute", "Mute an user, like Timeout but infinite duration")]
            [RequireUserPermission(GuildPermission.ModerateMembers)]
            public async Task ModerationGeneralMuteCommand(
                [Summary("user", "The user to mute")] SocketGuildUser user,
                [Summary("reason", "The reason")] string reason = "Rule violation")
            {
                await Context.Interaction.DeferAsync();

                if (user == Context.User || user.Id == Context.Client.CurrentUser.Id)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                        x.Content = "Skipped because it is either you or me");

                    return;
                }

                var (isExistedInPast, discordRole) =
                    await ModerationModuleUtils.GetOrCreateRoleAsync(Context, MuteRoleName, GuildPermissions.None, Color.Default);

                if (!isExistedInPast)
                    foreach (var channel in Context.Guild.Channels)
                        await channel.AddPermissionOverwriteAsync(discordRole,
                            new OverwritePermissions(sendMessages: PermValue.Deny, sendMessagesInThreads: PermValue.Deny));

                // no equivalent like Has?
                if (user.Roles.Count(x => x == discordRole) == 1)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                        x.Content = "Already muted");

                    return;
                }

                await user.AddRoleAsync(discordRole);

                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                    x.Content = $"Muted {user.Mention} because of reason: {Format.Bold(reason)}");

                var now = DateTime.Now;
                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(null, Context.Client.CurrentUser.GetAvatarUrl())
                    .WithTitle($"You have been muted in guild \"{Context.Guild.Name}\" (ID: {Context.Guild.Id})")
                    .WithThumbnailUrl(Context.Guild.IconUrl)
                    .AddField("Reason", reason, true)
                    .AddField("Moderator", $"{user} (ID: {Context.User.Id})", true)
                    .AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
                    .AddField("What to do now?",
                        $"If you believe this was a mistake, you can try sending an appeal using {Format.Code("/mod ticket appeal")} with provided IDs");

                if (!user.IsBot)
                    await user.SendMessageAsync(embed: embedBuilder.Build());
            }

            [SlashCommand("unmute", "Unmute an user, like Remove Timeout")]
            [RequireUserPermission(GuildPermission.ModerateMembers)]
            public async Task ModerationGeneralUnmuteCommand(
                [Summary("user", "The user to mute")] SocketGuildUser user,
                [Summary("reason", "The reason")] string reason = "Good behavior")
            {
                await Context.Interaction.DeferAsync();

                if (user == Context.User || user.Id == Context.Client.CurrentUser.Id)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                        x.Content = "Skipped because it is either you or me");

                    return;
                }

                var (isExistedInPast, discordRole) =
                    await ModerationModuleUtils.GetOrCreateRoleAsync(Context, MuteRoleName, GuildPermissions.None, Color.Default);

                if (!isExistedInPast)
                    foreach (var channel in Context.Guild.Channels)
                        await channel.AddPermissionOverwriteAsync(discordRole,
                            new OverwritePermissions(sendMessages: PermValue.Deny, sendMessagesInThreads: PermValue.Deny));

                // no equivalent like Has?
                if (user.Roles.Any(x => x == discordRole))
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                        x.Content = "Already unmuted");

                    return;
                }

                await user.RemoveRoleAsync(discordRole);

                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                    x.Content = $"Unmuted {user.Mention} because of reason: {Format.Bold(reason)}");

                var now = DateTime.Now;
                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(null, Context.Client.CurrentUser.GetAvatarUrl())
                    .WithTitle($"You have been unmuted in guild \"{Context.Guild.Name}\" (ID: {Context.Guild.Id})")
                    .WithThumbnailUrl(Context.Guild.IconUrl)
                    .AddField("Reason", reason, true)
                    .AddField("Moderator", $"{Context.User} (ID: {Context.User.Id})",
                        true)
                    .AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
                    .AddField("What to do now?", "Nothing");

                if (!user.IsBot)
                    await user.SendMessageAsync(embed: embedBuilder.Build());
            }
        }

        [Group("mail", "You know modmail right?")]
        public class ModerationMailModule : InteractionModuleBase<ShardedInteractionContext>
        {
            private readonly LiliaClient _client;

            public ModerationMailModule(LiliaClient client)
            {
                _client = client;
            }

            [SlashCommand("solve", "Mark a mail as solved")]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task ModerationMailSolveCommand(
                [Summary("messageId", "Embed message ID")]
                string messageId,
                [Summary("reason", "Solution's reason")]
                string reason) =>
                await GenericMailStuffs(messageId, "Solved", reason);

            [SlashCommand("reject", "Mark a mail as rejected")]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task ModerationMailRejectCommand(
                [Summary("messageId", "Embed message ID")]
                string messageId,
                [Summary("reason", "Rejection's reason")]
                string reason) =>
                await GenericMailStuffs(messageId, "Rejected", reason);

            [SlashCommand("reopen", "Reopen a mail")]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task ModerationMailReopenCommand(
                [Summary("messageId", "Embed message ID")]
                string messageId,
                [Summary("reason", "Opening's reason")]
                string reason) =>
                await GenericMailStuffs(messageId, "Reopened", reason);

            [SlashCommand("reply", "Reply to the sender of the mail")]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task ModerationMailReplyCommand(
                [Summary("messageId", "Embed message ID")]
                string messageId)
            {
                await Context.Interaction.DeferAsync();

                var canConvert = ulong.TryParse(messageId, out var id);

                if (!canConvert)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                    {
                        x.Content = "Invalid message ID\n" +
                                    "1 - The message must be in **this** channel\n" +
                                    "2 - In case you don't know how to get it: https://discordnet.dev/faq/basics/getting-started.html?tabs=dev-mode#what-is-a-clientuserobject-id";
                    });

                    return;
                }

                var msg = (RestUserMessage)await Context.Channel.GetMessageAsync(id);

                if (msg == null)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                    {
                        x.Content = "Invalid message ID\n" +
                                    "The message must be in **this** channel\n" +
                                    "In case you don't know how to get it: https://discordnet.dev/faq/basics/getting-started.html?tabs=dev-mode#what-is-a-clientuserobject-id";
                    });

                    return;
                }

                if (msg.Author.Id != Context.Client.CurrentUser.Id)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = "I was not the one to send the target message"; });
                    return;
                }

                var interactive = _client.InteractiveService;

                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                    x.Content = "Type your reply in the next message (use image as link, if any)");

                var result = await interactive.NextMessageAsync(x => x.Channel.Id == Context.Channel.Id && x.Author == Context.User);

                if (result.IsTimeout)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                        x.Content = "Timed out");

                    return;
                }

                var replyMsg = result.Value!;
                var userId = msg.Embeds.First().Fields.First(x => x.Name == "ID").Value;

                var member = Context.Guild.GetUser(Convert.ToUInt64(userId));

                if (member == null)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                        x.Content = "User does not exist in this guild");

                    return;
                }

                try
                {
                    await member.SendMessageAsync(replyMsg.Content);

                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                        x.Content = "Sent the reply to this user");
                }
                catch
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                        x.Content = "Unable to send DM to this user");
                }
            }

            private async Task GenericMailStuffs(string messageId, string title, string reason)
            {
                await Context.Interaction.DeferAsync(true);

                var canConvert = ulong.TryParse(messageId, out var id);

                if (!canConvert)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                    {
                        x.Content = "Invalid message ID\n" +
                                    "1 - The message must be in **this** channel\n" +
                                    "2 - In case you don't know how to get it: https://discordnet.dev/faq/basics/getting-started.html?tabs=dev-mode#what-is-a-clientuserobject-id";
                    });

                    return;
                }

                var msg = (RestUserMessage)await Context.Channel.GetMessageAsync(id);

                if (msg == null)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                    {
                        x.Content = "Invalid message ID\n" +
                                    "The message must be in **this** channel\n" +
                                    "In case you don't know how to get it: https://discordnet.dev/faq/basics/getting-started.html?tabs=dev-mode#what-is-a-clientuserobject-id";
                    });

                    return;
                }

                if (msg.Author.Id != Context.Client.CurrentUser.Id)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = "I was not the one to send the target message"; });
                    return;
                }

                if (msg.Embeds.First().Title == title)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = $"Already {title.ToLower()}"; });
                    return;
                }

                switch (msg.Embeds.First().Title)
                {
                    case "Solved" when title == "Rejected":
                        await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = "You can not reject a solved one"; });
                        return;
                    case "Rejected" when title == "Solved":
                        await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = "You can not solve a rejected one"; });
                        return;
                    case "Reopened" when title == "A mail has been sent to you":
                        await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = "You can not reopen a pending one"; });
                        return;
                    case "Reopened" when title == "Reopened":
                        await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = "You can not reopen a reopened one"; });
                        return;
                }

                var color = title switch
                {
                    "Solved" => Color.Green,
                    "Rejected" => Color.Red,
                    _ => Color.DarkGrey
                };

                await msg.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder()
                        .WithTitle(title)
                        .WithDescription(msg.Content)
                        .WithColor(color)
                        .AddField("Sender", msg.Embeds.First().Fields.First(field => field.Name == "Sender").Value, true)
                        .AddField("At", msg.Embeds.First().Fields.First(field => field.Name == "At").Value, true)
                        .AddField("Reason", reason)
                        .AddField("Moderator", $"{Context.User}", true)
                        .AddField("Execution time", DateTime.Now.ToLongDateTime(), true)
                        .Build();
                });

                await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = $"Done modifying. Jump link: {msg.GetJumpUrl()}"; });
            }
        }
    }
}

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Lilia.Database;
using Lilia.Services;

namespace Lilia.Modules;

public class ModerationModule : ApplicationCommandModule
{
    private LiliaClient _client;
    private LiliaDbContext _dbCtx;

    public ModerationModule(LiliaClient client)
    {
        this._client = client;
        this._dbCtx = client.Database.GetContext();
    }

    [SlashCommand("ban", "Ban members, obviously")]
    [SlashRequirePermissions(Permissions.BanMembers)]
    public async Task BanMembersCommand(InteractionContext ctx,
        [Option("reason", "Reason to ban")] string reason = "Rule violation")
    {
        await ctx.DeferAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"{Formatter.Bold("Mention")} all the people you want to ban"));

        InteractivityExtension interactivity = ctx.Client.GetInteractivity();

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
        
        foreach (DiscordUser user in res.Result.MentionedUsers)
        {
            DiscordMember member = (DiscordMember) user;
            
            if (member == ctx.Member)
            {
                stringBuilder.AppendLine("Beaned you");
            }
            else
            {
                await ctx.Guild.BanMemberAsync(member, 0, $"Banned by {ctx.Member.DisplayName}#{ctx.Member.Discriminator} -> Reason: {reason}");
                stringBuilder.AppendLine($"Banned {member.DisplayName}#{member.Discriminator}");
            }
        }

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
            .WithContent(stringBuilder.ToString()));
    }

    [SlashCommand("kick", "Kick members, obviously")]
    [SlashRequirePermissions(Permissions.KickMembers)]
    public async Task KickMembersCommand(InteractionContext ctx,
        [Option("reason", "Reason to kick")] string reason = "Rule violation")
    {
        await ctx.DeferAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"{Formatter.Bold("Mention")} all the people you want to kick"));

        InteractivityExtension interactivity = ctx.Client.GetInteractivity();

        var res = await interactivity.WaitForMessageAsync(x => x.MentionedUsers.Any(), TimeSpan.FromMinutes(5));

        if (res.TimedOut)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Timed out"));

            return;
        }
        
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
            .WithContent("Kicking mischievous people"));

        StringBuilder stringBuilder = new();
        
        foreach (DiscordUser user in res.Result.MentionedUsers)
        {
            DiscordMember member = (DiscordMember) user;
            
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

    [SlashCommand("notice", "Send a notification to a channel")]
    [SlashRequirePermissions(Permissions.ManageGuild)]
    public async Task SendNoticeCommand(InteractionContext ctx,
        [Option("message_id", "Message ID to copy, same channel as command")] string msgId,
        [ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Store, ChannelType.NewsThread, ChannelType.PublicThread)]
        [Option("channel", "Channel to send")] DiscordChannel channel)
    {
        await ctx.DeferAsync();
        
        try
        {
            DiscordMessage msg = await ctx.Channel.GetMessageAsync(Convert.ToUInt64(msgId));
            await channel.SendMessageAsync(msg.Content);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Sent to the destination channel"));
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case NotFoundException:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"I can not find a message with {msgId} in this channel"));    
                    break;
                case FormatException:
                case OverflowException:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"Invalid ID provided: {Formatter.InlineCode(msgId)}"));
                    break;
                default:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Unknown error found"));
                    break;
            }
        }
    }
    
    [SlashCommand("editnotice", "Edit an existing notification sent by me")]
    [SlashRequirePermissions(Permissions.ManageGuild)]
    public async Task EditNoticeCommand(InteractionContext ctx,
        [ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Store, ChannelType.NewsThread, ChannelType.PublicThread)]
        [Option("channel", "Where the old notification is sent")] DiscordChannel channel,
        [Option("old_message_id", "Old message ID")] string msgIdOld,
        [Option("new_message_id", "New message ID, same channel as command")] string msgIdNew
        )
    {
        await ctx.DeferAsync();
        
        try
        {
            DiscordMessage oldMsg = await channel.GetMessageAsync(Convert.ToUInt64(msgIdOld));
            DiscordMessage newMsg = await ctx.Channel.GetMessageAsync(Convert.ToUInt64(msgIdNew));

            await oldMsg.ModifyAsync(newMsg.Content);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Edited the notification"));
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case NotFoundException:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"I can not find a message with ID {msgIdNew} in this channel or {msgIdOld} in provided channel"));    
                    break;
                case FormatException:
                case OverflowException:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"Invalid ID(s) provided: {Formatter.InlineCode(msgIdNew)}, {Formatter.InlineCode(msgIdOld)}"));
                    break;
                case UnauthorizedException:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("I was not the one to write the PSA"));
                    break;
                default:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Unknown error found"));
                    break;
            }
        }
    }
    
    [SlashCommand("copynotice", "Copy a notification sent by me (with formats)")]
    [SlashRequirePermissions(Permissions.ManageGuild)]
    public async Task CopyNoticeCommand(InteractionContext ctx,
        [ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Store, ChannelType.NewsThread, ChannelType.PublicThread)]
        [Option("channel", "Where the notification is sent")] DiscordChannel channel,
        [Option("message_id", "Message ID to copy")] string msgId)
    {
        await ctx.DeferAsync();
        
        try
        {
            ulong msgIdU = Convert.ToUInt64(msgId);
            DiscordMessage msg = await channel.GetMessageAsync(msgIdU);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(Formatter.Sanitize(msg.Content)));
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case NotFoundException:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"I can not find a message with {Formatter.InlineCode(msgId)} in provided channel"));
                    break;
                case FormatException:
                case OverflowException:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"Invalid ID provided: {Formatter.InlineCode(msgId)}"));
                    break;
                case UnauthorizedException:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("I was not the one to write the notification"));
                    break;
                default:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Unknown error found"));
                    break;
            }
        }
    }
}
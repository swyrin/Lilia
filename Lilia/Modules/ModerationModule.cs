using System;
using System.Collections;
using System.Collections.Generic;
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

    [SlashCommand("sendpsa", "Send PSA to a channel")]
    [SlashRequirePermissions(Permissions.ManageGuild)]
    public async Task SendPsaCommand(InteractionContext ctx,
        [Option("message_id", "Message ID to copy, same channel as command")] string msgId,
        [ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Store, ChannelType.NewsThread, ChannelType.PublicThread)]
        [Option("channel", "Channel to send")] DiscordChannel channel)
    {
        try
        {
            await ctx.DeferAsync();

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
                        .WithContent("Invalid ID(s) provided"));
                    break;
                default:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Unknown error found"));
                    break;
            }
        }
    }
    
    [SlashCommand("editpsa", "Edit an existing PSA")]
    [SlashRequirePermissions(Permissions.ManageGuild)]
    public async Task EditPsaCommand(InteractionContext ctx,
        [ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Store, ChannelType.NewsThread, ChannelType.PublicThread)]
        [Option("channel", "Where the old PSA is is sent")] DiscordChannel channel,
        [Option("old_message_id", "Old message ID")] string msgIdOld,
        [Option("new_message_id", "New message ID, same channel as command")] string msgIdNew
        )
    {
        try
        {
            await ctx.DeferAsync();

            DiscordMessage oldMsg = await channel.GetMessageAsync(Convert.ToUInt64(msgIdOld));
            DiscordMessage newMsg = await ctx.Channel.GetMessageAsync(Convert.ToUInt64(msgIdNew));

            await oldMsg.ModifyAsync(newMsg.Content);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Edited the PSA"));
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
                        .WithContent("Invalid ID(s) provided"));
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
    
    [SlashCommand("copypsa", "Copy a PSA sent by me (with formats)")]
    [SlashRequirePermissions(Permissions.ManageGuild)]
    public async Task CopyPsaCommand(InteractionContext ctx,
        [ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Store, ChannelType.NewsThread, ChannelType.PublicThread)]
        [Option("channel", "Where the PSA is sent")] DiscordChannel channel,
        [Option("message_id", "Message ID to copy")] string msgId)
    {
        try
        {
            await ctx.DeferAsync();
            
            ulong msgIdU = Convert.ToUInt64(msgId); 
            
            DiscordMessage msg = await channel.GetMessageAsync(msgIdU);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(msg.Content));
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case NotFoundException:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"I can not find a message with {msgId} in provided channel"));
                    break;
                case FormatException:
                case OverflowException:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Invalid ID(s) provided"));
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
}
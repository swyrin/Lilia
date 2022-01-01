using System;
using System.Linq;
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
        [Option("reason", "Reason to ban")] string reason = "")
    {
        await ctx.DeferAsync();

        DiscordMessage msg = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"{Formatter.Bold("Mention")} all the people you want to ban"));

        InteractivityExtension interactivity = ctx.Client.GetInteractivity();

        var res = await interactivity.WaitForMessageAsync(x => x.MentionedUsers.Any());

        if (res.TimedOut)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Time exceeded"));

            return;
        }
        
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
            .WithContent("Banning mischievous people"));

        DiscordFollowupMessageBuilder builder = new DiscordFollowupMessageBuilder();
        
        foreach (DiscordUser user in res.Result.MentionedUsers)
        {
            DiscordMember member = (DiscordMember) user;
            
            if (member == ctx.Member)
            {
                builder.WithContent("Beaned you");
            }
            else
            {
                reason = string.IsNullOrWhiteSpace(reason)
                    ? $"Banned by {ctx.Member.DisplayName}#{ctx.Member.Discriminator}"
                    : reason;
                
                await ctx.Guild.BanMemberAsync(member, 0, reason);
                builder.WithContent($"Banned {member.DisplayName}#{member.Discriminator}");
            }
        }

        await ctx.FollowUpAsync(builder);
    }

    [SlashCommand("kick", "Kick members, obviously")]
    [SlashRequirePermissions(Permissions.KickMembers)]
    public async Task KickMembersCommand(InteractionContext ctx,
        [Option("reason", "Reason to kick")] string reason = "")
    {
        await ctx.DeferAsync();

        DiscordMessage msg = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"{Formatter.Bold("Mention")} all the people you want to kick"));

        InteractivityExtension interactivity = ctx.Client.GetInteractivity();

        var res = await interactivity.WaitForMessageAsync(x => x.MentionedUsers.Any());

        if (res.TimedOut)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Time exceeded"));

            return;
        }
        
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
            .WithContent("Kicking mischievous people"));

        DiscordFollowupMessageBuilder builder = new DiscordFollowupMessageBuilder();
        
        foreach (DiscordUser user in res.Result.MentionedUsers)
        {
            DiscordMember member = (DiscordMember) user;
            
            if (member == ctx.Member)
            {
                builder.WithContent("Imagine kicking yourself, smh");
            }
            else
            {
                reason = string.IsNullOrWhiteSpace(reason)
                    ? $"Kicked by {ctx.Member.DisplayName}#{ctx.Member.Discriminator}"
                    : reason;

                await member.RemoveAsync(reason);
                builder.WithContent($"Kicked {member.DisplayName}#{member.Discriminator}");
            }
        }

        await ctx.FollowUpAsync(builder);
    }

    [SlashCommand("sendpsa", "Send PSA to a channel without the member knowing the sender")]
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
        catch (NotFoundException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Message with specified ID was not found in this channel"));
        }
        catch (FormatException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Invalid message ID"));
        }
    }
    
    [SlashCommand("editpsa", "Edit an existing PSA")]
    [SlashRequirePermissions(Permissions.ManageGuild)]
    public async Task EditPsaCommand(InteractionContext ctx,
        [Option("old_message_id", "Old message ID")] string msgIdOld,
        [Option("new_message_id", "New message ID, same channel as command")] string msgIdNew,
        [ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Store, ChannelType.NewsThread, ChannelType.PublicThread)]
        [Option("channel", "Previously sent PSA channel")] DiscordChannel channel)
    {
        try
        {
            await ctx.DeferAsync();
        
            DiscordMessage oldMsg = await channel.GetMessageAsync(Convert.ToUInt64(msgIdOld));

            if (oldMsg.Author != ctx.Client.CurrentUser)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I was not the one to write the PSA"));
            
                return;
            }
        
            DiscordMessage newMsg = await ctx.Channel.GetMessageAsync(Convert.ToUInt64(msgIdNew));

            await oldMsg.ModifyAsync(newMsg.Content);
        
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Edited the message"));    
        }
        catch (NotFoundException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Either the old message was not found in the provided channel or the new message was not found in this channel"));
        }
        catch (FormatException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Invalid message ID(s)"));
        }
    }
}
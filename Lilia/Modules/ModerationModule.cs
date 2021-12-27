using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
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
    [SlashRequirePermissions(Permissions.BanMembers)]
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
            .WithContent("Kicked mischievous people"));

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
}
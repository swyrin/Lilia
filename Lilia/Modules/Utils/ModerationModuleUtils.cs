using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

namespace Lilia.Modules.Utils;

public static class ModerationModuleUtils
{
    public static async Task<IEnumerable<DiscordUser>> GetMentionedUsersAsync(InteractionContext ctx,
        bool deleteResponse = true)
    {
        var interactivity = ctx.Client.GetInteractivity();
        var res = await interactivity.WaitForMessageAsync(x => x.MentionedUsers.Any(), TimeSpan.FromMinutes(5));

        if (res.TimedOut)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Timed out"));

            return new List<DiscordUser>();
        }

        if (deleteResponse) await res.Result.DeleteAsync();

        return res.Result.MentionedUsers.Distinct();
    }

    public static async Task<(bool, DiscordRole)> GetOrCreateRoleAsync(InteractionContext ctx, string roleName, string createReason, Permissions perms)
    {
        var testRole = ctx.Guild.Roles.ToList().Find(x => x.Value.Name == roleName).Value;
        var isExistedFromTheBeginning = true;

        if (testRole == default)
        {
            isExistedFromTheBeginning = false;
            testRole = await ctx.Guild.CreateRoleAsync(roleName, reason: createReason, permissions: perms);
        }

        return (isExistedFromTheBeginning, testRole);
    }
}
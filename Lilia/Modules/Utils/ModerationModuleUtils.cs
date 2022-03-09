using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;

namespace Lilia.Modules.Utils;

public static class ModerationModuleUtils
{
	public static async Task<(bool, IRole)> GetOrCreateRoleAsync(ShardedInteractionContext ctx, string roleName,
		GuildPermissions perms, Color color, bool isHoisted = false, bool isMentionable = false)
	{
		IRole testRole = ctx.Guild.Roles.ToList().Find(x => x.Name == roleName);

		if (testRole != default) return (true, testRole);
		testRole = await ctx.Guild.CreateRoleAsync(roleName, perms, color, isHoisted, isMentionable);

		return (false, testRole);
	}

	public static async Task<IEnumerable<SocketUser>> GetMentionedUsersAsync(ShardedInteractionContext ctx, InteractiveService interactive)
	{
		var result = await interactive.NextMessageAsync(x => x.Channel.Id == ctx.Channel.Id && x.Author == ctx.User);
		var memberList = result.IsSuccess ? result.Value?.MentionedUsers.Distinct() : new List<SocketUser>();
		await result.Value!.DeleteAsync();
		return memberList;
	}
}

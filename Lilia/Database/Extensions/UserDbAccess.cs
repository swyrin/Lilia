using System.Linq;
using DSharpPlus.Entities;
using Lilia.Database.Models;

namespace Lilia.Database.Extensions;

public static class UserDbAccess
{
    public static DbUser GetUserRecord(this LiliaDatabaseContext ctx, DiscordUser discordUser)
    {
        var users = ctx.Users;
        var user = users.FirstOrDefault(entity => entity.DiscordUserId == discordUser.Id) ?? new DbUser
        {
            DiscordUserId = discordUser.Id,
            OsuMode = string.Empty,
            OsuUsername = string.Empty,
            WarnCount = 0
        };
        
        return user;
    }
}
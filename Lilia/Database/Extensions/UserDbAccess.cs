using DSharpPlus.Entities;
using Lilia.Database;
using Lilia.Database.Models;
using System.Linq;

namespace Lilia.Database.Extensions;

public static class UserDbAccess
{
    public static DbUser GetUserRecord(this HelyaDatabaseContext ctx, DiscordUser discordUser)
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
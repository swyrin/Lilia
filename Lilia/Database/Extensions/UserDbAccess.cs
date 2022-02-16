using DSharpPlus.Entities;
using Lilia.Database.Models;
using System.Linq;

namespace Lilia.Database.Extensions;

public static class UserDbAccess
{
    public static DbUser GetUserRecord(this LiliaDatabaseContext ctx, DiscordUser discordUser)
    {
        var users = ctx.Users;
        var user = users.FirstOrDefault(entity => entity.Id == discordUser.Id);
        
        if (user == default)
        {
            user = new DbUser
            {
                Id = discordUser.Id,
                OsuMode = string.Empty,
                OsuUsername = string.Empty,
                WarnCount = 0
            };

            users.Add(user);
            ctx.SaveChanges();
        }

        return user;
    }
}
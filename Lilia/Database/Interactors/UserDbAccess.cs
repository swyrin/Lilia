using System.Linq;
using Discord;
using Lilia.Database.Models;

namespace Lilia.Database.Interactors;

public static class UserDbInteractor
{
    public static DbUser GetUserRecord(this LiliaDatabaseContext ctx, IUser discordUser)
    {
        var users = ctx.Users;
        var user = users.FirstOrDefault(entity => entity.Id == discordUser.Id);

        if (user != default) return user;
        
        user = new DbUser
        {
            Id = discordUser.Id,
            OsuMode = string.Empty,
            OsuUsername = string.Empty,
            WarnCount = 0
        };

        users.Add(user);
        ctx.SaveChanges();

        return user;
    }
}
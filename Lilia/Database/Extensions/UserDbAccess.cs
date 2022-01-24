using System.Linq;
using DSharpPlus.Entities;
using Lilia.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Lilia.Database.Extensions;

public static class UserDbAccess
{
    public static DbUser GetOrCreateUserRecord(this LiliaDbContext ctx, DiscordUser discordUser)
    {
        DbSet<DbUser> users = ctx.Users;
        DbUser user = users.FirstOrDefault(entity => entity.DiscordUserId == discordUser.Id);

        if (user == default(DbUser))
        {
            user = new DbUser
            {
                DiscordUserId = discordUser.Id
            };

            users.Add(user);
        }

        ctx.SaveChanges();
        return user;
    }
}
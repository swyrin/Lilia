using System.Linq;
using Lilia.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Lilia.Database.Extensions;

public static class UserDbAccess
{
    public static DbUser GetOrCreateUserRecord(this LiliaDbContext ctx, ulong userId)
    {
        DbSet<DbUser> users = ctx.Users;
        DbUser user = users.FirstOrDefault(entity => entity.DiscordUserId == userId);

        if (user == default(DbUser))
        {
            user = new DbUser
            {
                DiscordUserId = userId
            };

            users.Add(user);
        }

        ctx.SaveChanges();
        return user;
    }
}
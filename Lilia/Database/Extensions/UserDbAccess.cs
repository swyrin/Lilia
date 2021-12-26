using System.Linq;
using Lilia.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Lilia.Database.Extensions;

public static class UserDbAccess
{
    public static DbUser GetOrCreateUserRecord(this LiliaDbContext ctx, ulong userId)
    {
        DbSet<DbUser> users = ctx.Users;
        DbUser user = users.FirstOrDefault(entity => entity.UserId == userId);

        if (user == default(DbUser))
        {
            user = new DbUser
            {
                UserId = userId,
                Shards = 0,
                OsuMode = -1
            };

            users.Add(user);
        }

        ctx.SaveChanges();
        return user;
    }
}
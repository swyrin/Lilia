using Lilia.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Lilia.Database
{
    public static class UserDbAccess
    {
        public static User GetOrCreateUserRecord(this LiliaDbContext ctx, ulong userId)
        {
            DbSet<User> users = ctx.Users;
            User user = users.FirstOrDefault(entity => entity.UserIndex == userId);

            if (user == default(User))
            {
                user = new User
                {
                    UserIndex = userId,
                    Shards = 0
                };

                users.Add(user);
            }

            ctx.SaveChanges();
            return user;
        }
    }
}
using Lilia.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Lilia.Database
{
    public static class GuildDbAccess
    {
        public static Guild GetOrCreateGuildRecord(this LiliaDbContext ctx, ulong guildId)
        {
            DbSet<Guild> guilds = ctx.Guilds;
            Guild guild = guilds.FirstOrDefault(entity => entity.GuildIndex == guildId);

            if (guild == default(Guild))
            {
                guild = new Guild
                {
                    GuildIndex = guildId,
                    Ranking = 1
                };

                guilds.Add(guild);
            }

            ctx.SaveChanges();
            return guild;
        }
    }
}
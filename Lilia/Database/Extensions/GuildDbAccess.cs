using System.Linq;
using DSharpPlus.Entities;
using Lilia.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Lilia.Database.Extensions;

public static class GuildDbAccess
{
    public static DbGuild GetOrCreateGuildRecord(this LiliaDbContext ctx, DiscordGuild discordGuild)
    {
        DbSet<DbGuild> guilds = ctx.Guilds;
        DbGuild guild = guilds.FirstOrDefault(entity => entity.DiscordGuildId == discordGuild.Id);

        if (guild == default(DbGuild))
        {
            guild = new DbGuild
            {
                DiscordGuildId = discordGuild.Id,
                Queue = string.Empty,
                QueueWithNames = string.Empty
            };

            guilds.Add(guild);
        }

        ctx.SaveChanges();
        return guild;
    }
}
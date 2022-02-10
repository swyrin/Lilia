using System.Linq;
using DSharpPlus.Entities;
using Lilia.Database.Models;

namespace Lilia.Database.Extensions;

public static class GuildDbAccess
{
    public static DbGuild GetOrCreateGuildRecord(this LiliaDatabaseContext ctx, DiscordGuild discordGuild)
    {
        var guilds = ctx.Guilds;
        var guild = guilds.FirstOrDefault(entity => entity.DiscordGuildId == discordGuild.Id);

        if (guild == default(DbGuild))
        {
            guild = new DbGuild
            {
                DiscordGuildId = discordGuild.Id
            };

            guilds.Add(guild);
        }

        ctx.SaveChanges();
        return guild;
    }
}
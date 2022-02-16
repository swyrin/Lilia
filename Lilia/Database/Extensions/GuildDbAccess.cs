using DSharpPlus.Entities;
using Lilia.Database.Models;
using System.Linq;

namespace Lilia.Database.Extensions;

public static class GuildDbAccess
{
    public static DbGuild GetGuildRecord(this LiliaDatabaseContext ctx, DiscordGuild discordGuild)
    {
        var guilds = ctx.Guilds;
        var guild = guilds.FirstOrDefault(entity => entity.Id == discordGuild.Id);
        
        if (guild == default)
        {
            guild = new DbGuild
            {
                Id = discordGuild.Id
            };

            guilds.Add(guild);
            ctx.SaveChanges();
        }
        
        return guild;
    }
}
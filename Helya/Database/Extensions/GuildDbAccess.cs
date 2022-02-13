using System.Linq;
using DSharpPlus.Entities;
using Helya.Database.Models;

namespace Helya.Database.Extensions;

public static class GuildDbAccess
{
    public static DbGuild GetGuildRecord(this HelyaDatabaseContext ctx, DiscordGuild discordGuild)
    {
        var guilds = ctx.Guilds;
        var guild = guilds.FirstOrDefault(entity => entity.DiscordGuildId == discordGuild.Id) ?? new DbGuild
        {
            DiscordGuildId = discordGuild.Id
        };
        
        return guild;
    }
}
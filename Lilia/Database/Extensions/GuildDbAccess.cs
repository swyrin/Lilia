using DSharpPlus.Entities;
using Lilia.Database.Models;
using System.Linq;

namespace Lilia.Database.Extensions;

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
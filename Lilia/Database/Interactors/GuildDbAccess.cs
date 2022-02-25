using System.Linq;
using Discord;
using Lilia.Database.Models;

namespace Lilia.Database.Interactors;

public static class GuildDbInteractor
{
    public static DbGuild GetGuildRecord(this LiliaDatabaseContext ctx, IGuild discordGuild)
    {
        var guilds = ctx.Guilds;
        var guild = guilds.FirstOrDefault(entity => entity.Id == discordGuild.Id);

        if (guild != default) return guild;
        
        guild = new DbGuild
        {
            Id = discordGuild.Id,
            GoodbyeMessage = string.Empty,
            WelcomeMessage = string.Empty,
            IsGoodbyeEnabled = false,
            IsWelcomeEnabled = false,
            GoodbyeChannelId = 0,
            WelcomeChannelId = 0
        };

        guilds.Add(guild);
        ctx.SaveChanges();

        return guild;
    }
}
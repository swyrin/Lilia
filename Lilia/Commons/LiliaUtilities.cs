using DSharpPlus.Entities;
using System;

namespace Lilia.Commons
{
    public static class LiliaUtilities
    {
        public static DiscordEmbedBuilder GetDefaultEmbedTemplateForMember(this DiscordMember member)
        {
            return new DiscordEmbedBuilder()
                .WithTimestamp(DateTime.Now)
                .WithColor(DiscordColor.Rose)
                .WithFooter($"Requested by: {member.DisplayName}#{member.Discriminator}", member.AvatarUrl);
        }
    }
}
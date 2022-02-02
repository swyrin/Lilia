using DSharpPlus.Entities;
using System;
using DSharpPlus;

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

        public static string ToDiscordBold(this string str) => Formatter.Bold(str);
        public static string ToDiscordItalic(this string str) => Formatter.Italic(str);
        public static string ToDiscordUnderline(this string str) => Formatter.Underline(str);
        public static string ToDiscordEscapedFormat(this string str) => Formatter.Sanitize(str);
        public static string ToDiscordNoFormat(this string str) => Formatter.Strip(str);
    }
}
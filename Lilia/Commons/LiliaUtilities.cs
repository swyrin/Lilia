using System;
using System.Text.RegularExpressions;
using DSharpPlus.Entities;

namespace Lilia.Commons;

public static class LiliaUtilities
{
    public static DiscordEmbedBuilder GetDefaultEmbedTemplateForMember(this DiscordUser user)
    {
        return new DiscordEmbedBuilder()
            .WithTimestamp(DateTime.Now)
            .WithColor(DiscordColor.Rose)
            .WithFooter($"Requested by: {user.Username}#{user.Discriminator} (not guild name)", user.AvatarUrl);
    }

    public static Tuple<ulong, ulong, ulong> ResolveDiscordMessageJumpLink(this string str)
    {
        // https://discord.com/channels/guild_id/channel_id/message_id
        string[] segments = new Uri(str).Segments;

        ulong guildId = Convert.ToUInt64(segments[2].Replace('/', '\0'));
        ulong channelId = Convert.ToUInt64(segments[3].Replace('/', '\0'));
        ulong messageId = Convert.ToUInt64(segments[4].Replace('/', '\0'));
        return new Tuple<ulong, ulong, ulong>(guildId, channelId, messageId);
    }

    public static bool IsDiscordValidBotInvite(this string str)
        => !string.IsNullOrWhiteSpace(str) && new Regex(@"(https?:\/\/)?(www\.|canary\.|ptb\.)?discord(app)?\.com\/(api\/)?oauth2\/authorize\?([^ ]+)\/?").IsMatch(str);

    public static bool IsDiscordValidGuildInvite(this string str)
       => !string.IsNullOrWhiteSpace(str) && new Regex(@"(https?:\/\/)?(www\.|canary\.|ptb\.)?discord(\.gg|(app)?\.com\/invite|\.me)\/([^ ]+)\/?").IsMatch(str);
}
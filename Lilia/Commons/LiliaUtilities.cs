using DSharpPlus.Entities;
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Lilia.Commons;

public static class LiliaUtilities
{
    private static readonly CultureInfo EnglishCulture = new("en-GB");
    
    public static DiscordEmbedBuilder GetDefaultEmbedTemplateForUser(this DiscordUser user)
    {
        return new DiscordEmbedBuilder()
            .WithTimestamp(DateTime.Now)
            .WithColor(DiscordColor.Rose)
            .WithFooter($"Requested by: {user.Username}#{user.Discriminator}", user.AvatarUrl);
    }
    
    public static Tuple<ulong, ulong, ulong> ResolveDiscordMessageJumpLink(this string str)
    {
        // https://discord.com/channels/guild_id/channel_id/message_id
        var segments = new Uri(str).Segments;

        var guildId = Convert.ToUInt64(segments[2].Replace('/', '\0'));
        var channelId = Convert.ToUInt64(segments[3].Replace('/', '\0'));
        var messageId = Convert.ToUInt64(segments[4].Replace('/', '\0'));
        
        return new Tuple<ulong, ulong, ulong>(guildId, channelId, messageId);
    }
    
    public static string ToLongReadableTimeSpan(this TimeSpan timeSpan)
    {
        StringBuilder timeStr = new();
        
        if (timeSpan.Days > 0) timeStr.Append(timeSpan.Days).Append(" day").Append(timeSpan.Days >= 2 ? 's' : string.Empty).Append(' ');
        if (timeSpan.Hours > 0) timeStr.Append(timeSpan.Hours).Append(" hour").Append(timeSpan.Hours >= 2 ? 's' : string.Empty).Append(' ');
        if (timeSpan.Minutes > 0) timeStr.Append(timeSpan.Minutes).Append(" minute").Append(timeSpan.Minutes >= 2 ? 's' : string.Empty).Append(' ');
        if (timeSpan.Seconds > 0) timeStr.Append(timeSpan.Seconds).Append(" second").Append(timeSpan.Seconds >= 2 ? 's' : string.Empty);

        return timeStr.ToString();
    }
    
    public static string ToShortReadableTimeSpan(this TimeSpan timeSpan)
    {
        StringBuilder timeStr = new();
        
        if (timeSpan.Days > 0) timeStr.Append(timeSpan.Days).Append('d').Append(' ');
        if (timeSpan.Hours > 0) timeStr.Append(timeSpan.Hours).Append('h').Append(' ');
        if (timeSpan.Minutes > 0) timeStr.Append(timeSpan.Minutes).Append('m').Append(' ');
        if (timeSpan.Seconds > 0) timeStr.Append(timeSpan.Seconds).Append('s');

        return timeStr.ToString();
    }

    public static string ToLongDateTime(this DateTime dateTime)
    {
        return $"{dateTime.ToString("G", EnglishCulture)}";
    }
    
    public static string ToShortDateTime(this DateTime dateTime)
    {
        return $"{dateTime.ToString("g", EnglishCulture)}";
    }

    public static bool IsDiscordValidBotInvite(this string? str)
        => !string.IsNullOrWhiteSpace(str) && new Regex(@"(https?:\/\/)?(www\.|canary\.|ptb\.)?discord(app)?\.com\/(api\/)?oauth2\/authorize\?([^ ]+)\/?").IsMatch(str);

    public static bool IsDiscordValidGuildInvite(this string? str)
       => !string.IsNullOrWhiteSpace(str) && new Regex(@"(https?:\/\/)?(www\.|canary\.|ptb\.)?discord(\.gg|(app)?\.com\/invite|\.me)\/([^ ]+)\/?").IsMatch(str);
}
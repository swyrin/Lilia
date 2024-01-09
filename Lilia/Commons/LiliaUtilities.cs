using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Fergun.Interactive;

namespace Lilia.Commons;

public static class LiliaUtilities
{
	private static readonly CultureInfo EnglishCulture = new("en-GB");

	public static EmbedBuilder CreateEmbedWithUserData(this SocketUser user) =>
		new EmbedBuilder()
			.WithTimestamp(DateTime.Now)
			.WithColor(Color.DarkRed)
			.WithFooter($"Requested by {user.Username}#{user.Discriminator}", user.GetAvatarUrl());

	public static Tuple<ulong, ulong, ulong> ResolveDiscordMessageJumpLink(this string str)
	{
		// https://discord.com/channels/guild_id/channel_id/message_id
		var segments = new Uri(str).Segments;

		var guildId = Convert.ToUInt64(segments[2].Replace("/", string.Empty));
		var channelId = Convert.ToUInt64(segments[3].Replace("/", string.Empty));
		var messageId = Convert.ToUInt64(segments[4].Replace("/", string.Empty));

		return new Tuple<ulong, ulong, ulong>(guildId, channelId, messageId);
	}

	public static string ToLongReadableTimeSpan(this TimeSpan timeSpan)
	{
		StringBuilder timeStr = new();

		if (timeSpan.Days > 0)
			timeStr.Append(timeSpan.Days).Append(" day").Append(timeSpan.Days >= 2 ? 's' : string.Empty).Append(' ');
		if (timeSpan.Hours > 0)
			timeStr.Append(timeSpan.Hours).Append(" hour").Append(timeSpan.Hours >= 2 ? 's' : string.Empty).Append(' ');
		if (timeSpan.Minutes > 0)
			timeStr.Append(timeSpan.Minutes).Append(" minute").Append(timeSpan.Minutes >= 2 ? 's' : string.Empty).Append(' ');
		if (timeSpan.Seconds > 0)
			timeStr.Append(timeSpan.Seconds).Append(" second").Append(timeSpan.Seconds >= 2 ? 's' : string.Empty);

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

	public static string ToLongDateTime(this DateTime dateTime) => $"{dateTime.ToString("G", EnglishCulture)}";

	public static string ToShortDateTime(this DateTime dateTime) => $"{dateTime.ToString("g", EnglishCulture)}";

#nullable enable
	public static bool IsDiscordValidBotInvite(this string? str)
#nullable disable
	{
		return !string.IsNullOrWhiteSpace(str) &&
		       new Regex(@"(https?:\/\/)?(www\.|canary\.|ptb\.)?discord(app)?\.com\/(api\/)?oauth2\/authorize\?([^ ]+)\/?").IsMatch(str);
	}

#nullable enable
	public static bool IsDiscordValidGuildInvite(this string? str)
#nullable disable
	{
		return !string.IsNullOrWhiteSpace(str) &&
		       new Regex(@"(https?:\/\/)?(www\.|canary\.|ptb\.)?discord(\.gg|(app)?\.com\/invite|\.me)\/([^ ]+)\/?").IsMatch(str);
	}

	public static IEnumerable<PageBuilder> CreatePagesFromString(string content, int fixedPageSplit = 15, int threshold = 2000)
	{
		var lines = content.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var idx = 0;
		List<PageBuilder> pages = new();
		StringBuilder text = new();

		foreach (var line in lines)
		{
			if ((idx != 0 && idx % fixedPageSplit == 0) || text.Length + line.Length > threshold)
			{
				pages.Add(new PageBuilder().WithText($"{text}"));
				text.Clear();
			}

			text.AppendLine(line);
			++idx;
		}

		pages.Add(new PageBuilder().WithText($"{text}"));
		return pages;
	}
}

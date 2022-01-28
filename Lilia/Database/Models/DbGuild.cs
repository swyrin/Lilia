using System;

namespace Lilia.Database.Models;

public class DbGuild
{
    public ulong DbGuildId { get; set; }
    public ulong DiscordGuildId { get; set; }

    // Separated by ||
    public string Queue { get; set; }
    public string QueueWithNames { get; set; }
    public bool IsPlaying { get; set; }
    public TimeSpan LastPlayedPosition { get; set; }
    public string LastPlayedTrack { get; set; }
}
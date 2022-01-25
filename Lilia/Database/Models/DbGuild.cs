namespace Lilia.Database.Models;

public class DbGuild
{
    public ulong DbGuildId { get; set; }
    public ulong DiscordGuildId { get; set; }

    // Separated by ||
    public string Queue { get; set; }
    public string QueueWithNames { get; set; }
    // end
    
    public bool IsPlaying { get; set; }
}
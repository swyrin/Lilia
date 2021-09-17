namespace Lilia.Database.Models
{
    public class DbUser
    {
        public ulong DbUserId { get; set; }
        public ulong UserId { get; set; }
        public ulong Shards { get; set; }
        public int OsuMode { get; set; }
        public string OsuUsername { get; set; }
    }
}
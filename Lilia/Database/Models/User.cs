namespace Lilia.Database.Models
{
    public class User
    {
        public ulong UserId { get; set; }
        public ulong UserIndex { get; set; }
        public ulong Shards { get; set; }
    }
}
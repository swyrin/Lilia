using Lilia.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Lilia.Database;

public class LiliaDatabaseContext : DbContext
{
    public LiliaDatabaseContext(DbContextOptions<LiliaDatabaseContext> options) : base(options)
    {
    }

    public DbSet<DbGuild> Guilds { get; set; }
    public DbSet<DbUser> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        base.OnConfiguring(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
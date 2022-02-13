using Helya.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Helya.Database;

public class HelyaDatabaseContext : DbContext
{
    public HelyaDatabaseContext(DbContextOptions<HelyaDatabaseContext> options) : base(options)
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
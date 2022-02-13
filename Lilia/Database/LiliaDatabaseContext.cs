using Lilia.Commons;
using Lilia.Database.Models;
using Lilia.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Serilog;
using Serilog.Extensions.Logging;

namespace Lilia.Database;

public class LiliaDatabaseContextFactory : IDesignTimeDbContextFactory<LiliaDatabaseContext>
{
    public LiliaDatabaseContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LiliaDatabaseContext>();
        var connStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = "database.db",
            Password = JsonManager<BotConfiguration>.Read().Credentials.DbPassword
        };

        optionsBuilder
            .UseLoggerFactory(new SerilogLoggerFactory(Log.Logger))
            .UseSqlite(connStringBuilder.ToString());
        
        var ctx = new LiliaDatabaseContext(optionsBuilder.Options);
        ctx.Database.SetCommandTimeout(30);
        return ctx;
    }
}

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
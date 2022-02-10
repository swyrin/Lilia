using System;
using Lilia.Database.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lilia.Database;

public class LiliaDatabaseContextFactory : IDesignTimeDbContextFactory<LiliaDatabaseContext>
{
    public LiliaDatabaseContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LiliaDatabaseContext>();
        var connStringBuilder = new SqliteConnectionStringBuilder($"Data Source=database.db;Password={Environment.GetEnvironmentVariable("DB_PASSWORD")}");

        optionsBuilder.UseSqlite(connStringBuilder.ToString());
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
using Lilia.Database.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace Lilia.Database
{
    public class LiliaDbContextFactory : IDesignTimeDbContextFactory<LiliaDbContext>
    {
        public LiliaDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<LiliaDbContext> optionsBuilder = new DbContextOptionsBuilder<LiliaDbContext>();
            SqliteConnectionStringBuilder connStringBuilder = new SqliteConnectionStringBuilder($"Data Source=database.db;Password={Environment.GetEnvironmentVariable("DB_PASSWORD")}");

            optionsBuilder.UseSqlite(connStringBuilder.ToString());
            LiliaDbContext ctx = new LiliaDbContext(optionsBuilder.Options);
            ctx.Database.SetCommandTimeout(30);
            return ctx;
        }
    }

    public class LiliaDbContext : DbContext
    {
        public DbSet<DbGuild> Guilds { get; set; }
        public DbSet<DbUser> Users { get; set; }

        public LiliaDbContext(DbContextOptions<LiliaDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
        }
    }
}
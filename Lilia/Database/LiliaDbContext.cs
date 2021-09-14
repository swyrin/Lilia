using Lilia.Database.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;

namespace Lilia.Database
{
    public class LiliaDbContext : DbContext
    {
        public DbSet<Guild> Guilds { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var connection = new SqliteConnection("Data Source=database.db;");
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA key = \"{Environment.GetEnvironmentVariable("DB_PASSWORD")}\";";

            cmd.ExecuteNonQuery();
            options.UseSqlite(connection);
        }
    }
}
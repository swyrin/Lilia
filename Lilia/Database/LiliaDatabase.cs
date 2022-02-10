using System.Linq;
using Lilia.Commons;
using Lilia.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Lilia.Database;

public class LiliaDatabase
{
    private readonly DbContextOptions<LiliaDatabaseContext> options;

    public LiliaDatabase()
    {
        var optionsBuilder = new DbContextOptionsBuilder<LiliaDatabaseContext>();
        var connStringBuilder = new SqliteConnectionStringBuilder($"Data Source=database.db;Password={JsonManager<BotConfiguration>.Read().Credentials.DbPassword}");

        optionsBuilder.UseSqlite(connStringBuilder.ToString());
        options = optionsBuilder.Options;
        Setup();
    }

    private void Setup()
    {
        Log.Logger.Information("Executing database migrations, if any");

        using var context = new LiliaDatabaseContext(options);
        while (context.Database.GetPendingMigrations().Any())
        {
            var nextMigration = context.Database.GetPendingMigrations().First();
            using var migrationContext = new LiliaDatabaseContext(options);
            migrationContext.Database.Migrate();
            migrationContext.SaveChanges();
        }

        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");
        context.SaveChanges();
    }

    public LiliaDatabaseContext GetContext()
    {
        var context = new LiliaDatabaseContext(options);
        context.Database.SetCommandTimeout(30);
        var conn = context.Database.GetDbConnection();
        conn.Open();

        using var com = conn.CreateCommand();
        // https://phiresky.github.io/blog/2020/sqlite-performance-tuning/
        com.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=OFF";
        com.ExecuteNonQuery();

        return context;
    }
}
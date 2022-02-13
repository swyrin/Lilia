using System.Linq;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Extensions.Logging;

namespace Lilia.Services;

public class LiliaDatabase
{
    private readonly DbContextOptions<LiliaDatabaseContext> _options;

    public LiliaDatabase()
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
        
        _options = optionsBuilder.Options;
        Setup();
    }

    private void Setup()
    {
        using var context = new LiliaDatabaseContext(_options);
        while (context.Database.GetPendingMigrations().Any())
        {
            var migrationContext = new LiliaDatabaseContext(_options);
            migrationContext.Database.Migrate();
            migrationContext.SaveChanges();
            migrationContext.Dispose();
        }

        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");
        context.SaveChanges();
    }

    public LiliaDatabaseContext GetContext()
    {
        var context = new LiliaDatabaseContext(_options);
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
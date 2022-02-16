using Lilia.Commons;
using Lilia.Database;
using Lilia.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Extensions.Logging;
using System.Linq;

namespace Lilia.Services;

public class LiliaDatabase
{
    public LiliaDatabase()
    {
        using var context = new LiliaDatabaseContext(LiliaClient.OptionsBuilder.Options);
        
        while (context.Database.GetPendingMigrations().Any())
        {
            var migrationContext = new HelyaDatabaseContext(_options);
            migrationContext.Database.Migrate();
            migrationContext.SaveChanges();
            migrationContext.Dispose();
        }

        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");
        context.SaveChanges();
    }

    public LiliaDatabaseContext GetContext()
    {
        var context = new HelyaDatabaseContext(_options);
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
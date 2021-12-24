using Lilia.Commons;
using Lilia.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq;
using Serilog;

namespace Lilia.Services;

public class LiliaDatabase
{
    private DbContextOptions<LiliaDbContext> options;

    public LiliaDatabase()
    {
        DbContextOptionsBuilder<LiliaDbContext> optionsBuilder = new DbContextOptionsBuilder<LiliaDbContext>();
        SqliteConnectionStringBuilder connStringBuilder = new SqliteConnectionStringBuilder($"Data Source=database.db;Password={JsonConfigurationsManager.Configurations.Credentials.DbPassword}");

        optionsBuilder.UseSqlite(connStringBuilder.ToString());
        this.options = optionsBuilder.Options;
        this.Setup();
    }

    private void Setup()
    {
        Log.Logger.Information("Executing database migrations, if any.");
        
        using (LiliaDbContext context = new LiliaDbContext(this.options))
        {
            while (context.Database.GetPendingMigrations().Any())
            {
                using (LiliaDbContext migrationContext = new LiliaDbContext(this.options))
                {
                    migrationContext.Database.Migrate();
                    migrationContext.SaveChanges();
                    Log.Logger.Information("Migrated upcoming changes on databases.");
                }
            }

            context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");
            context.SaveChanges();
        }
    }

    public LiliaDbContext GetContext()
    {
        LiliaDbContext context = new LiliaDbContext(this.options);
        context.Database.SetCommandTimeout(30);
        DbConnection conn = context.Database.GetDbConnection();
        conn.Open();

        using (DbCommand com = conn.CreateCommand())
        {
            // https://phiresky.github.io/blog/2020/sqlite-performance-tuning/
            com.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=OFF";
            com.ExecuteNonQuery();
        }
        
        return context;
    }
}
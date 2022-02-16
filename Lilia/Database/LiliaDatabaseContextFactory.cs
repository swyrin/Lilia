using Lilia.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lilia.Database;

public class HelyaDatabaseContextFactory : IDesignTimeDbContextFactory<LiliaDatabaseContext>
{
    public LiliaDatabaseContext CreateDbContext(string[] args)
    {
        var ctx = new LiliaDatabaseContext(LiliaClient.OptionsBuilder.Options);
        ctx.Database.SetCommandTimeout(30);
        return ctx;
    }
}
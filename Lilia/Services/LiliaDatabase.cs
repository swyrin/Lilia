using System.Linq;
using Lilia.Database;
using Microsoft.EntityFrameworkCore;

namespace Lilia.Services;

public class LiliaDatabase
{
	public LiliaDatabase()
	{
		using var context = new LiliaDatabaseContext(LiliaClient.OptionsBuilder.Options);

		while (context.Database.GetPendingMigrations().Any())
		{
			var migrationContext = new LiliaDatabaseContext(LiliaClient.OptionsBuilder.Options);
			migrationContext.Database.Migrate();
			migrationContext.SaveChanges();
			migrationContext.Dispose();
		}

		context.SaveChanges();
	}

	public LiliaDatabaseContext GetContext()
	{
		var context = new LiliaDatabaseContext(LiliaClient.OptionsBuilder.Options);
		context.Database.SetCommandTimeout(30);

		return context;
	}
}

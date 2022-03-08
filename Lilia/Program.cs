using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lilia.Services;
using Serilog;

namespace Lilia;

internal static class Program
{
	private static async Task Main()
	{
		var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
		Console.Title = $"Lilia v{currentVersion}";
		Console.OutputEncoding = Encoding.Unicode;

		Log.Logger = new LoggerConfiguration()
			.Enrich.WithProperty("SourceContext", "Lilia")
			.WriteTo.Console(
				outputTemplate:
				"{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}")
			.MinimumLevel.Debug()
			.CreateLogger();

#if DEBUG
		Log.Logger.Warning("Unless you are testing the code, you should NOT see this on production");
		Log.Logger.Warning("Consider appending \"-c Release\" when running/building the code");
#endif

		Log.Logger.Debug("Starting");
		await new LiliaClient().RunAsync();
	}
}

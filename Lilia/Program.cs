using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Lilia.Services;
using Serilog;

namespace Lilia
{
    internal static class Program
    {
        private static async Task Main()
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            Console.Title = $"Lilia v{currentVersion}";

            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("SourceContext", "Lilia")
                .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}")
                .MinimumLevel.Verbose()
                .CreateLogger();

#if DEBUG
            Log.Logger.Warning("Unless you are testing the code, you should NOT see this on production");
            Log.Logger.Warning("Consider appending \"-c Release\" when running/building the code");
#endif

            await EnsureConfigFile();

            Log.Logger.Debug("Starting");
            await new LiliaClient().RunAsync();
        }

        private static async Task EnsureConfigFile()
        {
            const string fileName = "config.json";

            if (!File.Exists(fileName))
            {
                var jsonString = JsonSerializer.Serialize(new BotConfiguration());
                await File.WriteAllTextAsync(fileName, jsonString);

                var ex = new FileNotFoundException("Config file not found. The program has generated a new one.");

                Log.Error(ex, "The program ran into an error.");

                throw ex;
            }
        }
    }
}

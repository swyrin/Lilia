using Lilia.Services;
using Serilog;

namespace Lilia;

internal static class Program
{
    private static void Main()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Debug()
            .CreateLogger();

        Log.Logger.Debug("Starting");
        new HelyaClient().Run().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
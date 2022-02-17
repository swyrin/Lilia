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
        
#if DEBUG
        Log.Logger.Warning("Unless you are testing the code, you should NOT see this on production");
        Log.Logger.Warning("Consider using \"-c Release\" when running/building the code");
#endif
        
        Log.Logger.Debug("Starting");
        new LiliaClient().Run().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
using System;
using System.Text;
using Lilia.Services;
using Serilog;

namespace Lilia;

internal static class Program
{
    private static void Main()
    {
        // just a precaution
        Console.OutputEncoding = Encoding.Unicode;

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Debug()
            .CreateLogger();

        Log.Logger.Debug("Starting");
        new LiliaClient().Run().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
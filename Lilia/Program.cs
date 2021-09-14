using Lilia.Services;

namespace Lilia
{
    internal static class Program
    {
        private static void Main()
        {
            new LiliaClient().Run().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
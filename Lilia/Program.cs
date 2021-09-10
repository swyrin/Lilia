using Lilia.Services;

namespace Lilia
{
    static class Program
    {
        private static void Main()
        {
            new LiliaClient().Run().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}

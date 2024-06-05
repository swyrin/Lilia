using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Lilia.Services
{
    /// <summary>
    /// The bot service itself.
    /// </summary>
    public class LiliaService : IHostedService
    {
        /// <summary>
        /// The current version.
        /// </summary>
        public readonly Version LiliaVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// The underlying Discord client. Retrieved via DI.
        /// </summary>
        private readonly DiscordClient discordClient;

        /// <summary>
        /// The underlying service collection. Retrieved via DI.
        /// </summary>
        private readonly IServiceCollection serviceCollection;

        /// <summary>
        ///     <inheritdoc cref="LiliaService"/>
        /// </summary>
        public LiliaService(
            DiscordClient client,
            IServiceCollection serviceCollection)
        {
            this.discordClient = client;
            this.serviceCollection = serviceCollection;
        }

        /// <summary>
        /// Register bot events.
        /// </summary>
        private void RegisterEvents()
        {
            this.serviceCollection.ConfigureEventHandlers(x => { });
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.Title = $"Lilia ({this.LiliaVersion})";

#if DEBUG
            // unless I fucked the running config.
            Log.Logger.Warning("Unless you are **TESTING** the code, you should NOT see this on production.");
#endif

            await discordClient.ConnectAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await discordClient.DisconnectAsync();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetEnv;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace Lilia.Services
{
    public class LiliaClient
    {
        public Dictionary<string, string> Configurations;
        public CancellationTokenSource Cts;

        private void InitialSetup()
        {
            this.Configurations = Env.Load().ToDictionary();
            this.Cts = new CancellationTokenSource();
        }

        public async Task Run()
        {
            this.InitialSetup();

            DiscordClient client = new(new DiscordConfiguration
            {
                Token = Configurations["DISCORD_TOKEN"],
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Debug
            });

            client.Ready += this.OnReady;
            client.GuildAvailable += this.OnGuildAvailable;
            client.ClientErrored += this.OnClientError;

            await client.ConnectAsync();
            if (!Cts.IsCancellationRequested) await Task.Delay(2000);
            await client.DisconnectAsync();
        }

        private Task OnReady(DiscordClient sender, ReadyEventArgs e)
        {
            sender.Logger.Log(LogLevel.Information, "Client is ready to serve.");
            return Task.CompletedTask;
        }

        private Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            sender.Logger.Log(LogLevel.Warning, $"Guild cached : {e.Guild.Name}");
            return Task.CompletedTask;
        }

        private Task OnClientError(DiscordClient sender, ClientErrorEventArgs e)
        {
            throw e.Exception;
        }
    }
}

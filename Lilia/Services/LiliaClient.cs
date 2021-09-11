using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetEnv;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Lilia.Commands;
using Microsoft.Extensions.DependencyInjection;
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

            SlashCommandsExtension slashCmd = client.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = new ServiceCollection().AddSingleton(this).BuildServiceProvider()
            });

            client.Ready += this.OnReady;
            client.GuildAvailable += this.OnGuildAvailable;
            client.ClientErrored += this.OnClientError;

            slashCmd.RegisterCommands<Test>();

            await client.ConnectAsync();
            await Task.Delay(-1);

            while (!Cts.IsCancellationRequested) await Task.Delay(2000);

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

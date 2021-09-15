using DotNetEnv;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Lilia.Commands.Slash;
using Lilia.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Lilia.Services
{
    public class LiliaClient
    {
        public CancellationTokenSource Cts;
        public LiliaDbContext DbCtx;

        private void InitialSetup()
        {
            Env.Load();
            this.Cts = new CancellationTokenSource();
            this.DbCtx = new LiliaDbContext();
        }

        public async Task Run()
        {
            this.InitialSetup();

            DiscordClient client = new(new DiscordConfiguration
            {
                Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Debug
            });

            ServiceProvider services = new ServiceCollection().AddSingleton(this).BuildServiceProvider();

            CommandsNextExtension commandsNext = client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new[] { "l." },
                DmHelp = true,
                Services = services
            });

            SlashCommandsExtension slash = client.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = services
            });

            commandsNext.RegisterCommands(Assembly.GetExecutingAssembly());
            slash.RegisterCommands<Test>();

            client.Ready += this.OnReady;
            client.GuildAvailable += this.OnGuildAvailable;
            client.ClientErrored += this.OnClientError;

            await client.ConnectAsync();
            await Task.Delay(-1);

            while (!Cts.IsCancellationRequested) await Task.Delay(2000);

            await client.DisconnectAsync();
        }

        private Task OnCommandsNextCommandErrored()
        {

        }

        private Task OnReady(DiscordClient sender, ReadyEventArgs e)
        {
            sender.Logger.Log(LogLevel.Information, "Client is ready to serve.");
            return Task.CompletedTask;
        }

        private Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            sender.Logger.Log(LogLevel.Information, $"Guild cached : {e.Guild.Name}");
            return Task.CompletedTask;
        }

        private Task OnClientError(DiscordClient sender, ClientErrorEventArgs e)
        {
            throw e.Exception;
        }
    }
}
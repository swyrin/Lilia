using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Helya.Services;
using Serilog;

namespace Helya.Modules;

public class OwnerModule : ApplicationCommandModule
{
    private readonly HelyaClient _client;

    public OwnerModule(HelyaClient client)
    {
        _client = client;
    }

    [SlashCommand("shutdown", "Shutdown the bot")]
    [SlashRequireOwner]
    public async Task ShutdownCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        Log.Logger.Warning("Shutting down");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Goodbye"));

        _client.Cts.Cancel();
    }

    [SlashCommand("refresh", "Refreshes slash commands")]
    [SlashRequireOwner]
    public async Task CommandRefreshCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Processing"));

        StringBuilder response = new();
        var slasher = ctx.Client.GetSlashCommands();

        // actually I can do this but I need more "verbose"
        // await slasher.RefreshCommands()
        var guildRegisteredCommands = slasher.RegisteredCommands;

        foreach (var (guildId, commands) in guildRegisteredCommands)
        {
            if (guildId == null)
            {
                Log.Logger.Warning("Refreshing slash commands in global scope");
                response.AppendLine("Refreshing slash commands in global scope");
                await ctx.Client.BulkOverwriteGlobalApplicationCommandsAsync(commands);
                continue;
            }

            Log.Logger.Warning($"Refreshing slash commands for private guild with ID {guildId.GetValueOrDefault()}");
            response.AppendLine($"Refreshing slash commands for private guild with ID {guildId.GetValueOrDefault()}");
            await ctx.Client.BulkOverwriteGuildApplicationCommandsAsync(guildId.Value, commands);
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent(response.ToString()));
    }
}
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Lilia.Services;
using Serilog;
using System.Threading.Tasks;

namespace Lilia.Modules;

public class OwnerModule : ApplicationCommandModule
{
    [SlashCommand("shutdown", "Shutdown the bot")]
    [SlashRequireOwner]
    public async Task ShutdownCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);

        Log.Logger.Warning("Shutting down");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Goodbye"));

        LiliaClient.Cts.Cancel();
    }

    [SlashCommand("refresh", "Refreshes slash commands")]
    [SlashRequireOwner]
    public async Task CommandRefreshCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Processing"));

        await ctx.Client.GetSlashCommands().RefreshCommands();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Done refreshing slash commands"));
    }
}
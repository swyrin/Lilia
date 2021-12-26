using Lilia.Services;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Serilog;

namespace Lilia.Modules;

public class OwnerModule : ApplicationCommandModule
{
    private LiliaClient _client;

    public OwnerModule(LiliaClient client)
    {
        this._client = client;
    }

    [SlashCommand("shutdown", "Shutdown the bot, obviously. Only bot owner(s) can do that.")]
    [SlashRequireOwner]
    public async Task ShutdownCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        Log.Logger.Warning("Shutting down");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Goodbye"));

        this._client.Cts.Cancel();
    }
}
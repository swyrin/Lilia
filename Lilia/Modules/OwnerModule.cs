using Lilia.Database;
using Lilia.Services;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Serilog;

namespace Lilia.Modules;

public class OwnerModule : ApplicationCommandModule
{
    private LiliaClient _client;
    private LiliaDbContext _dbCtx;

    public OwnerModule(LiliaClient client)
    {
        this._client = client;
        this._dbCtx = client.Database.GetContext();
    }

    [SlashCommand("shutdown", "Shutdown the bot, obviously. Only bot owner(s) can do that.")]
    [SlashRequireOwner]
    public async Task ShutdownCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Goodbye"));
        Log.Logger.Warning("Shutting down");
        this._client.Cts.Cancel();
    }
}
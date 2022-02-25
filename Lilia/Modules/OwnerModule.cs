using Lilia.Services;
using Serilog;
using System.Threading.Tasks;
using Discord.Interactions;

namespace Lilia.Modules;

public class OwnerModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("shutdown", "Shutdown the bot")]
    [RequireOwner]
    public async Task OwnerShutdownCommand()
    {
        await Context.Interaction.DeferAsync();

        Log.Logger.Warning("Shutting down");

        await Context.Interaction.ModifyOriginalResponseAsync(x =>
            x.Content = "Goodbye");

        LiliaClient.Cts.Cancel();
    }     
}
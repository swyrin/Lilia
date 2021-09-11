using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace Lilia.Commands
{
    public class Test : ApplicationCommandModule
    {
        [SlashCommand("test", "Just testing.")]
        public async Task TestCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);
            await Task.Delay(5000);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("After 5 secs. I am back."));
        }
    }
}

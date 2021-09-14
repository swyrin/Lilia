using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Lilia.Database;
using Lilia.Services;
using System.Threading.Tasks;

namespace Lilia.Commands.Slash
{
    public class Test : ApplicationCommandModule
    {
        private LiliaClient _client;
        private LiliaDbContext _dbCtx;

        public Test(LiliaClient client)
        {
            this._client = client;
            this._dbCtx = client.DbCtx;
        }

        [SlashCommand("test", "Just a test command.")]
        public async Task TestCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await Task.Delay(5000);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("After 5 seconds, I am here."));
        }
    }
}
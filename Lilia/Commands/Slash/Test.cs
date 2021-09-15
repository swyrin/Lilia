using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Lilia.Database;
using Lilia.Database.Models;
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
            this._dbCtx = client.Database.GetContext();
        }

        [SlashCommand("test", "Just a test command.")]
        public async Task TestCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await Task.Delay(5000);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("After 5 seconds, I am here."));
        }

        [SlashCommand("add5", "Add 5 shards to your inventory.")]
        public async Task AddFiveAsync(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            User user = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);
            user.Shards += 5;
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Added 5 shards to your inventory"));
        }

        [SlashCommand("check", "Check shards count in your inventory.")]
        public async Task CheckShardsAsync(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            User user = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"You are having {user.Shards} shards left."));
        }
    }
}
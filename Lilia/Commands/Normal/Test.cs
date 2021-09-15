using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Lilia.Database;
using Lilia.Database.Models;
using Lilia.Services;
using System.Threading.Tasks;

namespace Lilia.Commands.Normal
{
    public class Test : BaseCommandModule
    {
        private LiliaClient _client;
        private LiliaDbContext _dbCtx;

        public Test(LiliaClient client)
        {
            this._client = client;
            this._dbCtx = client.Database.GetContext();
        }

        [Command("test")]
        [Description("Just a test command.")]
        public async Task TestCommand(CommandContext ctx)
        {
            DiscordMessage msg = await ctx.RespondAsync("Thinking...");
            await Task.Delay(5000);
            await msg.ModifyAsync("After 5 seconds, this message is edited.");
        }

        [Command("add5")]
        [Description("Add 5 shards to your inventory.")]
        public async Task AddFiveAsync(CommandContext ctx)
        {
            User user = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);
            user.Shards += 5;
            await ctx.RespondAsync("Added 5 shards to your inventory");
        }

        [Command("check")]
        [Description("Check shards count in your inventory.")]
        public async Task CheckShardsAsync(CommandContext ctx)
        {
            User user = this._dbCtx.GetOrCreateUserRecord(ctx.Member.Id);
            await ctx.RespondAsync($"You are having {user.Shards} shards left.");
        }
    }
}
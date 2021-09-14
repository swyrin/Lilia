using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Lilia.Database;
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
            this._dbCtx = client.DbCtx;
        }

        [Command("test")]
        [Description("Just a test command.")]
        public async Task TestCommand(CommandContext ctx)
        {
            DiscordMessage msg = await ctx.RespondAsync("Thinking...");
            await Task.Delay(5000);
            await msg.ModifyAsync("After 5 seconds, this message is edited.");
        }
    }
}
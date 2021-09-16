using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Lilia.Database;
using Lilia.Services;
using System.Threading.Tasks;

namespace Lilia.Modules
{
    public class OwnerModule : BaseCommandModule
    {
        private LiliaClient _client;
        private LiliaDbContext _dbCtx;

        public OwnerModule(LiliaClient client)
        {
            this._client = client;
            this._dbCtx = client.Database.GetContext();
        }

        [Command("shutdown")]
        [Description("Shutdown the bot.")]
        [RequireOwner]
        public async Task ShutdownCommand(CommandContext ctx)
        {
            await ctx.RespondAsync("Hope to see you again.");
            this._client.Cts.Cancel();
        }
    }
}

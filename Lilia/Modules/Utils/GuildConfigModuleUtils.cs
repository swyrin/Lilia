using Discord.Interactions;

namespace Lilia.Modules.Utils
{
    public static class GuildConfigModuleUtils
    {
        public static bool IsChannelExist(ShardedInteractionContext ctx, ulong testId) => ctx.Guild.GetChannel(testId) != null;
    }
}

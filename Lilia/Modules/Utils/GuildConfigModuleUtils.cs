using Discord.Interactions;

namespace Lilia.Modules.Utils;

public static class GuildConfigModuleUtils
{
    public static bool IsChannelExist(SocketInteractionContext ctx, ulong testId)
    {
        var welcomeChn = ctx.Guild.GetChannel(testId);
        return welcomeChn != null;
    }
}
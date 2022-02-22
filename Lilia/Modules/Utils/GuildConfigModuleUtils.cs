using DSharpPlus.SlashCommands;

namespace Lilia.Modules.Utils;

public static class GuildConfigModuleUtils
{
    public static bool IsChannelExist(InteractionContext ctx, ulong testId)
    {
        var welcomeChn = ctx.Guild.GetChannel(testId);
        return welcomeChn != null;
    }
}
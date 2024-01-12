using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Lilia.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequireMemberInVoiceAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            return ((SocketGuildUser)context.User).VoiceState == null
                ? Task.FromResult(PreconditionResult.FromError("You need to join the voice channel."))
                : Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}

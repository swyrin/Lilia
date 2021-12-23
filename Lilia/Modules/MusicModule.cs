using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;

namespace Lilia.Modules
{
    public class MusicModule : BaseCommandModule
    {
        [Command("summon")]
        [RequireOwner]
        public async Task SummonToVoiceCommand(CommandContext ctx)
        {
            DiscordChannel channel = ctx.Member.VoiceState?.Channel;

            if (channel == null)
            {
                await ctx.RespondAsync("Join a voice channel please.");
                return;
            }
            
            await channel.ConnectAsync();
            await ctx.RespondAsync("Connected.");
        }
        
        [Command("leave")]
        [RequireOwner]
        public async Task LeaveVoiceCommand(CommandContext ctx)
        {
            VoiceNextExtension ext = ctx.Client.GetVoiceNext();
            VoiceNextConnection conn = ext.GetConnection(ctx.Guild);
            
            conn.Disconnect();
            await ctx.RespondAsync("Disconnected.");
        }

        [Command("radio")]
        [RequireOwner]
        public async Task TransmitRadioCommand(CommandContext ctx, string uri = "https://listen.moe/stream")
        {
            VoiceNextExtension ext = ctx.Client.GetVoiceNext();
            VoiceNextConnection conn = ext.GetConnection(ctx.Guild);
            VoiceTransmitSink sink = conn.GetTransmitSink();

            Process ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -err_detect ignore_err -i {uri} -f f32le -ar 48000 -vn -ac 2 pipe:1 -loglevel error", 
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            
            Stream output = ffmpeg.StandardOutput.BaseStream;
            
            await output.CopyToAsync(sink);
            await output.DisposeAsync();
        }
    }
}
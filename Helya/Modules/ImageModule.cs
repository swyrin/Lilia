using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Nekos.Net.V2;
using Nekos.Net.V2.Endpoint;
using Serilog;
using Serilog.Extensions.Logging;

namespace Helya.Modules;

public class ImageModule : ApplicationCommandModule
{
    [SlashCommand("neko", "Get a neko image")]
    public async Task GetNekoImageCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);
        NekosV2Client nekosV2Client = new(new SerilogLoggerProvider(Log.Logger).CreateLogger("Helya"));

        List<Page> savedNekos = new();

        var nextBtn = new DiscordButtonComponent(ButtonStyle.Success, "nextBtn", "Next");
        var saveBtn = new DiscordButtonComponent(ButtonStyle.Primary, "saveBtn", "Save");
        var stopBtn = new DiscordButtonComponent(ButtonStyle.Danger, "stopBtn", "Stop"); ;
        
        do
        {
            string imageUrl = (await nekosV2Client.RequestSfwResultsAsync(SfwEndpoint.Neko)).First().Url;    
            
            DiscordMessage msg = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Here is your neko\n{imageUrl}")
                .AddComponents(saveBtn, stopBtn, nextBtn));

            var res = await msg.WaitForButtonAsync();

            if (res.TimedOut)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Timed out"));
                
                break;
            }

            if (res.Result.Id == "stopBtn")
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Goodbye"));

                break;
            }

            switch (res.Result.Id)
            {
                case "saveBtn":
                    savedNekos.Add(new Page($"Your previous saved neko:\n{imageUrl}"));
                    continue;
                case "nextBtn":
                    continue;
            }
        } while (true);

        if (savedNekos.Any())
            await ctx.Interaction.SendPaginatedResponseAsync(true, ctx.Member, savedNekos, asEditResponse: true);
    }
}
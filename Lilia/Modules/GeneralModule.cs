using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Lilia.Commons;
using Lilia.Services;

namespace Lilia.Modules;

public class GeneralModule : ApplicationCommandModule
{
    private LiliaClient _client;

    public GeneralModule(LiliaClient client)
    {
        this._client = client;
    }

    [SlashCommand("uptime", "Check the uptime of the bot")]
    public async Task UptimeCheckCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);

        DiscordEmbedBuilder embedBuilder = LiliaUtilities.GetDefaultEmbedTemplate(ctx.Member)
            .WithTitle("My uptime")
            .AddField("Uptime", $"{DateTime.Now.Subtract(this._client.StartTime):g}", true)
            .AddField("Start since", this._client.StartTime.ToLongDateString(), true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(embedBuilder.Build()));
    }
}
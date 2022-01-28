using System;
using System.Threading.Tasks;
using DSharpPlus;
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
    
    [SlashCommand("changes", "What's new in this version of \"me\"")]
    public async Task ViewChangelogsCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);

        DiscordEmbedBuilder embedBuilder = LiliaUtilities.GetDefaultEmbedTemplate(ctx.Member)
            .WithTitle("Here is the changelogs")
            .AddField("All versions", Formatter.MaskedUrl("Click me!", new Uri("https://github.com/Swyreee/Lilia/blob/master/CHANGELOGS.md") , "A GitHub link"), true)
            .AddField("Development version", Formatter.MaskedUrl("Click me!", new Uri("https://github.com/Swyreee/Lilia/blob/master/CHANGELOG.md"), "A GitHub link"), true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(embedBuilder.Build()));
    }
}
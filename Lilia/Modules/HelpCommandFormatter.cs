using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lilia.Modules;

public class HelpCommandFormatter : BaseHelpFormatter
{
    private readonly DiscordEmbedBuilder _helpEmbedBuilder;
    private Command _currentCommand;
    private readonly CommandContext _currentCommandContext;

    public HelpCommandFormatter(CommandContext ctx) : base(ctx)
    {
        this._currentCommandContext = ctx;

        this._helpEmbedBuilder = new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Red)
            .WithTimestamp(DateTime.Now)
            .WithTitle("Help message")
            .WithDescription("This is a list of available commands, groups or details of a command")
            .WithFooter(
                $"Requested by {this._currentCommandContext.Member.DisplayName}#{this._currentCommandContext.Member.Discriminator}",
                this._currentCommandContext.Member.AvatarUrl);
    }

    public override BaseHelpFormatter WithCommand(Command command)
    {
        if (this._currentCommand is CommandGroup) return this;

        this._currentCommand = command;

        this._helpEmbedBuilder
            .AddField("Explanations",
                "`<argument>`: required argument\n`[argument]`: optional argument\n`argument..`: take to the last.")
            .AddField("Description", this._currentCommand.Description ?? "No description provided");

        int overloadCount = 1;

        foreach (CommandOverload overload in this._currentCommand.Overloads)
        {
            StringBuilder argsBuilder = new StringBuilder();
            StringBuilder commandNameWithAliases = new StringBuilder($"{this._currentCommand.Name}");
            StringBuilder usageBuilder =
                new StringBuilder(
                    $"{this._currentCommandContext.Prefix}{this._currentCommand.Parent?.Name ?? string.Empty}");

            foreach (string alias in this._currentCommand.Aliases) commandNameWithAliases.Append($"|{alias}");

            usageBuilder.Append($"{commandNameWithAliases} ");

            foreach (CommandArgument argument in overload.Arguments)
            {
                StringBuilder argName = new StringBuilder(argument.Name);

                if (argument.IsOptional)
                    usageBuilder.Append($"[{argument.Name}] ");
                else if (argument.IsCatchAll)
                    usageBuilder.Append($"<{argument.Name}...> ");
                else if (argument.IsOptional && argument.IsCatchAll)
                    usageBuilder.Append($"[{argument.Name}...] ");
                else
                    usageBuilder.Append($"<{argument.Name}> ");

                argName.AppendLine();
                argName.AppendLine("\t" + "Description: " + argument.Description ?? "No description provided")
                    .AppendLine("\t" + "Type: " +
                                this._currentCommandContext.CommandsNext.GetUserFriendlyTypeName(argument.Type))
                    .AppendLine("\t" + "Default value: " + (argument.DefaultValue ?? "None"));

                argsBuilder.Append(argName + "\n");
            }

            usageBuilder.AppendLine();

            this._helpEmbedBuilder
                .AddField($"Usage ({overloadCount})", Formatter.BlockCode(usageBuilder.ToString()))
                .AddField($"Arguments ({overloadCount})",
                    Formatter.BlockCode(string.IsNullOrWhiteSpace(argsBuilder.ToString())
                        ? "Wow, such empty"
                        : argsBuilder.ToString()));

            ++overloadCount;
        }

        return this;
    }

    public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
    {
        List<(string, string, string)> subgroups = new List<(string, string, string)>();

        foreach (Command subcommand in subcommands)
        {
            string description;

            if (subcommand is CommandGroup)
                description =
                    $"Type `{this._currentCommandContext.Prefix}help {subcommand.Name}` for more commands";
            else
                description = subcommand.Description ?? "No description provided";

            subgroups.Add((subcommand.Module.ModuleType.Name.Replace("Module", ""), subcommand.Name, description));
        }

        foreach (var sg in subgroups.Select(x => x.Item1).Distinct())
        {
            this._helpEmbedBuilder.AddField(this._currentCommand != null ? $"{sg} (subcommands)" : $"{sg}",
                string.Join(Environment.NewLine,
                    subgroups.Where(x => x.Item1 == sg)
                        .Select(x => Formatter.InlineCode(x.Item2) + " - " + x.Item3)));
        }

        return this;
    }

    public override CommandHelpMessage Build()
    {
        return new CommandHelpMessage(embed: this._helpEmbedBuilder.Build());
    }
}
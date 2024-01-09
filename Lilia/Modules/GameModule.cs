using System;
using System.Threading.Tasks;
using Discord.Interactions;

namespace Lilia.Modules;

public class GameModule : InteractionModuleBase<ShardedInteractionContext>
{
	[SlashCommand("flip", "Flip the coin")]
	public async Task GameCoinflipCommand()
	{
		await Context.Interaction.DeferAsync();

		var t = Random.Shared.Next(0, 1);
		var r = new[] { "Head", "Tail" };

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = $"The coin turned {r[t]}");
	}

	[SlashCommand("pick", "Pick a choice from given pool of choices")]
	public async Task GamePickCommand(
		[Summary("choices", "List of choices, separated by '|'")]
		string pool)
	{
		await Context.Interaction.DeferAsync();

		var choices = pool.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		var index = Random.Shared.Next(0, choices.Length - 1);

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = $"I would pick: {choices[index]}");
	}
}

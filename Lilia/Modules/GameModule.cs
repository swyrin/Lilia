using System;
using System.Threading.Tasks;
using Discord.Interactions;

namespace Lilia.Modules;

public class GameModule : InteractionModuleBase<ShardedInteractionContext>
{
	[SlashCommand("coinflip", "Flip the coin")]
	public async Task GameCoinflipCommand()
	{
		await Context.Interaction.DeferAsync();

		var t = Random.Shared.Next(0, 1);
		var r = new[] {"Head", "Tail"};

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = $"The coin turned {r[t]}");
	}
}

using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lilia.Commons;
using Lilia.Modules.Utils;

namespace Lilia.Modules;

public class ActivityModule : InteractionModuleBase<ShardedInteractionContext>
{
	[SlashCommand("activity", "Create a new activity")]
	public async Task ActivityActivityCommand(
		[Summary("target", "The target activity")]
		DefaultApplications applications = DefaultApplications.Youtube)
	{
		await Context.Interaction.DeferAsync();

		if (applications.ToString().EndsWith("Dev"))
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "You are not allowed to use this"
			);
		}

		await new MusicModuleUtils(Context.Interaction, null).EnsureUserInVoiceAsync();

		var invite = await ((SocketGuildUser)Context.User).VoiceState!.Value.VoiceChannel.CreateInviteToApplicationAsync(applications, maxUses: null,
			isTemporary: true, isUnique: true);
		await Context.Interaction.ModifyOriginalResponseAsync(x =>
		{
			x.Embed = Context.User.CreateEmbedWithUserData()
				.WithAuthor("Activity invite created", Context.Client.CurrentUser.GetAvatarUrl())
				.WithDescription($"Invite to {applications} in {Format.Bold(invite.Channel.Name)}: {invite.Url}")
				.Build();
		});
	}
}

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive.Pagination;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Interactors;
using Lilia.Modules.Utils;
using Lilia.Services;

namespace Lilia.Modules;

[Group("mod", "Moderation commands")]
public class ModerationModule : InteractionModuleBase<ShardedInteractionContext>
{
	[Group("general", "General command for moderating members")]
	public class ModerationGeneralModule : InteractionModuleBase<ShardedInteractionContext>
	{
		private const string MuteRoleName = "Lilia-mute";
		private readonly LiliaClient _client;
		private readonly LiliaDatabaseContext _dbContext;

		public ModerationGeneralModule(LiliaClient client, LiliaDatabase database)
		{
			_dbContext = database.GetContext();
			_client = client;
		}

		[SlashCommand("ban", "Ban members in batch")]
		[RequireUserPermission(GuildPermission.BanMembers)]
		[RequireBotPermission(GuildPermission.BanMembers)]
		public async Task ModerationGeneralBanCommand(
			[Summary("reason", "Reason to execute")]
			string reason = "Rule violation")
		{
			await Context.Interaction.DeferAsync();

			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = $"{Format.Bold("Mention")} all the users you want to ban with reason {Format.Bold(reason)}");

			var mentionedUsers = await ModerationModuleUtils.GetMentionedUsersAsync(Context, _client.InteractiveService);

			StringBuilder stringBuilder = new();

			foreach (var discordUser in mentionedUsers)
			{
				var mentionedMember = Context.Guild.GetUser(discordUser.Id);

				if (mentionedMember == Context.User || mentionedMember.Id == Context.Client.CurrentUser.Id)
				{
					stringBuilder.AppendLine("Skipped because it is either you or me");
					continue;
				}

				var execLine = $"Banning {Format.Bold(Format.UsernameAndDiscriminator(mentionedMember))}";
				stringBuilder.AppendLine(execLine);

				try
				{
					await mentionedMember.BanAsync(0, reason);
				}
				catch
				{
					stringBuilder.AppendLine($"Missing permission to ban {Format.Bold(Format.UsernameAndDiscriminator(mentionedMember))}");
				}

				var now = DateTime.Now;

				var embedBuilder = new EmbedBuilder()
					.WithAuthor(null, Context.Client.CurrentUser.GetAvatarUrl())
					.WithTitle($"You have been banned from guild \"{Context.Guild.Name}\" (ID: {Context.Guild.Id})")
					.WithThumbnailUrl(Context.Guild.IconUrl)
					.AddField("Reason", reason, true)
					.AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator} (ID: {Context.User.Id})", true)
					.AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
					.AddField("What to do now?",
						$"If you believe this was a mistake, you can try sending an appeal using {Format.Code("/mod ticket appeal")} with provided IDs");

				if (!mentionedMember.IsBot)
					await mentionedMember.SendMessageAsync(embed: embedBuilder.Build());
			}

			var pages = LiliaUtilities.CreatePagesFromString($"{stringBuilder}");

			var paginator = new StaticPaginatorBuilder()
				.AddUser(Context.User)
				.WithPages(pages)
				.Build();

			await _client.InteractiveService.SendPaginatorAsync(paginator, Context.Interaction,
				responseType: InteractionResponseType.DeferredChannelMessageWithSource);
		}

		[SlashCommand("kick", "Kick members in batch")]
		[RequireUserPermission(GuildPermission.KickMembers)]
		[RequireBotPermission(GuildPermission.KickMembers)]
		public async Task ModerationGeneralKickCommand(
			[Summary("reason", "Reason to execute")]
			string reason = "Rule violation")
		{
			await Context.Interaction.DeferAsync();

			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = $"{Format.Bold("Mention")} all the users you want to kick with reason {Format.Bold(reason)}");

			var mentionedUsers = await ModerationModuleUtils.GetMentionedUsersAsync(Context, _client.InteractiveService);

			StringBuilder stringBuilder = new();

			foreach (var discordUser in mentionedUsers)
			{
				var mentionedMember = Context.Guild.GetUser(discordUser.Id);

				if (mentionedMember == Context.User || mentionedMember.Id == Context.Client.CurrentUser.Id)
				{
					stringBuilder.AppendLine("Skipped because it is either you or me");
					continue;
				}

				var execLine = $"Kicking {Format.Bold(Format.UsernameAndDiscriminator(mentionedMember))}";
				stringBuilder.AppendLine(execLine);

				try
				{
					await mentionedMember.KickAsync(reason);
				}
				catch
				{
					stringBuilder.AppendLine($"Missing permission to kick {Format.Bold(Format.UsernameAndDiscriminator(mentionedMember))}");
				}

				var now = DateTime.Now;

				var embedBuilder = new EmbedBuilder()
					.WithAuthor(null, Context.Client.CurrentUser.GetAvatarUrl())
					.WithTitle($"You have been kicked from guild \"{Context.Guild.Name}\" (ID: {Context.Guild.Id})")
					.WithThumbnailUrl(Context.Guild.IconUrl)
					.AddField("Reason", reason, true)
					.AddField("Moderator", $"{Format.UsernameAndDiscriminator(Context.User)} (ID: {Context.User.Id})", true)
					.AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
					.AddField("What to do now?",
						$"If you believe this was a mistake, you can try sending an appeal using {Format.Code("/mod ticket appeal")} with provided IDs");

				if (!mentionedMember.IsBot)
					await mentionedMember.SendMessageAsync(embed: embedBuilder.Build());
			}

			var pages = LiliaUtilities.CreatePagesFromString($"{stringBuilder}");

			var paginator = new StaticPaginatorBuilder()
				.AddUser(Context.User)
				.WithPages(pages)
				.Build();

			await _client.InteractiveService.SendPaginatorAsync(paginator, Context.Interaction,
				responseType: InteractionResponseType.DeferredChannelMessageWithSource);
		}

		[SlashCommand("warn_add", "Add a warn to an user")]
		public async Task ModerationGeneralWarnAddCommand(
			[Summary("user", "The user to add a warn")]
			SocketGuildUser user,
			[Summary("reason", "Reason to execute")]
			string reason = "Rule violation")
		{
			await Context.Interaction.DeferAsync();

			if (user == Context.User || user.Id == Context.Client.CurrentUser.Id)
			{
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "Skipped because it is either you or me");

				return;
			}

			var dbUser = _dbContext.GetUserRecord(user);

			if (dbUser.WarnCount == 3)
			{
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = $"{user.Mention} has been warned and muted earlier");

				return;
			}

			dbUser.WarnCount += 1;

			var (isExistedInPast, discordRole) =
				await ModerationModuleUtils.GetOrCreateRoleAsync(Context, MuteRoleName, GuildPermissions.None, Color.Default);

			if (!isExistedInPast)
				foreach (var channel in Context.Guild.Channels)
					await channel.AddPermissionOverwriteAsync(discordRole,
						new OverwritePermissions(sendMessages: PermValue.Deny, sendMessagesInThreads: PermValue.Deny));

			if (dbUser.WarnCount == 3) await user.AddRoleAsync(discordRole);

			var now = DateTime.Now;

			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = $"Added a warn to {user.Mention}\nNow they have {dbUser.WarnCount} warn(s)");

			var embedBuilder = new EmbedBuilder()
				.WithAuthor(null, Context.Client.CurrentUser.GetAvatarUrl())
				.WithTitle($"You were warned in guild \"{Context.Guild.Name}\" (ID: {Context.Guild.Id})")
				.WithThumbnailUrl(Context.Guild.IconUrl)
				.AddField("Reason", reason, true)
				.AddField("Moderator", $"{Format.UsernameAndDiscriminator(Context.User)} (ID: {Context.User.Id})", true)
				.AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
				.AddField("What to do now?",
					$"If you believe this was a mistake, you can try sending an appeal using {Format.Code("/mod ticket appeal")} with provided IDs");

			if (!user.IsBot)
				await user.SendMessageAsync(embed: embedBuilder.Build());

			await _dbContext.SaveChangesAsync();
		}

		[SlashCommand("warn_remove", "Remove a warn from an user")]
		public async Task ModerationGeneralWarnRemoveCommand(
			[Summary("user", "The user to remove a warn")]
			SocketGuildUser user,
			[Summary("reason", "Reason to execute")]
			string reason = "False warn")
		{
			await Context.Interaction.DeferAsync();

			if (user == Context.User || user.Id == Context.Client.CurrentUser.Id)
			{
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "Skipped because it is either you or me");

				return;
			}

			var dbUser = _dbContext.GetUserRecord(user);

			switch (dbUser.WarnCount)
			{
				case 3:
				{
					var (isExistedInPast, discordRole) =
						await ModerationModuleUtils.GetOrCreateRoleAsync(Context, MuteRoleName, GuildPermissions.None, Color.Default);

					if (!isExistedInPast)
						foreach (var channel in Context.Guild.Channels)
							await channel.AddPermissionOverwriteAsync(discordRole,
								new OverwritePermissions(sendMessages: PermValue.Deny, sendMessagesInThreads: PermValue.Deny));

					await user.RemoveRoleAsync(discordRole);
					break;
				}
				case 0:
					await Context.Interaction.ModifyOriginalResponseAsync(x =>
						x.Content = "No warn to remove");

					return;
			}

			dbUser.WarnCount -= 1;

			var now = DateTime.Now;

			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = $"Removed a warn of {user.Mention}\nNow they have {dbUser.WarnCount} warn(s)");

			var embedBuilder = new EmbedBuilder()
				.WithAuthor(null, Context.Client.CurrentUser.GetAvatarUrl())
				.WithTitle($"You have been removed a warn in guild \"{Context.Guild.Name}\" (ID: {Context.Guild.Id})")
				.WithThumbnailUrl(Context.Guild.IconUrl)
				.AddField("Reason", reason, true)
				.AddField("Moderator", $"{Format.UsernameAndDiscriminator(Context.User)} (ID: {Context.User.Id})", true)
				.AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
				.AddField("What to do now?", "Nothing");

			if (!user.IsBot)
				await user.SendMessageAsync(embed: embedBuilder.Build());

			await _dbContext.SaveChangesAsync();
		}

		[SlashCommand("mute", "Mute an user, like Timeout but infinite duration")]
		public async Task ModerationGeneralMuteCommand(
			[Summary("user", "The user to mute")] SocketGuildUser user,
			[Summary("reason", "The reason")] string reason = "Excessive rule violation")
		{
			await Context.Interaction.DeferAsync();

			if (user == Context.User || user.Id == Context.Client.CurrentUser.Id)
			{
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "Skipped because it is either you or me");

				return;
			}

			var (isExistedInPast, discordRole) =
				await ModerationModuleUtils.GetOrCreateRoleAsync(Context, MuteRoleName, GuildPermissions.None, Color.Default);

			if (!isExistedInPast)
				foreach (var channel in Context.Guild.Channels)
					await channel.AddPermissionOverwriteAsync(discordRole,
						new OverwritePermissions(sendMessages: PermValue.Deny, sendMessagesInThreads: PermValue.Deny));

			// no equivalent like Has?
			if (user.Roles.Count(x => x == discordRole) == 1)
			{
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "Already muted");

				return;
			}

			await user.AddRoleAsync(discordRole);

			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = $"Muted {user.Mention} because of reason: {Format.Bold(reason)}");

			var now = DateTime.Now;
			var embedBuilder = new EmbedBuilder()
				.WithAuthor(null, Context.Client.CurrentUser.GetAvatarUrl())
				.WithTitle($"You have been muted in guild \"{Context.Guild.Name}\" (ID: {Context.Guild.Id})")
				.WithThumbnailUrl(Context.Guild.IconUrl)
				.AddField("Reason", reason, true)
				.AddField("Moderator", $"{Format.UsernameAndDiscriminator(Context.User)} (ID: {Context.User.Id})", true)
				.AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
				.AddField("What to do now?",
					$"If you believe this was a mistake, you can try sending an appeal using {Format.Code("/mod ticket appeal")} with provided IDs");

			if (!user.IsBot)
				await user.SendMessageAsync(embed: embedBuilder.Build());
		}

		[SlashCommand("unmute", "Unmute an user, like Remove Timeout")]
		public async Task ModerationGeneralUnmuteCommand(
			[Summary("user", "The user to mute")] SocketGuildUser user,
			[Summary("reason", "The reason")] string reason = "Good behavior")
		{
			await Context.Interaction.DeferAsync();

			if (user == Context.User || user.Id == Context.Client.CurrentUser.Id)
			{
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "Skipped because it is either you or me");

				return;
			}

			var (isExistedInPast, discordRole) =
				await ModerationModuleUtils.GetOrCreateRoleAsync(Context, MuteRoleName, GuildPermissions.None, Color.Default);

			if (!isExistedInPast)
				foreach (var channel in Context.Guild.Channels)
					await channel.AddPermissionOverwriteAsync(discordRole,
						new OverwritePermissions(sendMessages: PermValue.Deny, sendMessagesInThreads: PermValue.Deny));

			// no equivalent like Has?
			if (user.Roles.Any(x => x == discordRole))
			{
				await Context.Interaction.ModifyOriginalResponseAsync(x =>
					x.Content = "Already unmuted");

				return;
			}

			await user.RemoveRoleAsync(discordRole);

			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = $"Unmuted {user.Mention} because of reason: {Format.Bold(reason)}");

			var now = DateTime.Now;
			var embedBuilder = new EmbedBuilder()
				.WithAuthor(null, Context.Client.CurrentUser.GetAvatarUrl())
				.WithTitle($"You have been unmuted in guild \"{Context.Guild.Name}\" (ID: {Context.Guild.Id})")
				.WithThumbnailUrl(Context.Guild.IconUrl)
				.AddField("Reason", reason, true)
				.AddField("Moderator", $"{Format.UsernameAndDiscriminator(Context.User)} (ID: {Context.User.Id})",
					true)
				.AddField("Execution time", $"{now.ToLongDateString()}, {now.ToLongTimeString()}")
				.AddField("What to do now?", "Nothing");

			if (!user.IsBot)
				await user.SendMessageAsync(embed: embedBuilder.Build());
		}
	}

	[Group("ticket", "Commands for sending tickets to moderator")]
	public class ModerationMessageModule : InteractionModuleBase<SocketInteractionContext>
	{
		[SlashCommand("appeal", "Send an appeal")]
		[RequireContext(ContextType.DM)]
		public async Task ModerationModuleMessageAppealCommand()
		{
			var modalBuilder = new ModalBuilder();

			modalBuilder
				.WithTitle("Execution appeal")
				.WithCustomId("appeal-modal")
				.AddTextInput("Guild ID", "guild-id", placeholder: "The guild ID where you get executed")
				.AddTextInput("Executor ID", "receiver-id", placeholder: "The moderator ID who executed you")
				.AddTextInput("Your appeal", "appeal-text", TextInputStyle.Paragraph, "Your appeal");

			await Context.Interaction.RespondWithModalAsync(modalBuilder.Build());
			var res = (SocketModal)await InteractionUtility.WaitForInteractionAsync(Context.Client, TimeSpan.FromMinutes(10),
				interaction => interaction.Type == InteractionType.ModalSubmit);

			var components = res.Data.Components.ToList();
			var guildId = components.First(x => x.CustomId == "guild-id").Value;
			var receiverId = components.First(x => x.CustomId == "receiver-id").Value;
			var appealText = components.First(x => x.CustomId == "appeal-text").Value;

			try
			{
				var guildIdLong = Convert.ToUInt64(guildId);
				var receiverIdLong = Convert.ToUInt64(receiverId);

				var guild = Context.Client.GetGuild(guildIdLong);
				await Context.Client.DownloadUsersAsync(new[] {guild});

				if (guild == null)
				{
					await res.RespondAsync("I am not in the guild you specified");
					return;
				}

				var receiver = guild.GetUser(receiverIdLong);

				if (receiver == null)
				{
					await res.RespondAsync("The user you entered does not belong to the guild");
					return;
				}

				if (receiver.Id == Context.User.Id || receiver.Id == Context.Client.CurrentUser.Id)
				{
					await res.RespondAsync("You can not send to either yourself or me");
					return;
				}

				if (receiver.IsBot)
				{
					await res.RespondAsync("I can not send messages to a bot, please specify a human user");
					return;
				}

				await receiver.SendMessageAsync(embed: Context.User.CreateEmbedWithUserData()
					.WithTitle("An appeal has been sent to you")
					.WithDescription(appealText)
					.WithThumbnailUrl(guild.IconUrl)
					.WithAuthor(Format.UsernameAndDiscriminator(Context.User), Context.User.GetAvatarUrl())
					.AddField("Sender", $"{Format.UsernameAndDiscriminator(Context.User)} (ID: {Context.User.Id})")
					.AddField("Guild to appeal", $"{guild.Name} (ID: {guild.Id}")
					.Build());

				await res.RespondAsync("Sent the appeal");
			}
			catch
			{
				await res.RespondAsync("Looks like you provided invalid IDs");
			}
		}
	}
}

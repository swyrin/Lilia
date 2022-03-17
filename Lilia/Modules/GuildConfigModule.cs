using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lilia.Commons;
using Lilia.Database;
using Lilia.Database.Interactors;
using Lilia.Modules.Utils;
using Lilia.Services;

namespace Lilia.Modules;

[Group("config", "Server configuration")]
public class GuildConfigModule : InteractionModuleBase<ShardedInteractionContext>
{
	private readonly LiliaDatabaseContext _dbCtx;

	public GuildConfigModule(LiliaDatabase database)
	{
		_dbCtx = database.GetContext();
	}

	[SlashCommand("welcome_channel", "Set the welcome channel")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	public async Task ConfigWelcomeChannelCommand(
		[Summary("channel", "Channel to dump all welcome messages")]
		[ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Store, ChannelType.NewsThread, ChannelType.PublicThread)]
		SocketChannel channel)
	{
		await Context.Interaction.DeferAsync(true);

		var dbGuild = _dbCtx.GetGuildRecord(Context.Guild);

		if (string.IsNullOrWhiteSpace(dbGuild.WelcomeMessage))
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "You did not set a welcome message in this guild");

			return;
		}

		dbGuild.WelcomeChannelId = channel.Id;
		await _dbCtx.SaveChangesAsync();

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = $"Set the welcome channel of this guild to {MentionUtils.MentionChannel(channel.Id)}");
	}

	[SlashCommand("goodbye_channel", "Set the goodbye channel")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	public async Task ConfigGoodbyeChannelCommand(
		[Summary("channel", "Channel to dump all goodbye messages")]
		[ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Store, ChannelType.NewsThread, ChannelType.PublicThread)]
		SocketGuildChannel channel)
	{
		await Context.Interaction.DeferAsync(true);

		var dbGuild = _dbCtx.GetGuildRecord(Context.Guild);

		if (string.IsNullOrWhiteSpace(dbGuild.GoodbyeMessage))
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "You did not set a goodbye message in this guild");

			return;
		}

		dbGuild.GoodbyeChannelId = channel.Id;
		await _dbCtx.SaveChangesAsync();

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = $"Set the goodbye channel of this guild to {MentionUtils.MentionChannel(channel.Id)}");
	}

	[SlashCommand("goodbye_message", "Set the goodbye message")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	public async Task ConfigGoodbyeMessageCommand(
		[Summary("message", "Goodbye message, see \"/config placeholders\" for placeholders")]
		string message)
	{
		await Context.Interaction.DeferAsync(true);

		var dbGuild = _dbCtx.GetGuildRecord(Context.Guild);

		if (!GuildConfigModuleUtils.IsChannelExist(Context, dbGuild.GoodbyeChannelId))
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "You did not set a goodbye channel in this guild");

			return;
		}

		dbGuild.GoodbyeMessage = message;
		await _dbCtx.SaveChangesAsync();

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = $"Set the goodbye message of this guild: {Format.Code(message)}");
	}

	[SlashCommand("welcome_message", "Set the welcome message")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	public async Task ConfigWelcomeMessageCommand(
		[Summary("message", "Welcome message, see \"/config placeholders\" for placeholders")]
		string message)
	{
		await Context.Interaction.DeferAsync(true);

		var dbGuild = _dbCtx.GetGuildRecord(Context.Guild);

		if (!GuildConfigModuleUtils.IsChannelExist(Context, dbGuild.WelcomeChannelId))
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "You did not set a welcome channel in this guild");

			return;
		}

		dbGuild.WelcomeMessage = message;
		await _dbCtx.SaveChangesAsync();

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = $"Set the goodbye message of this guild: {Format.Code(message)}");
	}

	[SlashCommand("toggle_welcome", "Toggle welcome message allowance in this guild")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	public async Task ConfigToggleWelcomeCommand()
	{
		await Context.Interaction.DeferAsync(true);

		var dbGuild = _dbCtx.GetGuildRecord(Context.Guild);

		if (!GuildConfigModuleUtils.IsChannelExist(Context, dbGuild.WelcomeChannelId))
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "You did not set a welcome channel in this guild");

			return;
		}

		if (string.IsNullOrWhiteSpace(dbGuild.WelcomeMessage))
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "You did not set a welcome message in this guild");

			return;
		}

		dbGuild.IsWelcomeEnabled = !dbGuild.IsWelcomeEnabled;
		await _dbCtx.SaveChangesAsync();

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = $"{(dbGuild.IsWelcomeEnabled ? "Allowed" : "Blocked")} the delivery of welcome message in this guild");
	}

	[SlashCommand("toggle_goodbye", "Toggle goodbye message allowance in this guild")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	public async Task ConfigToggleGoodbyeCommand()
	{
		await Context.Interaction.DeferAsync(true);

		var dbGuild = _dbCtx.GetGuildRecord(Context.Guild);

		if (!GuildConfigModuleUtils.IsChannelExist(Context, dbGuild.WelcomeChannelId))
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "You did not set a goodbye channel in this guild");

			return;
		}

		if (string.IsNullOrWhiteSpace(dbGuild.GoodbyeMessage))
		{
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
				x.Content = "You did not set a goodbye message in this guild");

			return;
		}

		dbGuild.IsGoodbyeEnabled = !dbGuild.IsGoodbyeEnabled;
		await _dbCtx.SaveChangesAsync();

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Content = $"{(dbGuild.IsGoodbyeEnabled ? "Allowed" : "Blocked")} the delivery of goodbye message in this guild");
	}

	[SlashCommand("placeholders", "Get all available configuration placeholders")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	public async Task ConfigPlaceholdersCommand()
	{
		await Context.Interaction.DeferAsync(true);

		var embedBuilder = Context.User.CreateEmbedWithUserData()
			.WithAuthor("Available placeholders", null, Context.Client.CurrentUser.GetAvatarUrl())
			.AddField("{name} - The user's username", "Example: Swyrin#7193 -> {name} = Swyrin\n" +
			                                          "Restrictions: None")
			.AddField("{tag} - The user's discriminator", "Example: Swyrin#7193 -> {tag} = 7193\n" +
			                                              "Restrictions: None")
			.AddField("{guild} - The guild's name", $"Example: {{guild}} = {Context.Guild.Name}\n" +
			                                        "Restrictions: None")
			.AddField("{@user} - User mention", $"Example: Swyrin#7193 -> {{@user}} = {Context.User.Mention}\n" +
			                                    "Restrictions: Welcome message only");

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Embed = embedBuilder.Build());
	}

	[SlashCommand("view", "View the configurations of this guild")]
	public async Task ConfigViewCommand()
	{
		await Context.Interaction.DeferAsync(true);

		var dbGuild = _dbCtx.GetGuildRecord(Context.Guild);

		var welcomeChnMention = GuildConfigModuleUtils.IsChannelExist(Context, dbGuild.WelcomeChannelId)
			? MentionUtils.MentionChannel(dbGuild.WelcomeChannelId)
			: "Channel not exist or not set";

		var goodbyeChnMention = GuildConfigModuleUtils.IsChannelExist(Context, dbGuild.GoodbyeChannelId)
			? MentionUtils.MentionChannel(dbGuild.GoodbyeChannelId)
			: "Channel not exist or not set";

		var embedBuilder = Context.User.CreateEmbedWithUserData()
			.WithAuthor("All configurations", null, Context.Client.CurrentUser.GetAvatarUrl())
			.AddField("Welcome message", string.IsNullOrWhiteSpace(dbGuild.WelcomeMessage) ? "None" : dbGuild.WelcomeMessage, true)
			.AddField("Welcome channel", welcomeChnMention, true)
			.AddField("Welcome message allowed", $"{dbGuild.IsWelcomeEnabled}", true)
			.AddField("Goodbye message", string.IsNullOrWhiteSpace(dbGuild.GoodbyeMessage) ? "None" : dbGuild.GoodbyeMessage, true)
			.AddField("Goodbye channel", goodbyeChnMention, true)
			.AddField("Goodbye message allowed", $"{dbGuild.IsGoodbyeEnabled}", true);

		await Context.Interaction.ModifyOriginalResponseAsync(x =>
			x.Embed = embedBuilder.Build());
	}
}

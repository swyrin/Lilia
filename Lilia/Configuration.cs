using DSharpPlus.Entities;

namespace Lilia
{
    /// <summary>
    ///     The one to rule every configuration.
    /// </summary>
    public sealed class Configuration
    {
        /// <summary>
        /// Permissions required to make this bot run *properly*.
        /// </summary>
        public const DiscordPermissions RequiredPermissions =
            DiscordPermissions.ManageRoles |
            DiscordPermissions.ManageChannels |
            DiscordPermissions.AccessChannels |
            DiscordPermissions.SendMessages |
            DiscordPermissions.SendMessagesInThreads |
            DiscordPermissions.EmbedLinks |
            DiscordPermissions.ReadMessageHistory |
            DiscordPermissions.UseExternalEmojis |
            DiscordPermissions.UseExternalStickers |
            DiscordPermissions.AddReactions |
            DiscordPermissions.Speak |
            DiscordPermissions.UseVoice |
            DiscordPermissions.AttachFiles |
            DiscordPermissions.ManageMessages;

        /// <summary>
        /// Configurations that are related to setting up the bot.
        /// The values are serialized from the config.json file btw.
        /// </summary>
        public ClientConfiguration Client = new();
    }

    /// <summary>
    ///     <inheritdoc cref="Configuration.Client" />
    /// </summary>
    public sealed class ClientConfiguration
    {
        /// <summary>
        ///     Database password.
        /// </summary>
        public string DatabasePassword { get; set; }

        /// <summary>
        ///     Bot token.
        /// </summary>
        public string Token { get; set; }
    }
}

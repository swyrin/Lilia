using System.Collections.Generic;
using Newtonsoft.Json;

namespace Helya.Json;

public class BotConfiguration : BaseJson
{
    [JsonProperty("client")]
    public ClientConfigurations Client = new();

    [JsonProperty("credentials")]
    public CredentialConfigurations Credentials = new();

    public BotConfiguration()
    {
        FilePath = "config.json";
    }
}

public class ClientConfigurations
{
    [JsonProperty("activity")]
    public ClientActivityConfigurations Activity = new();

    [JsonProperty("private_guilds")]
    public List<ulong> PrivateGuildIds = new();

    [JsonProperty("slash_commands_for_global")]
    public bool SlashCommandsForGlobal = true;

    [JsonProperty("support_guild_invite_link")]
    public string SupportGuildInviteLink = "";
}

public class ClientActivityConfigurations
{
    [JsonProperty("name")]
    public string Name = "you";

    [JsonProperty("status")]
    public string Status = "DoNotDisturb";

    [JsonProperty("type")]
    public string Type = "Watching";
}

public class CredentialConfigurations
{
    [JsonProperty("db_password")]
    public string DbPassword = "thisisliterallynotapassword";

    [JsonProperty("discord_token")]
    public string DiscordToken;

    [JsonProperty("osu")]
    public OsuConfigurations Osu = new();
}

public class OsuConfigurations
{
    [JsonProperty("client_id")]
    public long ClientId;

    [JsonProperty("client_secret")]
    public string ClientSecret;
}
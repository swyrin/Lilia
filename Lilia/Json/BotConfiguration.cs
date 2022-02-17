using Newtonsoft.Json;
using System.Collections.Generic;

namespace Lilia.Json;

public class BotConfiguration : BaseJson
{
    [JsonProperty("client")]
    public ClientConfigurations Client = new();

    [JsonProperty("credentials")]
    public CredentialConfigurations Credentials = new();

    public BotConfiguration()
    {
#if DEBUG
        FilePath = "test-config.json";
#else
        FilePath = "config.json";
#endif
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
    [JsonProperty("postgresql")]
    public PostgreSqlConfigurations PostgreSql = new();

    [JsonProperty("discord_token")]
    public string DiscordToken;

    [JsonProperty("osu")]
    public OsuConfigurations Osu = new();
}

public class PostgreSqlConfigurations
{
    [JsonProperty("host")]
    public string Host = "localhost";

    [JsonProperty("port")]
    public int Port = 5432;

    [JsonProperty("username")]
    public string Username = "Lilia";

    [JsonProperty("password")]
    public string Password = "thisisnottheend";
    
    [JsonProperty("database_name")]
    public string DatabaseName = "lilia";
}

public class OsuConfigurations
{
    [JsonProperty("client_id")]
    public long ClientId;

    [JsonProperty("client_secret")]
    public string ClientSecret;
}
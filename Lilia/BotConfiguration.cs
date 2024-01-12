using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lilia;

public class BotConfiguration
{
    [JsonPropertyName("client")] public ClientConfigurations Client = new();

    [JsonPropertyName("credentials")] public CredentialConfigurations Credentials = new();
}

public class ClientConfigurations
{
    [JsonPropertyName("activity")] public ClientActivityConfigurations Activity = new();

    [JsonPropertyName("private_guilds")] public List<ulong> PrivateGuildIds = new();

    [JsonPropertyName("shard_count")] public int ShardCount = 1;

    [JsonPropertyName("slash_commands_for_global")]
    public bool SlashCommandsForGlobal = true;

    [JsonPropertyName("support_guild_invite_link")]
    public string SupportGuildInviteLink = "";

    [JsonPropertyName("token")] public string Token = "";
}

public class ClientActivityConfigurations
{
    [JsonPropertyName("name")] public string Name = "you";

    [JsonPropertyName("status")] public string Status = "DoNotDisturb";

    [JsonPropertyName("type")] public string Type = "Watching";
}

public class CredentialConfigurations
{
    [JsonPropertyName("lavalink_nodes")] public List<LavalinkNodeConfigurations> LavalinkNodes = new();

    [JsonPropertyName("osu")] public OsuConfigurations Osu = new();

    [JsonPropertyName("postgresql")] public PostgreSqlConfigurations PostgreSql = new();

    [JsonPropertyName("top.gg")] public string TopDotGeeGeeToken;
}

public class LavalinkNodeConfigurations
{
    [JsonPropertyName("host")] public string Host = "swyrin.me";

    [JsonPropertyName("is_secure")] public bool IsSecure;

    [JsonPropertyName("password")] public string Password = "youshallnotpass";

    [JsonPropertyName("port")] public int Port = 2333;
}

public class PostgreSqlConfigurations
{
    [JsonPropertyName("database_name")] public string DatabaseName;

    [JsonPropertyName("host")] public string Host = "localhost";

    [JsonPropertyName("password")] public string Password = "thisisnottheend";

    [JsonPropertyName("port")] public int Port = 5432;

    [JsonPropertyName("username")] public string Username;
}

public class OsuConfigurations
{
    [JsonPropertyName("client_id")] public long ClientId;

    [JsonPropertyName("client_secret")] public string ClientSecret;
}

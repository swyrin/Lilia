using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lilia.Json;

public class BotConfiguration : BaseJson
{
	[JsonProperty("client")] public ClientConfigurations Client = new();

	[JsonProperty("credentials")] public CredentialConfigurations Credentials = new();

	public BotConfiguration()
	{
		FilePath = "config.json";
	}
}

public class ClientConfigurations
{
	[JsonProperty("activity")] public ClientActivityConfigurations Activity = new();

	[JsonProperty("private_guilds")] public List<ulong> PrivateGuildIds = new();

	[JsonProperty("shard_count", NullValueHandling = NullValueHandling.Include)]
	public int ShardCount = 1;

	[JsonProperty("slash_commands_for_global")]
	public bool SlashCommandsForGlobal = true;

	[JsonProperty("support_guild_invite_link")]
	public string SupportGuildInviteLink = "";

	[JsonProperty("token")] public string Token = "";
}

public class ClientActivityConfigurations
{
	[JsonProperty("name")] public string Name = "you";

	[JsonProperty("status")] public string Status = "DoNotDisturb";

	[JsonProperty("type")] public string Type = "Watching";
}

public class CredentialConfigurations
{
	[JsonProperty("lavalink")] public LavalinkConfigurations Lavalink = new();

	[JsonProperty("osu")] public OsuConfigurations Osu = new();

	[JsonProperty("postgresql")] public PostgreSqlConfigurations PostgreSql = new();
}

public class LavalinkConfigurations
{
	[JsonProperty("host")] public string Host = "swyrin.me";

	[JsonProperty("password")] public string Password = "youshallnotpass";

	[JsonProperty("port")] public int Port = 2333;
}

public class PostgreSqlConfigurations
{
	[JsonProperty("database_name")] public string DatabaseName;

	[JsonProperty("host")] public string Host = "localhost";

	[JsonProperty("password")] public string Password = "thisisnottheend";

	[JsonProperty("port")] public int Port = 5432;

	[JsonProperty("username")] public string Username;
}

public class OsuConfigurations
{
	[JsonProperty("client_id")] public long ClientId;

	[JsonProperty("client_secret")] public string ClientSecret;
}

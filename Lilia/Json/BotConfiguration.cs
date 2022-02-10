using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lilia.Json;

public class BotConfiguration : BaseJson
{
    [JsonProperty("client")] public ClientData Client = new();

    [JsonProperty("credentials")] public CredentialsData Credentials = new();

    public BotConfiguration()
    {
        FilePath = "config.json";
    }
}

public class ClientData
{
    [JsonProperty("activity")] public ClientActivityData Activity = new();

    [JsonProperty("bot_invite_link")] public string BotInviteLink = "";
    [JsonProperty("private_guilds")] public List<ulong> PrivateGuildIds = new();

    [JsonProperty("slash_commands_for_global")]
    public bool SlashCommandsForGlobal = true;

    [JsonProperty("support_guild_invite_link")]
    public string SupportGuildInviteLink = "";
}

public class ClientActivityData
{
    [JsonProperty("name")] public string Name = "you";

    [JsonProperty("status")] public string Status = "DoNotDisturb";
    [JsonProperty("type")] public string Type = "Watching";
}

public class CredentialsData
{
    [JsonProperty("db_password")] public string DbPassword = "thisisliterallynotapassword";
    [JsonProperty("discord_token")] public string DiscordToken;

    [JsonProperty("osu")] public OsuData Osu = new();
}

public class OsuData
{
    [JsonProperty("client_id")] public long ClientId;

    [JsonProperty("client_secret")] public string ClientSecret;
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lilia.Commons;

public static class JsonConfigurationsManager
{
    public static readonly JsonConfigurations Configurations;
    public const string ConfigFileName = "config.json";

    static JsonConfigurationsManager()
    {
        EnsureConfigFileGenerated();
        Configurations = JsonConvert.DeserializeObject<JsonConfigurations>(File.ReadAllText(ConfigFileName));
    }

    private static void EnsureConfigFileGenerated()
    {
        if (!File.Exists(ConfigFileName))
        {
            File.Create(ConfigFileName);
            File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(new JsonConfigurations(), Formatting.Indented));
            Console.WriteLine($"Created configuration file {ConfigFileName}.");
            Console.WriteLine("Please fill it with necessary data.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}

public class JsonConfigurations
{
    [JsonProperty("client")] public ClientData Client = new();

    [JsonProperty("credentials")] public CredentialsData Credentials = new();

    [JsonProperty("lavalink")] public LavalinkData Lavalink = new();
}

public class ClientData
{
    [JsonProperty("private_guilds")] public List<ulong> PrivateGuildIds = new();
    
    [JsonProperty("activity")] public ClientActivityData Activity = new();
}

public class ClientActivityData
{
    [JsonProperty("type")] public string Type = "Watching";

    [JsonProperty("name")] public string Name = "you";

    [JsonProperty("status")] public string Status = "DoNotDisturb";
}

public class CredentialsData
{
    [JsonProperty("discord_token")] public string DiscordToken;

    [JsonProperty("db_password")] public string DbPassword = "thisisliterallynotapassword";

    [JsonProperty("osu")] public OsuData Osu = new();
}

public class OsuData
{
    [JsonProperty("client_id")] public long ClientId;
    
    [JsonProperty("client_secret")] public string ClientSecret;
}

public class LavalinkData
{
    [JsonProperty("host")] public string Hostname = "localhost";

    [JsonProperty("port")] public int Port = 2333;

    [JsonProperty("password")] public string Password = "youshallnotpass";
}
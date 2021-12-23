using Newtonsoft.Json;
using System;
using System.IO;

namespace Lilia.Commons
{
    public static class JsonConfigurationsManager
    {
        public static JsonConfigurations? Configurations { get; set; }
        public const string ConfigFileName = "config.json";

        static JsonConfigurationsManager()
        {
            Configurations = JsonConvert.DeserializeObject<JsonConfigurations>(File.ReadAllText(ConfigFileName));
        }

        public static void EnsureConfigFileGenerated()
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
        [JsonProperty("client")]
        public ClientData Client = new ClientData();

        [JsonProperty("credentials")]
        public CredentialsData Credentials = new CredentialsData();
    }

    public class ClientData
    {
        [JsonProperty("data")]
        public string[] StringPrefixes { get; set; } = new[] { "l." };

        [JsonProperty("activity")]
        public ClientActivityData Activity = new ClientActivityData();
    }

    public class ClientActivityData
    {
        [JsonProperty("type")]
        public int Type { get; set; } = 3;

        [JsonProperty("name")]
        public string Name { get; set; } = "you";

        [JsonProperty("status")]
        public int Status { get; set; } = 4;
    }

    public class CredentialsData
    {
        [JsonProperty("discord_token")]
        public string DiscordToken { get; set; }

        [JsonProperty("db_password")]
        public string DbPassword { get; set; } = "thisisliterallynotapassword";

        [JsonProperty("osu_api_key")]
        public string OsuApiKey { get; set; }
    }
}
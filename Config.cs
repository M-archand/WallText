using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace WallText
{
    public class PluginConfig : BasePluginConfig
    {
        [JsonPropertyName("UseDatabase")]
        public bool UseDatabase { get; set; } = true;
        public DatabaseSettings DatabaseSettings { get; set; } = new DatabaseSettings();

        [JsonPropertyName("WallText")]
        public Dictionary<int, List<string>> WallText { get; set; } = new Dictionary<int, List<string>>()
        {
            { 1, new List<string>
                {
                    "{White}First line of text from Group 1.",
                    "{White}Second line of text from Group 1.",
                    "{White}Third line of text from Group 1."
                }
            },
            { 2, new List<string>
                {
                    "{White}First line of text from Group 2.",
                    "{White}Second line of text from Group 2.",
                    "{White}Third line of text from Group 2."
                }
            }
        };

        [JsonPropertyName("TextAlignment")]
        public string TextAlignment { get; set; } = "left";

        [JsonPropertyName("FontSize")]
        public int FontSize { get; set; } = 24;

        [JsonPropertyName("TextScale")]
        public float TextScale { get; set; } = 0.45f;

        [JsonPropertyName("RemoveCommand")]
        public string RemoveCommand { get; set; } = "removetext";

        [JsonPropertyName("CommandPermission")]
        public string CommandPermission { get; set; } = "@css/root";

        [JsonPropertyName("ConfigVersion")]
        public override int Version { get; set; } = 3;
    }
    
    public sealed class DatabaseSettings
    {
        [JsonPropertyName("host")]
        public string Host { get; set; } = "localhost";

        [JsonPropertyName("database")]
        public string Database { get; set; } = "database";

        [JsonPropertyName("username")]
        public string Username { get; set; } = "user";

        [JsonPropertyName("password")]
        public string Password { get; set; } = "password";

        [JsonPropertyName("port")]
        public int Port { get; set; } = 3306;

        [JsonPropertyName("sslmode")]
        public string Sslmode { get; set; } = "none";

        [JsonPropertyName("table-name")]
        public string TableName { get; set; } = "Wall-Text";
    }
}
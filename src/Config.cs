using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace WorldText
{
    public class PluginConfig : BasePluginConfig
    {
        [JsonPropertyName("ConfigVersion")]
        public override int Version { get; set; } = 3;

        [JsonPropertyName("EnableDatabase")]
        public bool EnableDatabase { get; set; } = true;
        public DatabaseSettings DatabaseSettings { get; set; } = new DatabaseSettings();

        [JsonPropertyName("TextAlignment")]
        public string TextAlignment { get; set; } = "left";

        [JsonPropertyName("FontSize")]
        public int FontSize { get; set; } = 24;

        [JsonPropertyName("TextScale")]
        public float TextScale { get; set; } = 0.45f;

        [JsonPropertyName("EnableBackground")]
        public bool EnableBackground { get; set; } = true;

        [JsonPropertyName("RemoveDistance")]
        public float RemoveDistance { get; set; } = 200.0f;

        [JsonPropertyName("RemoveCommand")]
        public string RemoveCommand { get; set; } = "rtext";

        [JsonPropertyName("AddCommand")]
        public string AddCommand { get; set; } = "text";

        [JsonPropertyName("MoveCommand")]
        public string MoveCommand { get; set; } = "mtext";

        [JsonPropertyName("MenuType")]
        public string MenuType { get; set; } = "WasdMenu";

        [JsonPropertyName("CommandPermission")]
        public string CommandPermission { get; set; } = "@css/root";

        [JsonPropertyName("WorldText")]
        public Dictionary<int, WorldTextGroup> WorldText { get; set; } = new()
        {
            { 1, new WorldTextGroup {
                    BgWidth = 40f,
                    Lines = new()
                    {
                        "{White}First line of text from Group 1.",
                        "{White}Second line of text from Group 1.",
                        "{White}Third line of text from Group 1."
                    }
                }
            },
            { 2, new WorldTextGroup {
                    BgWidth = 64f,
                    Lines = new()
                    {
                        "{White}First line of text from Group 2.",
                        "{White}Second line of text from Group 2.",
                        "{White}Third line of text from Group 2."
                    }
                }
            }
        };
    }

    public sealed class WorldTextGroup
    {
        [JsonPropertyName("bgWidth")]
        public float BgWidth { get; set; } = 40f;

        [JsonPropertyName("lines")]
        public List<string> Lines { get; set; } = new();
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
        public string SslMode { get; set; } = "None";

        [JsonPropertyName("table-name")]
        public string TableName { get; set; } = "world_text";
    }
}
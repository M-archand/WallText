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

        [JsonPropertyName("MoveDistance")]
        public int MoveDistance { get; set; } = 5;

        [JsonPropertyName("CommandPermission")]
        public string CommandPermission { get; set; } = "@css/root";

        [JsonPropertyName("WorldText")]
        public Dictionary<int, WorldTextGroup> WorldText { get; set; } = new()
        {
            { 1, new WorldTextGroup {
                    BgEnable = true,
                    BgWidth = 35f,
                    TextAlignment = "left",
                    FontSize = 24,
                    TextScale = 0.45f,
                    ZOffset = 25f,
                    Lines = new()
                    {
                        "{Red}First line of text from Group 1.",
                        "{White}Second line of text from Group 1.",
                        "{Red}Third line of text from Group 1."
                    }
                }
            },
            { 2, new WorldTextGroup {
                    BgEnable = true,
                    BgWidth = 70f,
                    TextAlignment = "center",
                    FontSize = 24,
                    TextScale = 0.45f,
                    ZOffset = 0f,
                    Lines = new()
                    {
                        "{Lime}First line of text from Group 2. This is is a bit longer.",
                        "{Magenta}Second line of text from Group 2. This is is a bit longer.",
                        "{White}Third line of text from Group 2. This is is a bit longer."
                    }
                }
            }
        };
    }

    public sealed class WorldTextGroup
    {
        [JsonPropertyName("bgEnable")]
        public bool BgEnable { get; set; } = true;

        [JsonPropertyName("bgWidth")]
        public float BgWidth { get; set; } = 40f;

        [JsonPropertyName("textAlignment")]
        public string TextAlignment { get; set; } = "left";

        [JsonPropertyName("fontSize")]
        public int FontSize { get; set; } = 24;

        [JsonPropertyName("textScale")]
        public float TextScale { get; set; } = 0.45f;

        [JsonPropertyName("zOffset")]
        public float ZOffset { get; set; } = 0f;

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
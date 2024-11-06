using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace WallText
{
    public class PluginConfig : BasePluginConfig
    {
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
        public override int Version { get; set; } = 2;
    }
}
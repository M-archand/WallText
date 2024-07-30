using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using K4WorldTextSharedAPI;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Timers;

namespace WallText;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("DisplayTexts")]
    public List<string> DisplayTexts { get; set; } = new List<string>() 
	{ "{Salmon}First line of text.", "{Cyan}Second line of text.", "{MediumPurple}Third line of text." };
    [JsonPropertyName("FontSize")]
    public int FontSize { get; set; } = 24;
	[JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 3;
}

[MinimumApiVersion(205)]
public class PluginWallText : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "Wall Text";
    public override string ModuleAuthor => "Marchand";
    public override string ModuleVersion => "1.0.0";
    public required PluginConfig Config { get; set; } = new PluginConfig();
    public static PluginCapability<IK4WorldTextSharedAPI> Capability_SharedAPI { get; } = new("k4-worldtext:sharedapi");

    private List<int> _currentTexts = new();

    public void OnConfigParsed(PluginConfig config)
    {
        if (config.Version < Config.Version)
            base.Logger.LogWarning("Configuration version mismatch (Expected: {0} | Current: {1})", this.Config.Version, config.Version);

        this.Config = config;
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        AddTimer(3, LoadWorldTextFromFile);

        RegisterEventHandler((EventRoundStart @event, GameEventInfo info) =>
        {
            DisplayConfiguredText();
            return HookResult.Continue;
        });

        RegisterListener<Listeners.OnMapStart>((mapName) =>
        {
            AddTimer(1, LoadWorldTextFromFile);
        });

        RegisterListener<Listeners.OnMapEnd>(() =>
        {
            _currentTexts.Clear();
        });
    }

    public override void Unload(bool hotReload)
    {
        foreach (int messageID in _currentTexts)
        {
            Capability_SharedAPI.Get()?.RemoveWorldText(messageID);
        }
        _currentTexts.Clear();
    }

    [ConsoleCommand("css_walltext", "Sets up the wall text")]
    [RequiresPermissions("@css/root")]
    public void OnTextAdd(CCSPlayerController player, CommandInfo command)
    {
        var checkAPI = Capability_SharedAPI.Get();
        if (checkAPI is null)
        {
            command.ReplyToCommand($" {ChatColors.Silver}[ {ChatColors.Lime}Wall-Text {ChatColors.Silver}] {ChatColors.LightRed}Failed to get the shared API.");
            return;
        }

        Task.Run(() =>
        {
            var linesList = GetTextLines();

            Server.NextWorldUpdate(() =>
            {
                int messageID = checkAPI.AddWorldTextAtPlayer(player, TextPlacement.Wall, linesList);
                _currentTexts.Add(messageID);

                var lineList = checkAPI.GetWorldTextLineEntities(messageID);
                if (lineList?.Count > 0)
                {
                    var location = lineList[0]?.AbsOrigin;
                    var rotation = lineList[0]?.AbsRotation;

                    if (location != null && rotation != null)
                    {
                        SaveWorldTextToFile(location, rotation);
                    }
                    else
                    {
                        Logger.LogError("Failed to get location or rotation for message ID: {0}", messageID);
                    }
                }
                else
                {
                    Logger.LogError("Failed to get world text line entities for message ID: {0}", messageID);
                }
            });
        });
    }

    [ConsoleCommand("css_walltextrem", "Removes the closest text")]
    [RequiresPermissions("@css/root")]
    public void OnTextRemove(CCSPlayerController player, CommandInfo command)
    {
        var checkAPI = Capability_SharedAPI.Get();
        if (checkAPI is null)
        {
            command.ReplyToCommand($" {ChatColors.Silver}[ {ChatColors.Lime}Wall-Text {ChatColors.Silver}] {ChatColors.LightRed}Failed to get the shared API.");
            return;
        }

        var target = _currentTexts
            .SelectMany(id => checkAPI.GetWorldTextLineEntities(id)?.Select(entity => new { Id = id, Entity = entity }) ?? Enumerable.Empty<dynamic>())
            .Where(x => x.Entity.AbsOrigin != null && player.PlayerPawn.Value?.AbsOrigin != null && DistanceTo(x.Entity.AbsOrigin, player.PlayerPawn.Value!.AbsOrigin) < 100)
            .OrderBy(x => x.Entity.AbsOrigin != null && player.PlayerPawn.Value?.AbsOrigin != null ? DistanceTo(x.Entity.AbsOrigin, player.PlayerPawn.Value!.AbsOrigin) : float.MaxValue)
            .FirstOrDefault();

        if (target is null)
        {
            command.ReplyToCommand($" {ChatColors.Silver}[ {ChatColors.Lime}Wall-Text {ChatColors.Silver}] {ChatColors.Red}Move closer to the text that you want to remove.");
            return;
        }

        checkAPI.RemoveWorldText(target.Id);
        _currentTexts.Remove(target.Id);

        var mapName = Server.MapName;
        var path = Path.Combine(ModuleDirectory, $"{mapName}_texts.json");
        if (File.Exists(path))
        {
            var data = JsonSerializer.Deserialize<List<WorldTextData>>(File.ReadAllText(path));
            if (data != null)
            {
                data.RemoveAll(x => x.Location == target.Entity.AbsOrigin.ToString() && x.Rotation == target.Entity.AbsRotation.ToString());
                File.WriteAllText(path, JsonSerializer.Serialize(data));
            }
        }

        command.ReplyToCommand($" {ChatColors.Silver}[ {ChatColors.Lime}Wall-Text {ChatColors.Silver}] {ChatColors.Green}Text removed!");
    }

    private float DistanceTo(Vector a, Vector b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private void SaveWorldTextToFile(Vector location, QAngle rotation)
    {
        var mapName = Server.MapName;
        var path = Path.Combine(ModuleDirectory, $"{mapName}_texts.json");
        var worldTextData = new WorldTextData
        {
            Location = location.ToString(),
            Rotation = rotation.ToString()
        };

        List<WorldTextData> data;
        if (File.Exists(path))
        {
            data = JsonSerializer.Deserialize<List<WorldTextData>>(File.ReadAllText(path)) ?? new List<WorldTextData>();
        }
        else
        {
            data = new List<WorldTextData>();
        }

        data.Add(worldTextData);

        File.WriteAllText(path, JsonSerializer.Serialize(data));
    }

    private void LoadWorldTextFromFile()
    {
        var mapName = Server.MapName;
        var path = Path.Combine(ModuleDirectory, $"{mapName}_texts.json");

        if (File.Exists(path))
        {
            var data = JsonSerializer.Deserialize<List<WorldTextData>>(File.ReadAllText(path));
            if (data == null) return;

            Task.Run(() =>
            {
                var linesList = GetTextLines();

                Server.NextWorldUpdate(() =>
                {
                    var checkAPI = Capability_SharedAPI.Get();
                    if (checkAPI is null) return;

                    foreach (var worldTextData in data)
                    {
                        if (!string.IsNullOrEmpty(worldTextData.Location) && !string.IsNullOrEmpty(worldTextData.Rotation))
                        {
                            var messageID = checkAPI.AddWorldText(TextPlacement.Wall, linesList, ParseVector(worldTextData.Location), ParseQAngle(worldTextData.Rotation));
                            _currentTexts.Add(messageID);
                        }
                    }
                });
            });
        }
    }

    public static Vector ParseVector(string vectorString)
    {
        string[] components = vectorString.Split(' ');
        if (components.Length == 3 &&
            float.TryParse(components[0], out float x) &&
            float.TryParse(components[1], out float y) &&
            float.TryParse(components[2], out float z))
        {
            return new Vector(x, y, z);
        }
        throw new ArgumentException("Invalid vector string format.");
    }

    public static QAngle ParseQAngle(string qangleString)
    {
        string[] components = qangleString.Split(' ');
        if (components.Length == 3 &&
            float.TryParse(components[0], out float x) &&
            float.TryParse(components[1], out float y) &&
            float.TryParse(components[2], out float z))
        {
            return new QAngle(x, y, z);
        }
        throw new ArgumentException("Invalid QAngle string format.");
    }

    private List<TextLine> GetTextLines()
    {
        var linesList = new List<TextLine>();
		
        foreach (var text in Config.DisplayTexts)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var (color, parsedText) = ParseColorAndText(text);
				linesList.Add(new TextLine
                {
                    Text = parsedText,
                    Color = color,
                    FontSize = Config.FontSize,
                    FullBright = true,
                    Scale = 0.45f
                });
            }
        }

        return linesList;
    }

	private (Color color, string text) ParseColorAndText(string text)
    {
        var color = Color.White; // Default color
        var colorCodeMatch = System.Text.RegularExpressions.Regex.Match(text, @"^\{(\w+)\}");

        if (colorCodeMatch.Success)
        {
            var colorName = colorCodeMatch.Groups[1].Value;
            try
            {
                color = Color.FromName(colorName);
                if (!color.IsKnownColor)
                {
                    color = Color.White;
                }
            }
            catch
            {
                color = Color.White;
            }
            text = text.Substring(colorCodeMatch.Length).Trim();
        }

        return (color, text);
    }

    private void DisplayConfiguredText()
    {
        Task.Run(() =>
        {
            var linesList = GetTextLines();

            Server.NextWorldUpdate(() =>
            {
                var checkAPI = Capability_SharedAPI.Get();
                if (checkAPI != null)
                {
                    foreach (int messageID in _currentTexts)
                    {
                        checkAPI.UpdateWorldText(messageID, linesList);
                    }
                }
            });
        });
    }
}

public class WorldTextData
{
    public required string Location { get; set; }
    public required string Rotation { get; set; }
}
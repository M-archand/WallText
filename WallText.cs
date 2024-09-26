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
using System.Drawing;
using System.Text.Json;

namespace WallText
{
    [MinimumApiVersion(205)]
    public class PluginWallText : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleName => "Wall Text";
        public override string ModuleAuthor => "Marchand";
        public override string ModuleVersion => "1.0.1";
        public required PluginConfig Config { get; set; } = new PluginConfig();
        public static PluginCapability<IK4WorldTextSharedAPI> Capability_SharedAPI { get; } = new("k4-worldtext:sharedapi");
        private Dictionary<int, List<int>> _currentTextByGroup = new();
        private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public void OnConfigParsed(PluginConfig config)
        {
            if (config.Version < Config.Version)
                base.Logger.LogWarning("Configuration version mismatch (Expected: {0} | Current: {1})", this.Config.Version, config.Version);

            this.Config = config;
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            AddTimer(3, () => LoadWorldTextFromFile());

            AddCommand($"css_{Config.RemoveCommand}", "Removes the closest list, whether points or map", OnTextRemove);

            RegisterEventHandler((EventRoundStart @event, GameEventInfo info) =>
            {
                DisplayConfiguredText();
                return HookResult.Continue;
            });

            RegisterListener<Listeners.OnMapStart>((mapName) =>
            {
                AddTimer(1, () => LoadWorldTextFromFile(mapName));
            });

            RegisterListener<Listeners.OnMapEnd>(() =>
            {
                var checkAPI = Capability_SharedAPI.Get();
                if (checkAPI != null)
                foreach (var groupTextList in _currentTextByGroup.Values)
                {
                    groupTextList.ForEach(id => checkAPI.RemoveWorldText(id, false));
                }
                _currentTextByGroup.Clear();

            });
        }

        public override void Unload(bool hotReload)
        {
            var checkAPI = Capability_SharedAPI.Get();
            if (checkAPI != null)
            {
                foreach (var groupTextList in _currentTextByGroup.Values)
                {
                    groupTextList.ForEach(id => checkAPI.RemoveWorldText(id, false));
                }
            }
            _currentTextByGroup.Clear();
        }


        [ConsoleCommand("css_walltext", "Set-up the wall text locations")]
        [RequiresPermissions("@css/root")]
        public void OnTextAdd(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || info == null) return;

            string arg = info.GetArg(1);
            if (int.TryParse(arg, out var groupNumber))
            {
                CreateWallText(player, info, groupNumber);
            }
            else
            {
                CreateWallText(player, info, 1);
            }
        }

        public void CreateWallText(CCSPlayerController player, CommandInfo command, int groupNumber)
        {
            var checkAPI = Capability_SharedAPI.Get();
            if (checkAPI is null)
            {
                command.ReplyToCommand($" {ChatColors.Purple}[{ChatColors.LightPurple}Wall-Text{ChatColors.Purple}] {ChatColors.LightRed}Failed to get the shared API.");
                return;
            }

            if (!Config.WallText.ContainsKey(groupNumber))
            {
                command.ReplyToCommand($" {ChatColors.Purple}[{ChatColors.LightPurple}Wall-Text{ChatColors.Purple}] {ChatColors.Red}Group {ChatColors.White}{groupNumber} {ChatColors.Red}does not exist in the plugin config.");
                command.ReplyToCommand($"                     {ChatColors.Red}Please create it first.");
                return;
            }

            Task.Run(() =>
            {
                var linesList = GetTextLines(groupNumber);

                Server.NextWorldUpdate(() =>
                {
                    int messageID = checkAPI.AddWorldTextAtPlayer(player, TextPlacement.Wall, linesList);

                    if (!_currentTextByGroup.ContainsKey(groupNumber))
                    {
                        _currentTextByGroup[groupNumber] = new List<int>();
                    }

                    _currentTextByGroup[groupNumber].Add(messageID);

                    var lineList = checkAPI.GetWorldTextLineEntities(messageID);
                    if (lineList?.Count > 0)
                    {
                        var location = lineList[0]?.AbsOrigin;
                        var rotation = lineList[0]?.AbsRotation;

                        if (location != null && rotation != null)
                        {
                            SaveWorldTextToFile(location, rotation, groupNumber);
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

        [ConsoleCommand($"css_{DefaultCommandNames.RemoveCommand}", "Removes the closest text")]
        [RequiresPermissions($"{DefaultCommandNames.CommandPermission}")]
        public void OnTextRemove(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null || command == null) return;
            RemoveClosestText(player, command);
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                Server.NextWorldUpdate(() => RemoveClosestText(player, command));
            });
        }

        public void RemoveClosestText(CCSPlayerController player, CommandInfo command)
        {
            var checkAPI = Capability_SharedAPI.Get();
            if (checkAPI is null)
            {
                command.ReplyToCommand($" {ChatColors.Purple}[{ChatColors.LightPurple}Wall-Text{ChatColors.Purple}] {ChatColors.LightRed}Failed to get the shared API.");
                return;
            }

            dynamic? target = null;
            int groupWithTarget = -1;

            foreach (var group in _currentTextByGroup)
            {
                var groupTextList = group.Value;
                
                var closest = groupTextList
                    .SelectMany(id => checkAPI.GetWorldTextLineEntities(id)?.Select(entity => new { Id = id, Entity = entity }) ?? Enumerable.Empty<dynamic>())
                    .Where(x => x.Entity.AbsOrigin != null && player.PlayerPawn.Value?.AbsOrigin != null && DistanceTo(x.Entity.AbsOrigin, player.PlayerPawn.Value!.AbsOrigin) < 100)
                    .OrderBy(x => x.Entity.AbsOrigin != null && player.PlayerPawn.Value?.AbsOrigin != null ? DistanceTo(x.Entity.AbsOrigin, player.PlayerPawn.Value!.AbsOrigin) : float.MaxValue)
                    .FirstOrDefault();

                if (closest != null)
                {
                    target = closest;
                    groupWithTarget = group.Key;
                    break;
                }
            }

            if (target is null)
            {
                command.ReplyToCommand($" {ChatColors.Purple}[{ChatColors.LightPurple}Wall-Text{ChatColors.Purple}] {ChatColors.Red}Move closer to the text that you want to remove.");
                return;
            }

            checkAPI.RemoveWorldText(target.Id, false);
            
            if (groupWithTarget != -1)
            {
                _currentTextByGroup[groupWithTarget].Remove(target.Id);
            }

            var mapName = Server.MapName;
            var mapsDirectory = Path.Combine(ModuleDirectory, "maps");
            var path = Path.Combine(mapsDirectory, $"{mapName}_text.json");

            if (File.Exists(path))
            {
                var data = JsonSerializer.Deserialize<List<WorldTextData>>(File.ReadAllText(path));
                if (data != null)
                {
                    Vector entityVector = target.Entity.AbsOrigin;
                    data.RemoveAll(x =>
                    {
                        Vector location = ParseVector(x.Location);
                        return location.X == entityVector.X &&
                            location.Y == entityVector.Y &&
                            x.Rotation == target.Entity.AbsRotation.ToString();
                    });

                    string jsonString = JsonSerializer.Serialize(data, jsonOptions);
                    File.WriteAllText(path, jsonString);
                }
            }

            command.ReplyToCommand($" {ChatColors.Purple}[{ChatColors.LightPurple}Wall-Text{ChatColors.Purple}] {ChatColors.Green}Text removed!");
        }

        private float DistanceTo(Vector a, Vector b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private void SaveWorldTextToFile(Vector location, QAngle rotation, int groupNumber)
        {
            var mapName = Server.MapName;
            var mapsDirectory = Path.Combine(ModuleDirectory, "maps");
            var path = Path.Combine(mapsDirectory, $"{mapName}_text.json");

            var worldTextData = new WorldTextData
            {
                GroupNumber = groupNumber,
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

            File.WriteAllText(path, JsonSerializer.Serialize(data, jsonOptions));
        }

        private void LoadWorldTextFromFile(string? passedMapName = null)
        {
            var mapName = passedMapName ?? Server.MapName;
            var mapsDirectory = Path.Combine(ModuleDirectory, "maps");
            var path = Path.Combine(mapsDirectory, $"{mapName}_text.json");

            if (File.Exists(path))
            {
                var data = JsonSerializer.Deserialize<List<WorldTextData>>(File.ReadAllText(path));
                if (data == null) return;

                Task.Run(() =>
                {
                    foreach (var worldTextData in data)
                    {
                        var linesList = GetTextLines(worldTextData.GroupNumber);

                        Server.NextWorldUpdate(() =>
                        {
                            var checkAPI = Capability_SharedAPI.Get();
                            if (checkAPI != null && !string.IsNullOrEmpty(worldTextData.Location) && !string.IsNullOrEmpty(worldTextData.Rotation))
                            {
                                var messageID = checkAPI.AddWorldText(TextPlacement.Wall, linesList, ParseVector(worldTextData.Location), ParseQAngle(worldTextData.Rotation));
                                if (!_currentTextByGroup.ContainsKey(worldTextData.GroupNumber))
                                {
                                    _currentTextByGroup[worldTextData.GroupNumber] = new List<int>();
                                }
                                _currentTextByGroup[worldTextData.GroupNumber].Add(messageID);

                            }
                        });
                    }
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

        private PointWorldTextJustifyHorizontal_t GetTextAlignment()
        {
            return Config.TextAlignment.ToLower() switch
            {
                "left" => PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT,
                _ => PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER,
            };
        }

        private List<TextLine> GetTextLines(int groupNumber)
        {
            var linesList = new List<TextLine>();

            if (Config.WallText.TryGetValue(groupNumber, out var textGroup))
            {
                foreach (var text in textGroup)
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
                            Scale = Config.TextScale,
                            JustifyHorizontal = GetTextAlignment()
                        });
                    }
                }
            }
            else
            {
                Logger.LogWarning($"WallText {groupNumber} not found in config.");
            }

            return linesList;
        }

        private (Color color, string text) ParseColorAndText(string text)
        {
            var color = Color.White;
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
                foreach (var groupNumber in Config.WallText.Keys)
                {
                    var linesList = GetTextLines(groupNumber);

                    Server.NextWorldUpdate(() =>
                    {
                        var checkAPI = Capability_SharedAPI.Get();
                        if (checkAPI != null)
                        {
                            if (_currentTextByGroup.TryGetValue(groupNumber, out var messageIDs))
                            {
                                foreach (int messageID in messageIDs)
                                {
                                    checkAPI.UpdateWorldText(messageID, linesList);
                                }
                            }
                        }
                    });
                }
            });
        }
    }

    public static class DefaultCommandNames
    {
        public const string RemoveCommand = "removetext";
        public const string CommandPermission = "@css/root";
    }

    public class WorldTextData
    {
        public int GroupNumber { get; set; }
        public required string Location { get; set; }
        public required string Rotation { get; set; }
    }
}
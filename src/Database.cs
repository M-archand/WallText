using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using System.Globalization;
using System.Text.Json;
using System.Data;
using Microsoft.Extensions.Logging;
using Dapper;
using MySqlConnector;

namespace WallText
{
    public partial class PluginWallText
    {
        private string? _connectionString;

        private void InitializeDatabaseConnectionString()
        {
            if (!Config.EnableDatabase) return;

            var csb = new MySqlConnectionStringBuilder
            {
                Server   = Config.DatabaseSettings.Host,
                Port     = (uint)Config.DatabaseSettings.Port,
                Database = Config.DatabaseSettings.Database,
                UserID   = Config.DatabaseSettings.Username,
                Password = Config.DatabaseSettings.Password,
                SslMode  = Enum.TryParse<MySqlSslMode>(Config.DatabaseSettings.SslMode, true, out var mode)
                           ? mode
                           : MySqlSslMode.None
            };

            _connectionString = csb.ToString();
        }

        private IDbConnection CreateDbConnection()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("MySQL connection string not initialized");

            return new MySqlConnection(_connectionString);
        }

        private async Task EnsureTablesAsync()
        {
            if (!Config.EnableDatabase) return;

            string table = $"{Config.DatabaseSettings.TableName}";
            string sql = $@"
                CREATE TABLE IF NOT EXISTS `{table}` (
                `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                `MapName`     VARCHAR(255) NOT NULL,
                `GroupNumber` INT          NOT NULL,
                `Location`    VARCHAR(128) NOT NULL, -- ""X Y Z"" (InvariantCulture)
                `Angle`       VARCHAR(128) NOT NULL, -- ""P Y R"" (InvariantCulture)
                PRIMARY KEY (`Id`),
                KEY `idx_map` (`MapName`),
                KEY `idx_map_group` (`MapName`, `GroupNumber`),
                UNIQUE KEY `uniq_exact` (`MapName`,`GroupNumber`,`Location`,`Angle`)
                );";

            using var conn = CreateDbConnection();
            await conn.ExecuteAsync(sql);
        }

        private sealed class WtTextRecord
        {
            public ulong Id { get; set; }
            public int GroupNumber { get; set; }
            public string Location { get; set; } = "";
            public string Angle { get; set; } = "";
        }

        private static string VecToStringInvariant(Vector v) =>
            $"{v.X.ToString("0.###", CultureInfo.InvariantCulture)} {v.Y.ToString("0.###", CultureInfo.InvariantCulture)} {v.Z.ToString("0.###", CultureInfo.InvariantCulture)}";

        private static string AngToStringInvariant(QAngle a) =>
            $"{a.X.ToString("0.###", CultureInfo.InvariantCulture)} {a.Y.ToString("0.###", CultureInfo.InvariantCulture)} {a.Z.ToString("0.###", CultureInfo.InvariantCulture)}";

        private async Task InsertWorldTextToDb(string mapName, int group, Vector location, QAngle rotation)
        {
            string table = $"{Config.DatabaseSettings.TableName}";
            using var conn = CreateDbConnection();

            var loc = VecToStringInvariant(location);
            var ang = AngToStringInvariant(rotation);

            string sql = $@"
                    INSERT IGNORE INTO `{table}` (`MapName`,`GroupNumber`,`Location`,`Angle`)
                    VALUES (@m,@g,@loc,@ang);";

            await conn.ExecuteAsync(sql, new { m = mapName, g = group, loc, ang });
        }

        private async Task DeleteWorldTextFromDb(string mapName, int group, Vector location, QAngle rotation)
        {
            string table = $"{Config.DatabaseSettings.TableName}";
            using var conn = CreateDbConnection();

            var loc = VecToStringInvariant(location);
            var ang = AngToStringInvariant(rotation);

            string sql = $@"
                DELETE FROM `{table}`
                WHERE `MapName`=@m AND `GroupNumber`=@g AND `Location`=@loc AND `Angle`=@ang
                LIMIT 1;";

            await conn.ExecuteAsync(sql, new { m = mapName, g = group, loc, ang });
        }

        // Imports any existing JSON text files into the database
        private void OnImportText(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null || command == null) return;

            if (!AdminManager.PlayerHasPermissions(player, Config.CommandPermission))
            {
                player.PrintToChat($"{chatPrefix} {ChatColors.LightRed}You do not have permission to execute this command");
                return;
            }

            if (!Config.EnableDatabase)
            {
                player.PrintToChat($"{chatPrefix} {ChatColors.LightRed}EnableDatabase must be true to import into the database");
                return;
            }

            player.PrintToChat($"{chatPrefix} {ChatColors.White}Scanning {ChatColors.Lime}/plugins/WallText/maps {ChatColors.White}folder");

            _ = Task.Run(async () =>
            {
                var mapsDir = Path.Combine(ModuleDirectory, "maps");
                if (!Directory.Exists(mapsDir))
                {
                    Server.NextWorldUpdate(() => player.PrintToChat($"{chatPrefix} {ChatColors.Red}No maps folder found at {mapsDir}"));
                    return;
                }

                string[] files;
                try
                {
                    files = Directory.GetFiles(mapsDir, "*.json", SearchOption.TopDirectoryOnly);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "[Wall-Text] Error reading maps directory");
                    Server.NextWorldUpdate(() => player.PrintToChat($"{chatPrefix} {ChatColors.Red}Failed to read {mapsDir} (check logs)"));
                    return;
                }

                var firstFew = string.Join(", ", files.Take(5).Select(Path.GetFileName));

                var importQueue = new List<ImportEntry>();
                int filesTouched = 0;

                foreach (var filePath in files)
                {
                    var fileName = Path.GetFileName(filePath);

                    var baseName = Path.GetFileNameWithoutExtension(filePath);
                    if (baseName.EndsWith("_text", StringComparison.OrdinalIgnoreCase))
                        baseName = baseName[..^5];
                    var mapName = baseName;

                    filesTouched++;

                    List<WorldTextData>? data = null;
                    try
                    {
                        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                        data = JsonSerializer.Deserialize<List<WorldTextData>>(json, JsonOpts);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"[Wall-Text] Failed reading {fileName}; skipping");
                        continue;
                    }

                    if (data == null || data.Count == 0)
                    {
                        Logger.LogInformation($"[Wall-Text] {fileName}: 0 entries");
                        continue;
                    }

                    int ok = 0;
                    foreach (var entry in data)
                    {
                        try
                        {
                            if (entry.GroupNumber <= 0) continue;

                            var locParts = entry.Location.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            var angParts = entry.Rotation.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            if (locParts.Length != 3 || angParts.Length != 3) throw new ArgumentException("Bad vector/angle parts");

                            if (!TryParseFloatInv(locParts[0], out var fx) ||
                                !TryParseFloatInv(locParts[1], out var fy) ||
                                !TryParseFloatInv(locParts[2], out var fz))
                                throw new ArgumentException("Bad vector floats");

                            if (!TryParseFloatInv(angParts[0], out var fp) ||
                                !TryParseFloatInv(angParts[1], out var fyaw) ||
                                !TryParseFloatInv(angParts[2], out var fr))
                                throw new ArgumentException("Bad angle floats");

                            importQueue.Add(new ImportEntry
                            {
                                MapName = mapName,
                                GroupNumber = entry.GroupNumber,
                                X = fx, Y = fy, Z = fz,
                                Pitch = fp, Yaw = fyaw, Roll = fr
                            });
                            ok++;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"[Wall-Text] {fileName}: skipping malformed entry (#{importQueue.Count + 1}). {ex.Message}");
                            continue;
                        }
                    }

                    Logger.LogInformation($"[Wall-Text] {fileName}: queued {ok} / {data.Count}.");
                }

                var totalQueued = importQueue.Count;

                Server.NextWorldUpdate(() =>
                {
                    player.PrintToChat($"{chatPrefix} {ChatColors.White}Queued {totalQueued} placements from {filesTouched} files");
                    if (totalQueued == 0)
                        player.PrintToChat($"{chatPrefix} {ChatColors.Red}Nothing to import");
                });

                if (totalQueued == 0) return;


                const int importsPerFrame = 1;
                int index = 0;

                Action pump = null!;
                pump = () =>
                {
                    int doneThisFrame = 0;
                    while (index < totalQueued && doneThisFrame < importsPerFrame)
                    {
                        var e = importQueue[index++];
                        try
                        {
                            var loc = new Vector(e.X, e.Y, e.Z);
                            var ang = new QAngle(e.Pitch, e.Yaw, e.Roll);

                            InsertWorldTextToDb(e.MapName, e.GroupNumber, loc, ang).GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, $"[Wall-Text] Import insert failed for {e.MapName}/Group {e.GroupNumber}");
                        }
                        doneThisFrame++;
                    }

                    if (index < totalQueued)
                    {
                        Server.NextWorldUpdate(pump);
                    }
                    else
                    {
                        Server.NextWorldUpdate(() =>
                        {
                            RefreshText();
                            player.PrintToChat($"{chatPrefix} {ChatColors.Lime}Database import completed! {ChatColors.White}{totalQueued}{ChatColors.Lime} placements imported!");
                        });
                    }
                };

                Server.NextWorldUpdate(pump);
            });
        }

        private sealed class ImportEntry
        {
            public required string MapName { get; init; }
            public int GroupNumber { get; init; }
            public float X { get; init; }
            public float Y { get; init; }
            public float Z { get; init; }
            public float Pitch { get; init; }
            public float Yaw   { get; init; }
            public float Roll  { get; init; }
        }
    }
}
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Data;
using Microsoft.Extensions.Logging;
using Dapper;
using MySqlConnector;
using System.Globalization;

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
                throw new InvalidOperationException("MySQL connection string not initialized. Call InitializeDatabaseConnectionString() first.");

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
    }
}
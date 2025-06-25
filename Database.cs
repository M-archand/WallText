using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using Dapper;
using MySqlConnector;

namespace WallText
{
    public partial class PluginWallText : BasePlugin, IPluginConfig<PluginConfig>
    {
        private string? _databasePath;
        private string? _connectionString;

        private void InitializeDatabaseAndConnectionString()
        {
            var dbSettings = Config.DatabaseSettings;

            var mySqlSslMode = dbSettings.Sslmode.ToLower() switch
            {
                "none" => MySqlSslMode.None,
                "preferred" => MySqlSslMode.Preferred,
                "required" => MySqlSslMode.Required,
                "verifyca" => MySqlSslMode.VerifyCA,
                "verifyfull" => MySqlSslMode.VerifyFull,
                _ => MySqlSslMode.None
            };
            _connectionString = $@"Server={dbSettings.Host};Port={dbSettings.Port};Database={dbSettings.Database};Uid={dbSettings.Username};Pwd={dbSettings.Password};SslMode={mySqlSslMode};AllowPublicKeyRetrieval=True;";
            
        }

        private async Task<bool> HasEmptyDbSlot(string mapName, Group group)
        {
            string table = Config.DatabaseSettings.TableName;
            using var conn = 

            var rec = await conn.QueryFirstOrDefaultAsync<StListRecord>(
                $@"SELECT Location1,Location2,Location3,Location4
                FROM {table}
                WHERE MapName = @m AND Group = @g;",
                new { m = mapName, t = group.ToString() }
            );

            if (rec == null)
                return true;

            if (string.IsNullOrEmpty(rec.Location1)) return true;
            if (string.IsNullOrEmpty(rec.Location2)) return true;
            if (string.IsNullOrEmpty(rec.Location3)) return true;
            if (string.IsNullOrEmpty(rec.Location4)) return true;

            return false;
        }

    }
}

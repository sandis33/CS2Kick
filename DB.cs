using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using MySqlConnector;
using Kick.Player;
using Microsoft.Extensions.Logging;
using static Kick.ModuleWeb;
using System.Web;

namespace Kick
{
    public sealed partial class Plugin : BasePlugin
    {
        public MySqlConnection CreateConnection()
        {
            bool debugserv = false;

            if (!debugserv)
            {
                MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
                {
                    Server = "127.0.0.1",
                    UserID = "kick_cs2",
                    Password = "ymMxFd7DC#tBz",
                    Database = "kick_web",
                    Port = (uint)3306,

                };
                return new MySqlConnection(builder.ToString());
            }
            else
            {
                MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
                {
                    Server = "127.0.0.1",
                    UserID = "root",
                    Password = "parole",
                    Database = "kick_web",
                    Port = (uint)3306,
                };
                return new MySqlConnection(builder.ToString());
            }
        }
        public async Task LoadWebDataAsync(KickPlayer kickplayer)
        {
            string combinedQuery = $@"
                SELECT id,username,lvl,xp,name,steamid2 FROM web_users WHERE steamid2 = @steamid2";

            try
            {
                using (var connection = CreateConnection())
                {
                    await connection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@steamid2", kickplayer.SteamID2);

                    var rows = await connection.QueryAsync(combinedQuery, parameters);
                    foreach (var row in rows)
                    {
                        Server.NextFrame(() => LoadPlayerRowToCache(kickplayer, row));
                    }
                    Server.NextFrame(() => SetWebStatusAsync(kickplayer));
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() => Logger.LogError("An error occurred while loading player cache: {ErrorMessage}", ex.Message));
            }
        }
        public void LoadPlayerRowToCache(KickPlayer kickplayer, dynamic row)
        {
            WebData? webData = null;
            webData = new WebData
            {
                webID = row.id,
                webName = row.username,
                xp = row.xp,
                lvl = row.lvl,
                hasWeb = true,
            };
            
            kickplayer.webData = webData;
            KickPlayers.Add(kickplayer);
        }
        public async Task SetWebStatusAsync(KickPlayer kickplayer)
        {
            Int32 unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            string last_activity = unixTimestamp.ToString();

            string combinedQuery = $@"
                UPDATE web_users SET current_module = @current_module, last_activity = @last_activity WHERE id = @kickid";

            try
            {
                using (var connection = CreateConnection(/*Config*/))
                {
                    await connection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@kickid", kickplayer.webData.webID);
                    parameters.Add("@current_module", "play_2cs1");
                    parameters.Add("@last_activity", last_activity);

                    var rows = await connection.QueryAsync(combinedQuery, parameters);

                    Server.NextFrame(() => UpdateWebStatusAsync(kickplayer.webData.webID));
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() => Logger.LogError("ERROR SETWEBSTATUS: {ErrorMessage}", ex.Message));
            }
        }

        public async Task UpdateWebStatusAsync(int kickid)
        {
            using (var _httpclient = new HttpClient())
            {
                msg = $"/?type=update_location&userid={kickid}&online=0";
                var url = $"{host}{msg}";
                var response = await _httpclient.GetAsync(url);
            }

        }
    }
}
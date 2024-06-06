using System;
using System.Collections.Generic;
using System.Data;
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
                SELECT id,username,name,lvl,xp,xp_time,steamid2 FROM web_users WHERE steamid2 = @steamid2";

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
                hasWeb = true,
                webID = row.id,
                webNick = row.username,
                webName = row.name,
                lvl = row.lvl,
                xp = row.xp,
                xpTime = row.xp_time,
                itemChance = 0,
            };

            kickplayer.webData = webData;
        }
        public async Task SetWebStatusAsync(KickPlayer kickplayer)
        {
            Int32 unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            string last_activity = unixTimestamp.ToString();

            string combinedQuery = $@"
                UPDATE web_users SET current_module = @current_module, last_activity = @last_activity WHERE id = @kickID";

            try
            {
                using (var connection = CreateConnection(/*Config*/))
                {
                    await connection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@kickID", kickplayer.webData.webID);
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

        public async Task UpdateWebStatusAsync(int kickID)
        {
            using (var _httpclient = new HttpClient())
            {
                string msg = $"/?type=update_location&userid={kickID}&online=0";
                var url = $"{host}{msg}";
                var response = await _httpclient.GetAsync(url);
            }

        }

        public async Task ReloadWebDataAsync(KickPlayer kickplayer)
        {
            string query = $@"
            SELECT xp_time FROM web_users WHERE id = @kickID";

            try
            {
                using (var connection = CreateConnection())
                {
                    await connection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@kickID", kickplayer.webData.webID);

                    var results = await connection.QueryFirstAsync(query, parameters);
                    kickplayer.webData.xpTime = results.xp_time;
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame((() => Logger.LogError("ERROR WHILE UPDATING WEBDATA: {ErrorMessage}", ex.Message)));
            }
        }
        public async Task UpdateXpTime(KickPlayer kickplayer)
        {
            string combinedQuery = $@"
                UPDATE web_users SET xp_time = @xptime WHERE id = @kickid";

            try
            {
                using (var connection = CreateConnection(/*Config*/))
                {
                    await connection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@kickid", kickplayer.webData.webID);
                    parameters.Add("@xptime", kickplayer.webData.xpTime);

                    var rows = await connection.QueryAsync(combinedQuery, parameters);
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() => Logger.LogError("ERROR while updating XP_TIME in DB: {ErrorMessage}", ex.Message));
            }
        }
        public async Task SaveAllPlayersDataAsync()
        {
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();

                using (var save = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (KickPlayer kickplayer in KickPlayers.ToList())
                        {
                            if (!kickplayer.IsValid || !kickplayer.IsPlayer)
                                continue;

                            if (kickplayer.webData.hasWeb)
                                await UpdateXpTime(kickplayer);
                        }

                        await save.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await save.RollbackAsync();
                        Server.NextFrame(() => Logger.LogError("ERROR while saving all players WEB data: {ErrorMessage}", ex.Message));
                        throw;
                    }
                }
            }

            KickPlayers = new List<KickPlayer>(KickPlayers.Where(player => player.IsValid));
        }
    }
}
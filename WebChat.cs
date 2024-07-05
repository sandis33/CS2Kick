using System.Web;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Admin;
using Newtonsoft.Json;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using Kick.Player;
using static Kick.ModuleWeb;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text;

namespace Kick
{ 
    public partial class KickCS2
    {
        private static readonly string host = "http://127.0.0.1:9080";
        private readonly string server = "ttt";
        private static readonly string callAdminServer = "CS2 PUB";
        public readonly int xpTime = 3600;

        public int ctWins = 0;
        public int tsWins = 0;
        public int MaxPlayers { get; set; }
        public string Map { get; set; }
        public class User
        {
            public int Id { get; set; }
            public int chance { get; set; }
        }

        public class UserList
        {
            public List<User> users { get; set; }
            public List<Guest> guests { get; set; }
        }

        public class Guest
        {
            public string name { get; set; }
            public int chance { get; set; }
        }
        public int playersOn
        {
            get
            {
                int players = 0;
                foreach (var player in Utilities.GetPlayers())
                {
                    if (player is null || !player.IsValid || player.IsBot)
                        continue;
                    KickPlayer? kickplayer = GetKickPlayer(player!);
                    if (kickplayer is null || !kickplayer.IsValid || !kickplayer.IsPlayer)
                        continue;
                    players++;
                }
                return players;
            }
        }
        public void InitializeChat()
        {                       
            var monitor = AddTimer(1, () =>
            {
                updateMonitor();
            }, TimerFlags.REPEAT);
        }


        public HookResult OnPlayerSayPublic(CCSPlayerController? player, CommandInfo info)
        {
            string chatMsg = info.GetArg(1);

            if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV || chatMsg == null)
                return HookResult.Continue;

            KickPlayer? kickplayer = GetKickPlayer(player!);
            WebData? WebData = kickplayer.WebData;
            int kickID = WebData.webID;
            string playername = player.PlayerName;
            if (chatMsg.StartsWith('/'))
                return HookResult.Continue;
            if (chatMsg.StartsWith('!'))
                return HookResult.Continue;
            if (string.IsNullOrWhiteSpace(chatMsg))
                return HookResult.Continue;

            if (kickplayer.WebData.hasWeb)
            {
                string msg =
                    $"/?id={kickID}&nick={HttpUtility.UrlEncode(playername)}&msg={HttpUtility.UrlEncode(chatMsg)}&server={server}";
                _ = WebGetReqAsync(msg);
            }
            else
            {
                string msg =
                    $"/?nick={HttpUtility.UrlEncode(playername)}&msg={HttpUtility.UrlEncode(chatMsg)}&server={server}";
                _ = WebGetReqAsync(msg);
            }
            return HookResult.Continue;
        }

        public static async Task WebGetReqAsync(string msg)
        {
            using var _httpclient = new HttpClient();
            var url = $"{host}{msg}";
            var response = await _httpclient.GetAsync(url);
        }

        public void startItemTimer(KickPlayer kickplayer)
        {
            if (!kickplayer.IsPlayer && !kickplayer.IsValid && !kickplayer.WebData.hasWeb)
                return;
            kickplayer.itemTimer = AddTimer(1, () =>
            {
                kickplayer.WebData.itemChance++;
            }, TimerFlags.REPEAT);
        }

        public void startXpTimer(KickPlayer kickplayer)
        {
            if (!kickplayer.IsPlayer && !kickplayer.WebData.hasWeb)
                return;
            string msg = $"/?type=add_server_xp&userid={kickplayer.WebData.webID}&server=cs2";
            kickplayer.XPtimer = AddTimer(10, () =>
            {
                kickplayer.WebData.xpTime += 10;
                if (kickplayer.WebData.xpTime == xpTime)
                {
                    kickplayer.Controller.PrintToChat($" \x04[Kick] \x10Saņēmi\x04 10 \x10XP forumā par spēlēšanu serverī");
                    kickplayer.WebData.xpTime = 0;
                    _ = WebGetReqAsync(msg);
                    Server.NextFrame(async () => await UpdateXpTime(kickplayer));
                }
            }, TimerFlags.REPEAT);
        }

        public void rewardItems()
        {
            var userlist = new UserList
            {
                users = new List<User>(),
                guests = new List<Guest> { }
            };
            foreach (var kickplayer in KickPlayers)
            {
                
                if (!kickplayer.IsValid || !kickplayer.IsPlayer)
                    continue;
                if (kickplayer.WebData.hasWeb)
                {
                    var newUser = new User
                    {
                        Id = kickplayer.WebData.webID,
                        chance = kickplayer.WebData.itemChance
                    };
                    userlist.users.Add(newUser);
                    kickplayer.itemTimer?.Kill();
                    kickplayer.itemTimer = null;
                    kickplayer.WebData.itemChance = 0;
                }
            }

            foreach (var kickplayer in KickPlayers)
            {
                
                if (!kickplayer.WebData.hasWeb)
                {
                    var newGuest = new Guest
                    {
                        name = HttpUtility.UrlEncode(kickplayer.PlayerName),
                        chance = kickplayer.WebData.itemChance
                    };
                    userlist.guests.Add(newGuest);
                    kickplayer.itemTimer?.Kill();
                    kickplayer.itemTimer = null;
                    kickplayer.WebData.itemChance = 0;
                }
            }

            var usersJson = JsonConvert.SerializeObject(userlist);
            string type = $"/?type=award_item_winners&server={server}&list=";
            var msg = $"{type}{usersJson}";
            _ = WebGetReqAsync(msg);

        }

        public void SharedGetReq(string msg)
        {
            _ = WebGetReqAsync(msg);
        }
        public void updateMonitor()
        {
            string updateMonitors;
            updateMonitors = $"\"Map\" is \"{Map}\"\n\"playercount\" is \"{playersOn}\"\n\"twin\" is \"{tsWins}\"\n\"ctwin\" is \"{ctWins}\"\n\"maxplayers\" is \"{MaxPlayers}\"\n";
            foreach (var player in Utilities.GetPlayers())
            {
                if(player is null || !player.IsValid || player.IsBot)
                    continue;
                KickPlayer? kickplayer = GetKickPlayer(player!);
                if (kickplayer is null || !kickplayer.IsValid || !kickplayer.IsPlayer)
                    continue;
                string team = player.TeamNum.ToString();
                switch (player.TeamNum)
                {
                    case 0: //unas
                        team = "UNASSIGNED|SPECTATOR";
                        break;
                    case 1: //spec
                        team = "UNASSIGNED|SPECTATOR";
                        break;
                    case 2:
                        team = "TERRORIST";
                        break;
                    case 3:
                        team = "CT";
                        break;
                }
                string escapedName = kickplayer.PlayerName.Replace('\"', ' ');
                int frags = player.ActionTrackingServices?.MatchStats?.Kills ?? 0;
                int vips = 0;
                string country = "Latvia";
                if (AdminManager.PlayerHasPermissions(player, "@css/vip")) 
                    vips = 1;
                if (kickplayer.WebData.hasWeb)
                {
                    updateMonitors =
                        $"{updateMonitors}\"{HttpUtility.UrlEncode(escapedName)}\" \"{team}\" \"{frags}\" \"{country}\" \"{kickplayer.WebData.webID}/{kickplayer.WebData.username_seo}/\" \"{vips}\"";
                }
                else
                {
                    updateMonitors =
                        $"{updateMonitors}\"{HttpUtility.UrlEncode(escapedName)}\" \"{team}\" \"{frags}\" \"{country}\" \" \" \"{vips}\"";
                }
            }
            _ = UpdateMonitorsAsync(updateMonitors);
        }        
        //public void updateMonitor()
        //{
        //    //To add????
        //    //server_print("^"amx_nextmap ^ " is ^" % s ^ "", amx_nextmap);
        //    //server_print("^"amx_timeleft ^ " is ^" % s ^ "", amx_timeleft);
        //    //server_print("^"mp_timelimit ^ " is ^" % s ^ "", mp_timelimit);
        //    // Add country to users data
        //    //if (user_id[i]) server_print(user_name[i], team, frags, country[i], user_id[i], user_seo[i], vips);
        //    string serverInfo;
        //    string playerInfo = "";
        //    string sendUpdate = "";




        //    serverInfo = $"\"Map\" is \"{Map}\"\n\"playercount\" is \"{playersOn}\"\n\"twin\" is \"{tsWins}\"\n\"ctwin\" is \"{ctWins}\"\n\"maxplayers\" is \"{MaxPlayers}\"\n";
        //    foreach (var player in Utilities.GetPlayers())
        //    {
        //        if (player is null || !player.IsValid || player.IsBot)
        //            continue;
        //        KickPlayer? kickplayer = GetKickPlayer(player!);
        //        if (kickplayer is null || !kickplayer.IsValid || !kickplayer.IsPlayer)
        //            continue;
        //        string team = player.TeamNum.ToString();
        //        switch (player.TeamNum)
        //        {
        //            case 0: //unas
        //                team = "UNASSIGNED|SPECTATOR";
        //                break;
        //            case 1: //spec
        //                team = "UNASSIGNED|SPECTATOR";
        //                break;
        //            case 2:
        //                team = "TERRORIST";
        //                break;
        //            case 3:
        //                team = "CT";
        //                break;
        //        }


        //        string escapedName = kickplayer.PlayerName.Replace('\"', ' ');
        //        int frags = player.ActionTrackingServices?.MatchStats?.Kills ?? 0;
        //        int vips = 0;
        //        string country = "Latvia";
        //        if (AdminManager.PlayerHasPermissions(player, "@css/vip"))
        //            vips = 1;
        //        if (kickplayer.WebData.hasWeb)
        //        {
        //            playerInfo =
        //                $"{playerInfo}\"{escapedName}\" \"{team}\" \"{frags}\" \"{country}\" \"{kickplayer.WebData.webID}/{kickplayer.WebData.username_seo}/\" \"{vips}\"\n";
        //        }
        //        else
        //        {
        //            playerInfo =
        //                $"{playerInfo}\"{escapedName}\" \"{team}\" \"{frags}\" \"{country}\" \"0/ /\" \"{vips}\"";
        //        }
        //        sendUpdate = serverInfo + playerInfo;
        //    }
        //    _ = UpdateMonitorsAsync(sendUpdate);
        //}       
    }
}
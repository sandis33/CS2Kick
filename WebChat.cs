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
    public sealed partial class Plugin
    {
        public readonly string host = "http://127.0.0.1:9080";
        public readonly string server = "ttt";
        public readonly string callAdminServer = "CS2PUB";
        private int xpTime = 3600;

        public int ctWins = 0;
        public int tsWins = 0;
        public int maxPlayers { get; set; }
        public string map { get; set; }
        public class user
        {
            public int id { get; set; }
            public int chance { get; set; }
        }

        public class userList
        {
            public List<user> users { get; set; }
            public List<guest> guests { get; set; }
        }

        public class guest
        {
            public string name { get; set; }
            public int chance { get; set; }
        }

        public void Initialize_Chat()
        {
            CreateLog();
            //AddCommand("css_web", "web test", OnCommandWeb);
            //AddCommand("css_get", "web test", OnCommandTestGet);
            AddCommand("webchat", "Send message from web", OnCommandWebChatRcon);
            AddCommand("web_pm_say", "Sends pm to player from web", OnCommadWebPmSay);
            AddCommand("css_calladmin", "Makes a call for admin", OnCommandCalladmin);
            AddCommand("css_help", "List available commands",[CommandHelper(0, whoCanExecute: CommandUsage.CLIENT_ONLY)] (player, info) =>
                {
                    if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
                        return;

                    info.ReplyToCommand($" \x0A[\x04Kick.lv\x0A] \x04Pieejamās komandas");
                    info.ReplyToCommand($" --- \x0AStatu komadas: \x04!rank !top !time");
                    info.ReplyToCommand($" --- \x0AVisi pieejamie rangi: \x04!ranks");
                    info.ReplyToCommand($" --- \x0APaziņojumi par punktu izmaiņām: \x04!points");
                    info.ReplyToCommand($" --- \x0AIzsaukt administratoru \x04!calladmin");
                    info.ReplyToCommand($" --- \x0ARTV saistītās komandas \x04!rtv !nominate !timeleft !nextmap");
                });
            AddCommandListener("say", OnPlayerSayPublic, HookMode.Post);

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
            WebData? webData = kickplayer.webData;
            int kickID = webData.webID;
            string playername = player.PlayerName;
            if (chatMsg.StartsWith("/"))
                return HookResult.Continue;
            if (chatMsg.StartsWith("!"))
                return HookResult.Continue;
            if (string.IsNullOrWhiteSpace(chatMsg))
                return HookResult.Continue;

            if (kickplayer.webData.hasWeb)
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

            string logmsg = ($" {player.SteamID} || {playername}: {chatMsg}");
            chatLog(logmsg);
            return HookResult.Continue;
        }

        public async Task WebGetReqAsync(string msg)
        {
            using (var _httpclient = new HttpClient())
            {
                var url = $"{host}{msg}";
                var response = await _httpclient.GetAsync(url);
            }
        }

        public void startItemTimer(KickPlayer kickplayer)
        {
            if (!kickplayer.IsPlayer && !kickplayer.IsValid && !kickplayer.webData.hasWeb)
                return;
            kickplayer.itemTimer = AddTimer(1, () =>
            {
                kickplayer.webData.itemChance++;
            }, TimerFlags.REPEAT);
        }

        public void startXpTimer(KickPlayer kickplayer)
        {


            if (!kickplayer.IsPlayer && !kickplayer.webData.hasWeb)
                return;

            string msg = $"/?type=add_server_xp&userid={kickplayer.webData.webID}&server=cs2";

            kickplayer.XPtimer = AddTimer(10, () =>
            {
                kickplayer.webData.xpTime += 10;
                if (kickplayer.webData.xpTime == xpTime)
                {
                    kickplayer.Controller.PrintToChat($" \x04[Kick] \x10Saņēmi\x04 10 \x10XP forumā par spēlēšanu serverī");
                    kickplayer.webData.xpTime = 0;
                    _ = WebGetReqAsync(msg);
                    Server.NextFrame(() => UpdateXpTime(kickplayer));
                }
            }, TimerFlags.REPEAT);

        }

        public void rewardItems()
        {
            userList userlist = new userList
            {
                users = new List<user>(),
                guests = new List<guest> { }
            };
            foreach (var kickplayer in KickPlayers)
            {
                
                if (!kickplayer.IsValid || !kickplayer.IsPlayer)
                    continue;
                if (kickplayer.webData.hasWeb)
                {
                    user newUser = new user
                    {
                        id = kickplayer.webData.webID,
                        chance = kickplayer.webData.itemChance
                    };
                    userlist.users.Add(newUser);
                    kickplayer.itemTimer?.Kill();
                    kickplayer.itemTimer = null;
                    kickplayer.webData.itemChance = 0;
                }
            }

            foreach (var kickplayer in KickPlayers)
            {
                
                if (!kickplayer.webData.hasWeb)
                {
                    guest newGuest = new guest
                    {
                        name = HttpUtility.UrlEncode(kickplayer.PlayerName),
                        chance = kickplayer.webData.itemChance
                    };
                    userlist.guests.Add(newGuest);
                    kickplayer.itemTimer?.Kill();
                    kickplayer.itemTimer = null;
                    kickplayer.webData.itemChance = 0;
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
            //To add????
            //server_print("^"amx_nextmap ^ " is ^" % s ^ "", amx_nextmap);
            //server_print("^"amx_timeleft ^ " is ^" % s ^ "", amx_timeleft);
            //server_print("^"mp_timelimit ^ " is ^" % s ^ "", mp_timelimit);
            // Add country to users data
            //if (user_id[i]) server_print(user_name[i], team, frags, country[i], user_id[i], user_seo[i], vips);
            string updateMonitors;
            int playersOn = 0;
            

            foreach (var player in Utilities.GetPlayers())
            {
                if (player is null || !player.IsValid || player.IsBot)
                    continue;
                KickPlayer? kickplayer = GetKickPlayer(player!);
                if (kickplayer is null || !kickplayer.IsValid || !kickplayer.IsPlayer)
                    continue;
                playersOn++;
            }
            
            updateMonitors = $"\"map\" is \"{map}\"\n\"playercount\" is \"{playersOn}\"\n\"twin\" is \"{tsWins}\"\n\"ctwin\" is \"{ctWins}\"\n\"maxplayers\" is \"{maxPlayers}\"\n";
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
                if (AdminManager.PlayerHasPermissions(player, "css_vip")) vips = 1;
                if (kickplayer.webData.hasWeb)
                {
                    updateMonitors =
                        $"{updateMonitors}\"{escapedName}\" \"{team}\" \"{frags}\" \"{country}\" \"{kickplayer.webData.webID}/{kickplayer.webData.username_seo}/\" \"{vips}\"\n";
                }
                else
                {
                    updateMonitors =
                        $"{updateMonitors}\"{escapedName}\" \"{team}\" \"{frags}\" \"{country}\" \"{vips}\"";
                }
            }

            playersOn = 0;
            _ = UpdateMonitorsAsync(updateMonitors);
        }
        //To do for Call Admin: Add user, reason panel
        //	[red]Lxst[/red] sauc adminu uz [red]JB[/red] serveri! Parkapejs: [red]Ass[/red] Iemesls: [red]Cheats[/red]
        public void OnCommandCalladmin(CCSPlayerController player, CommandInfo info)
        {
            KickPlayer? kickplayer = GetKickPlayer(player!);
            
            if (kickplayer.CDTimer != null)
            {
                player.PrintToChat($" \x04[Kick]\x10 Tu jau nosūtiji izsaukumu pēc administratora!");
                return;
            }
                

            kickplayer.CDTimer = AddTimer(60, ()=>
            {
                kickplayer.CDTimer?.Kill();
                kickplayer.CDTimer = null;
            });

            info.ReplyToCommand($" \x04[Kick]\x10 Izsaukums pēc admina nosūtīts, paldies un gaidi adminu!");
            info.ReplyToCommand($" \x04[Kick]\x0F LASI ČATU UN NEEJ ĀRĀ NO SERVERA NĀKAMĀS 10 MINŪTES.");
            info.ReplyToCommand($" \x04[Kick]\x0F ADMINS var uzdot jautājumus calladmin sakarā un tev ir pienākums viņam atbildēt!");
            string playername = kickplayer.PlayerName;
            string calladminmsg = $"[red]{HttpUtility.UrlEncode(playername)}[/red] sauc adminu uz [red]{callAdminServer}[/red] serveri!";
            string msg = $"/?&nick=CALLADMIN&msg={calladminmsg}&server=forums";
            _ = WebGetReqAsync(msg);
        }

        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnCommandWebChatRcon(CCSPlayerController player, CommandInfo info)
        {
            if (!CommandHelper(player, info, CommandUsage.SERVER_ONLY, 2, "<name> <msg>", permission: "@css/rcon"))
                return;
            string name = info.GetArg(1);
            string msg = info.GetArg(2);

            Server.PrintToChatAll($" \x04[WEB] \x0F{name} \x10: {msg}");
        }

        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnCommadWebPmSay(CCSPlayerController player, CommandInfo info)
        {
            if (!CommandHelper(player, info, CommandUsage.SERVER_ONLY, 2, "<id> <msg>", permission: "@css/rcon"))
                return;


            int id = int.Parse(info.GetArg(1));
            string msg = info.GetArg(2);

            if (msg == null || msg.StartsWith(" "))
                return;
            foreach (var kickplayer in KickPlayers)
            {
                if(!kickplayer.webData.hasWeb)
                    continue;
                if (kickplayer.webData.webID == id)
                {
                    kickplayer.Controller.PrintToChat($" \x0F[kick.lv] \x04: {msg}");
                }
            }
        }

        //////////////////////////////
        //          CHATLOG         //
        //////////////////////////////
        public void CreateLog()
        {
            var LogPath = Path.Combine(_ModuleDirectory, "../../logs/kick/");
            var FileName = ("chat-") + DateTime.Now.ToString("MM-dd-yyyy") + (".txt");
            var FilePath = Path.Combine(_ModuleDirectory, "../../logs/kick/") + ($"{FileName}");

            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }

            if (!File.Exists(FilePath))
            {
                File.Create(FilePath);
            }
        }
        public void chatLog(string logmsg)
        {
            var FileName = ("chat-") + DateTime.Now.ToString("MM-dd-yyyy") + (".txt");
            var FilePath = Path.Combine(_ModuleDirectory, "../../logs/kick/") + ($"{FileName}");
            using (StreamWriter writer = File.AppendText(FilePath)) 
            {
                writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + logmsg);
            }
        }
        //DEBUGING CMDS//

        //public void OnCommandTestGet(CCSPlayerController pl, CommandInfo info)
        //{
        //    userList userlist = new userList
        //    {
        //        users = new List<user>(),
        //        guests = new List<guest> { }
        //    };
        //    foreach (var player in Utilities.GetPlayers())
        //    {
        //        KickPlayer? kickplayer = GetKickPlayer(player!);
        //        if (!kickplayer.IsValid || !kickplayer.IsPlayer)
        //            continue;
        //        if (kickplayer.webData.hasWeb)
        //        {
        //            user newUser = new user
        //            {
        //                id = kickplayer.webData.webID,
        //                chance = kickplayer.webData.itemChance
        //            };
        //            userlist.users.Add(newUser);
        //            kickplayer.webData.itemChance = 60;
        //        }
        //    }

        //    foreach (var player in Utilities.GetPlayers())
        //    {
        //        KickPlayer? kickplayer = GetKickPlayer(player!);
        //        if (!kickplayer.IsValid || !kickplayer.IsPlayer)
        //            continue;
        //        if (!kickplayer.webData.hasWeb)
        //        {
        //            guest newGuest = new guest
        //            {
        //                name = HttpUtility.UrlEncode(kickplayer.PlayerName),
        //                chance = kickplayer.webData.itemChance
        //            };
        //            userlist.guests.Add(newGuest);
        //            kickplayer.webData.itemChance = 60;
        //        }
        //    }

        //    var usersJson = JsonConvert.SerializeObject(userlist);
        //    string type = "/?type=award_item_winners&server=ttt&list=";
        //    var msg = $"{type}{usersJson}";
        //    Server.PrintToChatAll(msg);
        //    _ = WebGetReqAsync(msg);
        //}

        //public void OnCommandWeb(CCSPlayerController player, CommandInfo info)
        //{
        //    KickPlayer? kp = GetKickPlayer(player!);
        //    info.ReplyToCommand($"web: {kp.webData.hasWeb.ToString()}");
        //    info.ReplyToCommand(kp.webData.webID.ToString());
        //    info.ReplyToCommand(kp.webData.webNick);
        //    info.ReplyToCommand(kp.webData.lvl.ToString());
        //    info.ReplyToCommand(kp.webData.xp.ToString());
        //    info.ReplyToCommand(kp.webData.xpTime.ToString());
        //}
    }

}
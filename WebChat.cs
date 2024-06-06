using System.Net;
using System.Web;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Admin;

namespace Kick
{
    using CounterStrikeSharp.API.Core;
    using Kick;
    using Serilog;
    using System.Text;
    using CounterStrikeSharp.API.Modules.Utils;
    using CounterStrikeSharp.API.Modules.Commands;
    using CounterStrikeSharp.API.Modules.Entities;
    using Kick.Player;
    using static Kick.ModuleWeb;
    using CounterStrikeSharp.API.Modules.Menu;
    using CounterStrikeSharp.API.Modules.Timers;

    public sealed partial class Plugin : BasePlugin
    {
        public readonly string host = "http://127.0.0.1:9080";
        public readonly string server = "ttt";
        public readonly string callAdminServer = "CS2PUB";
        private int xpTime = 3600;

        public string playername;
        private int kickID;

        //public static IKickShared? kick;



        private string? LogPath;
        private string? FileName;
        private string? FilePath;

        public void Initialize_Chat()
        {
            AddCommand("css_web", "web test", OnCommandWeb);
            AddCommand("webchat", "Send message from web", OnCommandWebChat);
            AddCommand("css_calladmin", "Makes a call for admin", OnCommandCalladmin);
            AddCommandListener("say", OnPlayerSayPublic, HookMode.Post);

            RegisterEventHandler((EventPlayerConnectFull @event, GameEventInfo info) =>
            {
                CCSPlayerController? player = @event.Userid;

                if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV)
                    return HookResult.Continue;

                string msg = $"/?id=31&nick={HttpUtility.UrlEncode("kick.lv")}&msg={HttpUtility.UrlEncode($"[green]{player.PlayerName}[/green] pievienojās serverim")}&server={server}";
                _ = sendChatMessageAsync(msg);
                return HookResult.Continue;
            });

            RegisterEventHandler((EventPlayerDisconnect @event, GameEventInfo info) =>
            {
                CCSPlayerController? player = @event.Userid;

                if (player is null || player.IsBot || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV)
                    return HookResult.Continue;

                string msg = $"/?id=31&nick={HttpUtility.UrlEncode("kick.lv")}&msg={HttpUtility.UrlEncode($"[blue]{player.PlayerName}[/blue] izgāja no servera")}&server={server}";
                _ = sendChatMessageAsync(msg);
                return HookResult.Continue;
            });

            RegisterListener<Listeners.OnMapStart>((mapName) =>
            {
                string msg = $"/?id=31&nick={HttpUtility.UrlEncode("kick.lv")}&msg={HttpUtility.UrlEncode($"Karte nomainījās uz [blue]{mapName}[/blue]")}&server={server}";
                _ = sendChatMessageAsync(msg);
                KickPlayers.Clear();
            });
        }
        public HookResult EventCsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info)
        {


            return HookResult.Continue;
        }
        public HookResult OnPlayerSayPublic(CCSPlayerController? player, CommandInfo info)
        {
            string chatMsg = info.GetArg(1);

            if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV || chatMsg == null)
                return HookResult.Continue;

            KickPlayer? kickplayer = GetKickPlayer(player!);
            WebData? webData = kickplayer.webData;
            kickID = webData.webID;
            playername = player.PlayerName;
            if (chatMsg.StartsWith("/"))
                return HookResult.Continue;
            if (chatMsg.StartsWith("!"))
                return HookResult.Continue;
            if (string.IsNullOrWhiteSpace(chatMsg))
                return HookResult.Continue;

            if (kickplayer.webData.hasWeb)
            {
                string msg = $"/?id={kickID}&nick={HttpUtility.UrlEncode(playername)}&msg={HttpUtility.UrlEncode(chatMsg)}&server={server}";
                _ = sendChatMessageAsync(msg);
            }
            else
            {
                string msg = $"/?nick={HttpUtility.UrlEncode(playername)}&msg={HttpUtility.UrlEncode(chatMsg)}&server={server}";
                _ = sendChatMessageAsync(msg);
            }
            

            string logmsg = ($" {player.SteamID} || {playername}: {chatMsg}");
            chatLog(logmsg);
            return HookResult.Continue;
        }
        public async Task sendChatMessageAsync(string msg)
        {
            using (var _httpclient = new HttpClient())
            {
                //Server.PrintToChatAll(msg);
                //msg = $"/?id={kickID}&nick={HttpUtility.UrlEncode(playername)}&msg={HttpUtility.UrlEncode(message)}&server={server}";
                var url = $"{host}{msg}";
                //Server.PrintToChatAll(url);
                var response = await _httpclient.GetAsync(url);
            }
        }
        public async Task callAdminAsync(string msg)
        {
            using (var calladmin = new HttpClient())
            {
                var url = $"{host}{msg}";
                var response = await calladmin.GetAsync(url);
            }

        }
        //public async Task rewardXP(KickPlayer kickplayer)
        //{

        //    string msg = $"/?type=add_server_xp&userid={kickplayer.webData.webID}";
        //    using (var _rewardXp = new HttpClient())
        //    {
        //        var url = $"{host}{msg}";
        //        kickplayer.Controller.PrintToChat(url);
        //        var response = await _rewardXp.GetAsync(url);

        //        //kickplayer.Controller.PrintToChat($" \x04[Kick] \x10Saņēmi\x04 25 \x10XP forumā par spēlēšanu serverī");
        //        //kickplayer.Controller.PrintToChat("done");
        //    }

        //}

        //public void startItemTimer(KickPlayer kickplayer)
        //{
        //    if (!kickplayer.IsPlayer && !kickplayer.IsValid && !kickplayer.webData.hasWeb)
        //        return;
        //    kickplayer.itemTimer = AddTimer(1, () =>
        //    {
        //        kickplayer.webData.itemChance += 1;
        //    }, TimerFlags.REPEAT);
        //}

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
                    _ = sendChatMessageAsync(msg);
                    Server.NextFrame(() => UpdateXpTime(kickplayer));
                }
            }, TimerFlags.REPEAT);

        }

        public void rewardItems(CCSPlayerController player)
        {
            Server.PrintToChatAll("Map End");

            KickPlayer? kickplayer = GetKickPlayer(player!);
            
            if (!kickplayer.IsPlayer)
                return;

            kickplayer.itemTimer.Kill();
            kickplayer.itemTimer = null;

            var award = new
            {
                id = kickplayer.webData.webID,
                chance = kickplayer.webData.itemChance,
            };


        }

        public void OnCommandCalladmin(CCSPlayerController player, CommandInfo info)
        {
            KickPlayer? kickplayer = GetKickPlayer(player!);
            if (!kickplayer.IsPlayer)
                return;
            playername = kickplayer.PlayerName;
            //	[red]Lxst[/red] sauc adminu uz [red]JB[/red] serveri! Parkapejs: [red]Ass[/red] Iemesls: [red]Cheats[/red]
            string calladminmsg = $"[red]{HttpUtility.UrlEncode(playername)}[/red] sauc adminu uz [red]{callAdminServer}[/red] serveri!";
            string msg = $"/?&nick=CALLADMIN&msg={calladminmsg}&server={server}";
            _ = callAdminAsync(msg);
        }

        public void OnCommandWebChat(CCSPlayerController player, CommandInfo info)
        {
            if (!CommandHelper(player, info, CommandUsage.SERVER_ONLY, 2, "<name> <msg>", permission: "@css/rcon"))
                return;
            string name = info.GetArg(1);
            string msg = info.GetArg(2);

            Server.PrintToChatAll($" \x04[WEB] \x0F{name} \x10: {msg}");


        }

        public void OnCommandWeb(CCSPlayerController player, CommandInfo info)
        {
            KickPlayer? kp = GetKickPlayer(player!);
            info.ReplyToCommand($"web: {kp.webData.hasWeb.ToString()}");
            info.ReplyToCommand(kp.webData.webID.ToString());
            info.ReplyToCommand(kp.webData.webNick);
            info.ReplyToCommand(kp.webData.lvl.ToString());
            info.ReplyToCommand(kp.webData.xp.ToString());
            info.ReplyToCommand(kp.webData.xpTime.ToString());
        }

        //////////////////////////////
        //          CHATLOG         //
        //////////////////////////////
        public void chatLog(string logmsg)
        {
            LogPath = Path.Combine(_ModuleDirectory, "../../logs/kick/");
            FileName = ("chat-") + DateTime.Now.ToString("MM-dd-yyyy") + (".txt");
            FilePath = Path.Combine(_ModuleDirectory, "../../logs/kick/") + ($"{FileName}");

            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }

            if (!File.Exists(FilePath))
            {
                File.Create(FilePath);
            }

            using (StreamWriter writer = File.AppendText(FilePath))
            {
                writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + logmsg);
            }
        }
    }
}
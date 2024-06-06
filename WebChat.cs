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
        public readonly string kicklv = "kick.lv";
        public readonly int kicklvid = 31;
        public readonly string callAdminServer = "CS2PUB";
        private int xpTime = 3600;

        public string playername;
        public string message;
        private int kickID;
        private string msg;

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

                _ = sendChatMessageAsync(kicklvid, kicklv, $"[green]{HttpUtility.UrlEncode(player.PlayerName)}[/green] Pievienojās serverim");
                return HookResult.Continue;
            });

            RegisterEventHandler((EventPlayerDisconnect @event, GameEventInfo info) =>
            {
                CCSPlayerController? player = @event.Userid;

                if (player is null || player.IsBot || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV)
                    return HookResult.Continue;


                _ = sendChatMessageAsync(kicklvid, kicklv, msg = $"[blue]{HttpUtility.UrlEncode(player.PlayerName)}[/blue] izgāja no servera");
                return HookResult.Continue;
            });

            RegisterListener<Listeners.OnMapStart>((mapName) =>
            {

                _ = sendChatMessageAsync(kicklvid, kicklv, msg = $"Karte nomainījās uz [blue]{HttpUtility.UrlEncode(mapName)}[/blue]");
            });
        }
        public HookResult OnPlayerSayPublic(CCSPlayerController? player, CommandInfo info)
        {
            message = info.GetArg(1);

            if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV || message == null)
                return HookResult.Continue;

            KickPlayer? kickplayer = GetKickPlayer(player!);
            WebData? webData = kickplayer.webData;
            kickID = webData.webID;
            playername = player.PlayerName;
            if (message.StartsWith("/"))
                return HookResult.Continue;
            if (message.StartsWith("!"))
                return HookResult.Continue;
            if (string.IsNullOrWhiteSpace(message))
                return HookResult.Continue;

            _ = sendChatMessageAsync(kickID, playername, message);

            string logmsg = ($" {player.SteamID} || {playername}: {message}");
            chatLog(logmsg);
            return HookResult.Continue;
        }
        public async Task sendChatMessageAsync(int kickID, string playername, string message)
        {
            using (var _httpclient = new HttpClient())
            {
                msg = $"/?id={kickID}&nick={HttpUtility.UrlEncode(playername)}&msg={HttpUtility.UrlEncode(message)}&server={server}";
                var url = $"{host}{msg}";
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
        public async Task rewardXP(KickPlayer kickplayer)
        {

            string msg = $"/?type=add_server_xp&userid={kickplayer.webData.webID}";
            using (var _rewardXp = new HttpClient())
            {
                var url = $"{host}{msg}";
                kickplayer.Controller.PrintToChat(url);
                var response = await _rewardXp.GetAsync(url);

                //kickplayer.Controller.PrintToChat($" \x04[Kick] \x10Saņēmi\x04 25 \x10XP forumā par spēlēšanu serverī");
                //kickplayer.Controller.PrintToChat("done");
            }

        }

        public void startTimer(KickPlayer kickplayer)
        {
            if (!kickplayer.IsPlayer)
                return;

            if (!kickplayer.webData.hasWeb)
                return;

            kickplayer.timer = AddTimer(10, () =>
            {
                kickplayer.webData.xpTime += 1800;
                if (kickplayer.webData.xpTime == xpTime)
                {
                    kickplayer.webData.xpTime = 0;
                    Server.NextFrame(() => rewardXP(kickplayer));
                    Server.NextFrame(() => UpdateXpTime(kickplayer));
                }
            }, TimerFlags.REPEAT);

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
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

    public sealed partial class Plugin : BasePlugin
    {
        public readonly string host = "http://127.0.0.1:9080";
        public readonly string server = "ttt";
        public readonly string kicklv = "kick.lv";
        public readonly int kicklvid = 31;
        

        public string playername;
        public string message;
        private int kickid;
        private string msg;

        private string? LogPath;
        private string? FileName;
        private string? FilePath;

        public void Initialize_Chat()
        {
            //AddCommand("css_web", "web test", OnCommandWeb);
            AddCommand("webchat", "Send message from web", OnCommandWebChat);
            //AddCommand("css_calladmin", "Izsauc adminu", OnCommandCalladmin);
            AddCommandListener("say", OnPlayerSayPublic, HookMode.Post);

            RegisterEventHandler((EventPlayerConnectFull @event, GameEventInfo info) =>
            {
                CCSPlayerController? player = @event.Userid;

                if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV)
                    return HookResult.Continue;

                _ = sendChatMessageAsync(kicklvid, kicklv, $"[green]{player.PlayerName}[/green] Pievienojās serverim");
                    return HookResult.Continue;
            });

            RegisterEventHandler((EventPlayerDisconnect @event, GameEventInfo info) =>
            {
                CCSPlayerController? player = @event.Userid;

                if (player is null || player.IsBot || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV)
                    return HookResult.Continue;

                
                _ = sendChatMessageAsync(kicklvid, kicklv, msg = $"[blue]{player.PlayerName}[/blue] izgāja no servera");
                return HookResult.Continue;
            });

            RegisterListener<Listeners.OnMapStart>((mapName) =>
            {
            
            _ = sendChatMessageAsync(kicklvid, kicklv, msg = $"Karte nomainījās uz [blue]{mapName}[/blue]");
            });
        }
        public HookResult OnPlayerSayPublic(CCSPlayerController? player, CommandInfo info)
        {
            message = info.GetArg(1);

            if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV || message == null)
                return HookResult.Continue;

            KickPlayer? kickplayer = GetKickPlayer(player!);
            WebData? webData = kickplayer.webData;
            kickid = webData.webID;
            playername = player.PlayerName;
            if (message.StartsWith("/"))
                return HookResult.Continue;
            if (message.StartsWith("!"))
                return HookResult.Continue;
            if (string.IsNullOrWhiteSpace(message)) 
                return HookResult.Continue;
            
            _ = sendChatMessageAsync(kickid, playername, message);
            
            string logmsg = ($" {player.SteamID} || {playername}: {message}");
            chatLog(logmsg);
            return HookResult.Continue;
        }
        public async Task sendChatMessageAsync(int kickid, string playername, string message)
        {
            using (var _httpclient = new HttpClient())
            {
                msg = $"/?id={kickid}&nick={HttpUtility.UrlEncode(playername)}&msg={HttpUtility.UrlEncode(message)}&server={HttpUtility.UrlEncode(server)}";
                var url = $"{host}{msg}";
                var response = await _httpclient.GetAsync(url);
            }
        }

        //public async Task callAdminAsync(int kickid, string playername, string message, bool hasWeb)
        //{
        //    using (var _httpclient = new HttpClient())
        //    {
        //        if (hasWeb)
        //        {
        //            msg =
        //                $"/?id={kickid}&nick={HttpUtility.UrlEncode(playername)}&msg={HttpUtility.UrlEncode(message)}&server={HttpUtility.UrlEncode(server)}";
        //        }
        //        else
        //        {
        //            msg =
        //                $"/?nick={HttpUtility.UrlEncode(playername)}&msg={HttpUtility.UrlEncode(message)}&server={HttpUtility.UrlEncode(server)}";
        //        }

        //        var url = $"{host}{msg}";
        //        var response = await _httpclient.GetAsync(url);
        //    }
        //}
        public void OnCommandWebChat(CCSPlayerController player, CommandInfo info)
        {
            if(!CommandHelper(player, info, CommandUsage.SERVER_ONLY, 2,"<name> <msg>", permission: "@css/rcon"))
               return;
            string name = info.GetArg(1);
            string msg = info.GetArg(2);
            
            Server.PrintToChatAll($" \x04[WEB] \x0F{name} \x10: {msg}");
            
            
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
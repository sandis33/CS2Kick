using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using Kick.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Kick.ModuleWeb;
using System.Web;

namespace Kick
{
    public partial class KickCS2
    {
        public void ListenerOnMapStartHandler(string MapName)
        {
            MaxPlayers = NativeAPI.GetMaxClients();
            Map = NativeAPI.GetMapName();
            string msg =
                $"/?nick=SERVER&msg={HttpUtility.UrlEncode($"Karte nomainījās uz [blue]{Map}[/blue]")}&server={server}";
            _ = WebGetReqAsync(msg);
            KickPlayers.Clear();
            AddTimer(60.0F, () =>
            {
                if (playersOn == 0)
                {
                    Server.ExecuteCommand($"mp_warmup_end");
                }
            });
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult EventPlayerActivate(EventPlayerActivate @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;

            if (player is null || !player.IsValid || player.IsBot || !player.PlayerPawn.IsValid || player.IsHLTV)
                return HookResult.Continue;

            if (KickPlayers.Any(p => p.Controller == player))
                return HookResult.Continue;

            KickPlayer kickplayer = new(player);

            WebData? WebData = null;
            WebData = new WebData
            {
                hasWeb = false,
                itemChance = 0,
            };

            kickplayer.WebData = WebData;

            Task.Run(async () => await LoadWebDataAsync(kickplayer)).Wait();
            KickPlayers.Add(kickplayer);
            return HookResult.Continue;
        }
        [GameEventHandler(HookMode.Post)]
        public HookResult EventPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            string name;

            CCSPlayerController? player = @event.Userid;

            if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV)
                return HookResult.Continue;

            string msg =
                $"/?nick=SERVER&msg={HttpUtility.UrlEncode($"[green]{player.PlayerName}[/green] pievienojās serverim")}&server={server}";
            _ = WebGetReqAsync(msg);
            //chatLog($"{player.PlayerName} pievienojās serverim");

            KickPlayer? kickplayer = GetKickPlayer(player!);
            if (kickplayer is null)
                return HookResult.Continue;
            startItemTimer(kickplayer);
            if (kickplayer.WebData.hasWeb)
            {
                Server.NextFrame(async () => await SetWebStatusAsync(kickplayer));
                Server.NextFrame(async () => await ReloadWebDataAsync(kickplayer));
                Server.NextFrame(() => startXpTimer(kickplayer));

                if (kickplayer.WebData.webName.Length > 1)
                {
                    name = kickplayer.WebData.webName;
                }
                else
                {
                    name = kickplayer.WebData.webNick;
                }
                kickplayer.Controller.PrintToChat($" \x04[Kick] \x0AČau \x04{name} \x0ATev šobrīd ir \x04{kickplayer.WebData.lvl} \x0Alīmenis");
            }
            else
            {
                kickplayer.Controller.PrintToChat($" \x04[Kick] \x0AČau \x04{kickplayer.PlayerName} \x0ASeko līdzi jaunumiem un uzzini svarīgu informāciju mūsu mājaslapā \x04www.kick.lv");
            }
            kickplayer.Controller.PrintToChat($" \x04[Kick] \x0ARaksti \x04!help \x0Alai redzētu pieejamās komandas!");
            return HookResult.Continue;
        }
        [GameEventHandler(HookMode.Post)]
        public HookResult EventRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            switch (@event.Winner)
            {
                case 0:
                    break;
                case 1:
                    break;
                case 2:
                    tsWins++;
                    break;
                case 3:
                    ctWins++;
                    break;
            }

            Task.Run(SaveAllPlayersDataAsync);
            return HookResult.Continue;
        }
        [GameEventHandler(HookMode.Post)]
        public HookResult EventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;

            if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV || player.IsBot)
                return HookResult.Continue;
            KickPlayer? kickplayer = GetKickPlayer(player!);

            if (kickplayer.WebData.hasWeb)
            {
                Server.NextFrame(async () => await UpdateXpTime(kickplayer));
                kickplayer.XPtimer?.Kill();
            }
            kickplayer.itemTimer?.Kill();

            string msg =
                $"/?nick=SERVER&msg={HttpUtility.UrlEncode($"[blue]{player.PlayerName}[/blue] izgāja no servera")}&server={server}";
            _ = WebGetReqAsync(msg);
            //chatLog($"{player.PlayerName} izgāja no servera");
            return HookResult.Continue;
        }
    }
}

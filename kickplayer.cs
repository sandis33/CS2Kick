using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Kick.ModuleWeb;


namespace Kick.Player
{
    public class KickPlayer
    {
        public readonly CCSPlayerController Controller;
        public readonly ulong SteamID;
        public readonly string SteamID2;
        public readonly string PlayerName;
        public CounterStrikeSharp.API.Modules.Timers.Timer? XPtimer = null;
        public CounterStrikeSharp.API.Modules.Timers.Timer? itemTimer = null;

        //** ? Data */
        public WebData? webData { get; set; }

        public KickPlayer(CCSPlayerController playerController)
        {
            Controller = playerController;
            SteamID = playerController.SteamID;
            SteamID2 = playerController.AuthorizedSteamID.SteamId2;
            PlayerName = playerController.PlayerName;
        }

        public bool IsValid
        {
            get
            {
                return Controller?.IsValid == true && Controller.PlayerPawn?.IsValid == true && Controller.Connected == PlayerConnectedState.PlayerConnected;
            }
        }

        public bool IsPlayer
        {
            get
            {
                return !Controller.IsBot && !Controller.IsHLTV;
            }
        }

    }
}
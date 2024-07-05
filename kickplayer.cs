using CounterStrikeSharp.API.Core;
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
        public CounterStrikeSharp.API.Modules.Timers.Timer? CDTimer = null;

        public WebData WebData { get; set; }

        public KickPlayer(CCSPlayerController playerController)
        {
            Controller = playerController;
            SteamID = playerController.SteamID;
            PlayerName = playerController.PlayerName;
            if (playerController.AuthorizedSteamID == null)
                throw new ArgumentNullException(nameof(playerController.AuthorizedSteamID), "AuthorizedSteamID cannot be null");
            SteamID2 = playerController.AuthorizedSteamID.SteamId2;
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

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using Kick.Player;
using static Kick.ModuleWeb;
using static CounterStrikeSharp.API.Core.Listeners;
using System.Numerics;
using System.Xml.Linq;


namespace Kick;
public partial class Plugin : BasePlugin
{
    public required string _ModuleDirectory { get; set; }

    public List<KickPlayer> KickPlayers = new List<KickPlayer>();

    public override void Load(bool hotReload)
    {
        _ModuleDirectory = ModuleDirectory;

        Initialize_Chat();
        Initialize_Events();

    }
    public override void Unload(bool hotReload)
    {
        Task.Run(SaveAllPlayersDataAsync);

        this.Dispose();
    }
    public void Initialize_Events()
    {
        // bot id = 31 , nick kick.lv
        RegisterEventHandler<EventCsWinPanelMatch>(EventCsWinPanelMatchHandler);
        RegisterEventHandler((EventPlayerActivate @event, GameEventInfo info) =>
        {
            CCSPlayerController? player = @event.Userid;
            string name;
            if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV)
                return HookResult.Continue;

            KickPlayer kickplayer = new KickPlayer(player);
            if (player.IsBot)
            {
                KickPlayers.Add(kickplayer);
                return HookResult.Continue;
            }

            WebData? webData = null;
            webData = new WebData
            {
                hasWeb = false,
                webID = 0,
                webNick = "",
                webName = "",
                xp = 0,
                lvl = 0,
                xpTime = 0,
                itemChance = 0,
            };

            kickplayer.webData = webData;

            Task.Run(async () => await LoadWebDataAsync(kickplayer)).Wait();

            KickPlayers.Add(kickplayer);

            return HookResult.Continue;
        });

        RegisterEventHandler((EventPlayerConnectFull @event, GameEventInfo info) =>
        {
            CCSPlayerController? player = @event.Userid;
            string name;
            if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV)
                return HookResult.Continue;
            KickPlayer? kickplayer = GetKickPlayer(player!);

            if (kickplayer.webData.hasWeb)
            {
                Server.NextFrame(() => SetWebStatusAsync(kickplayer));
                Server.NextFrame(() => ReloadWebDataAsync(kickplayer));
                Server.NextFrame(() => startXpTimer(kickplayer));

                if (kickplayer.webData.webName.Length > 1)
                {
                    name = kickplayer.webData.webName;
                }
                else
                {
                    name = kickplayer.webData.webNick;
                }
                kickplayer.Controller.PrintToChat($" \x04[Kick] \x0AČau \x04{name} \x0ATev šobrīd ir \x04{kickplayer.webData.lvl} \x0Alīmenis");
            }
            else
            {
                kickplayer.Controller.PrintToChat($" \x04[Kick] \x0AČau \x04{kickplayer.PlayerName} \x0ASeko līdzi jaunumiem un uzzini svarīgu informāciju mūsu mājaslapā \x04www.kick.lv");
            }

            return HookResult.Continue;
        });

        RegisterEventHandler((EventPlayerDisconnect @event, GameEventInfo info) =>
        {
            CCSPlayerController player = @event.Userid;

            if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV || player.IsBot)
                return HookResult.Continue;
            KickPlayer? kickplayer = GetKickPlayer(player!);

            if (kickplayer.webData.hasWeb)
            {
                Server.NextFrame(() => UpdateXpTime(kickplayer));
                kickplayer.XPtimer?.Kill();
            }

            return HookResult.Continue;
        });
        RegisterEventHandler((EventRoundEnd @event, GameEventInfo info) =>
        {
            Task.Run(SaveAllPlayersDataAsync);
            return HookResult.Continue;
        });
    }
    public bool CommandHelper(CCSPlayerController? player, CommandInfo info, CommandUsage usage, int argCount = 0, string? help = null, string? permission = null)
    {
        switch (usage)
        {
            case CommandUsage.CLIENT_ONLY:
                if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
                {
                    info.ReplyToCommand($"Klienta komanda");
                    return false;
                }
                break;
            case CommandUsage.SERVER_ONLY:
                if (player != null)
                {

                    return false;
                }
                break;
            case CommandUsage.CLIENT_AND_SERVER:
                if (!(player == null || (player != null && player.IsValid && player.PlayerPawn.Value != null)))
                    return false;
                break;
        }
        return true;
    }
    public KickPlayer? GetKickPlayer(CCSPlayerController? playerController)
    {
        return KickPlayers.ToList().FirstOrDefault(player => player.Controller == playerController);
    }
}

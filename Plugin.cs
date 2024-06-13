using System.Web;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Kick.Player;
using static Kick.ModuleWeb;

// bot id = 31 , nick kick.lv
namespace Kick;
public partial class Plugin : BasePlugin
{
    public required string _ModuleDirectory { get; set; }

    public List<KickPlayer> KickPlayers = new List<KickPlayer>();
    
    public override void Load(bool hotReload)
    {
        _ModuleDirectory = ModuleDirectory;
        Initialize_SharedApi();
        Initialize_Chat();
        Initialize_Events();
    }

    public void Initialize_Events()
    {
        RegisterListener<Listeners.OnMapStart>(ListenerOnMapStartHandler);
    }

    public void ListenerOnMapStartHandler(string MapName)
    {
        maxPlayers = NativeAPI.GetMaxClients();
        map = NativeAPI.GetMapName();
        string msg =
            $"/?nick=SERVER&msg={HttpUtility.UrlEncode($"Karte nomainījās uz [blue]{map}[/blue]")}&server={server}";
        _ = WebGetReqAsync(msg);
        KickPlayers.Clear();
        chatLog($"Karte nomainījās uz {map}");
    }
    [GameEventHandler(HookMode.Post)]
    public HookResult EventPlayerActivate(EventPlayerActivate @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player is null || !player.IsValid || player.IsBot || !player.PlayerPawn.IsValid || player.IsHLTV)
            return HookResult.Continue;

        if (KickPlayers.Any(p => p.Controller == player))
            return HookResult.Continue;

        KickPlayer kickplayer = new KickPlayer(player);

        WebData? webData = null;
        webData = new WebData
        {
            hasWeb = false,
            itemChance = 0,
        };

        kickplayer.webData = webData;

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
        chatLog($"{player.PlayerName} pievienojās serverim");

        KickPlayer? kickplayer = GetKickPlayer(player!);

        startItemTimer(kickplayer);
        if (kickplayer.webData.hasWeb)
        {
            Server.NextFrame(async () => await SetWebStatusAsync(kickplayer));
            Server.NextFrame(async () => await ReloadWebDataAsync(kickplayer));
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

        if (kickplayer.webData.hasWeb)
        {
            Server.NextFrame(async () => await UpdateXpTime(kickplayer));
            kickplayer.XPtimer?.Kill();
        }
        kickplayer.itemTimer?.Kill();

        string msg =
            $"/?nick=SERVER&msg={HttpUtility.UrlEncode($"[blue]{player.PlayerName}[/blue] izgāja no servera")}&server={server}";
        _ = WebGetReqAsync(msg);
        chatLog($"{player.PlayerName} izgāja no servera");
        return HookResult.Continue;
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

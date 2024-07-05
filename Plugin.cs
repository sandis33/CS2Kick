using System.Web;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using Kick.Player;
using static Kick.ModuleWeb;

// bot id = 31 , nick kick.lv
namespace Kick;
public partial class KickCS2
{
    public List<KickPlayer> KickPlayers = [];

    public static KickCS2 Instance { get; private set; } = new();
    public override void Load(bool hotReload)
    {
        Initialize_SharedApi();
        InitializeCommands();
        InitializeChat();
        RegisterListener<Listeners.OnMapStart>(ListenerOnMapStartHandler);
        AddCommandListener("say", OnPlayerSayPublic, HookMode.Post);
    }
    public override void Unload(bool hotReload)
    {
        Task.Run(SaveAllPlayersDataAsync);

        this.Dispose();
    }
    
    public KickPlayer? GetKickPlayer(CCSPlayerController? playerController)
    {
        return KickPlayers.ToList().FirstOrDefault(player => player.Controller == playerController);
    }
}

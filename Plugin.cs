
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


namespace Kick;
public partial class Plugin : BasePlugin/*, IPluginConfig<PluginConfig>*/
{
    //public required PluginConfig Config { get; set; } = new PluginConfig();
    public required string _ModuleDirectory { get; set; }

    public List<KickPlayer> KickPlayers = new List<KickPlayer>();

    //public void OnConfigParsed(PluginConfig config)
    //{
    //    this.Config = config;
    //}
    public override void Load(bool hotReload)
    {
        _ModuleDirectory = ModuleDirectory;

        Initialize_Chat();
        Initialize_Events();
        
    }

    public void Initialize_Events()
    {
        string kicklv = "kick.lv";
        string kicklvid = "31";
        string msg;
        //RegisterEventHandler((EventPlayerConnectFull @event, GameEventInfo info) =>
        //{
        //    CCSPlayerController? player = @event.Userid;
        //    if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV)
        //        return HookResult.Continue;
        //    KickPlayer kickplayer = GetKickPlayer(player);
        //    if (kickplayer.webData.hasWeb == true)
        //    {
        //        Server.NextFrame(() => SetWebStatusAsync(kickplayer));
        //    }
        //    return HookResult.Continue;
        //});
        RegisterEventHandler((EventPlayerActivate @event, GameEventInfo info) =>
        {
            CCSPlayerController? player = @event.Userid;

            if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV)
                return HookResult.Continue;
            
            if (KickPlayers.Any(p => p.Controller == player))
            {
                KickPlayer kplayer = GetKickPlayer(player);
                if(kplayer.webData.hasWeb)
                {
                    Server.NextFrame(() => SetWebStatusAsync(kplayer));
                }
                return HookResult.Continue;
            }

            
            KickPlayer kickplayer = new KickPlayer(player);
            if (player.IsBot)
            {
                KickPlayers.Add(kickplayer);
                return HookResult.Continue;
            }
            WebData? webData = null;
            webData = new WebData
            {
                webID = 0,
                webName = "",
                xp = 0,
                lvl = 0,
                hasWeb = false,
            };
            kickplayer.webData = webData;
            KickPlayers.Add(kickplayer);

            Task.Run(() => LoadWebDataAsync(kickplayer));

        
            return HookResult.Continue;
        });

        RegisterEventHandler((EventPlayerDisconnect @event, GameEventInfo info) =>
        {
            CCSPlayerController player = @event.Userid;

            if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsHLTV)
                return HookResult.Continue;
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

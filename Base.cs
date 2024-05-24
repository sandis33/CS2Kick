
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using static CounterStrikeSharp.API.Core.Listeners;


namespace CS2Kick;
public sealed partial class Plugin : BasePlugin
{
    public required string _ModuleDirectory { get; set; }


    public override void Load(bool hotReload)
    {
        _ModuleDirectory = ModuleDirectory;

        AddCommandListener("say", OnPlayerSayPublic, HookMode.Post);
        
    }
}

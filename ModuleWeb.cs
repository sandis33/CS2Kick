using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core.Plugin;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Kick.Player;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;


namespace Kick
{
    public partial class ModuleWeb
    {
        
        public ModuleWeb(ILogger<ModuleWeb> logger, IPluginContext pluginContext)
        {
            this.Logger = logger;
            this.PluginContext = (pluginContext as PluginContext);
        }

        public Timer? reservePlayTimeTimer = null;

        public void Initialize(bool hotReload)
        {
            
            Plugin plugin = (this.PluginContext.Plugin as Plugin)!;
            this.Config = plugin.Config;
            this.Logger.LogInformation("Initializing '{0}'", this.GetType().Name);

        }
        public void Release(bool hotReload)
        {
            this.Logger.LogInformation("Releasing '{0}'", this.GetType().Name);
        }
    }
}


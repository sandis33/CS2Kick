using CounterStrikeSharp.API.Core.Plugin;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Kick
{
    public partial class ModuleWeb
    {
        public class WebData
        {
            public required int webID { get; set; }
            public required int lvl { get; set; }
            public required int xp { get; set; }
            public required string webName { get; set; }
            public required bool hasWeb { get; set; }
        }
        public required Plugin plugin;
        public readonly ILogger<ModuleWeb> Logger;

        //public required PluginConfig Config;
        public readonly IPluginContext PluginContext;
    }
}

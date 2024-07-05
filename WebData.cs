using Microsoft.Extensions.Logging;

namespace Kick
{
    public partial class ModuleWeb
    {
        public class WebData
        {
            public required bool hasWeb { get; set; }
            public int webID { get; set; }
            public string webNick { get; set; }
            public string webName { get; set; }
            public int lvl { get; set; }
            public int xp { get; set; }
            public int xpTime { get; set; }
            public required int itemChance { get; set; }
            public string username_seo { get; set; }
    }
        public required KickCS2 plugin;
        public readonly ILogger<ModuleWeb> Logger;
    }
}
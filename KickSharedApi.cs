using KickSharedApi;
using CounterStrikeSharp.API.Core.Capabilities;

namespace Kick
{
    public sealed partial class Plugin
    {
        public static PluginCapability<IKickSharedApi> Capability_KickSharedAPI { get; } = new("Kick:sharedapi");

        public void Initialize_SharedApi()
        {
            Capabilities.RegisterPluginCapability(Capability_KickSharedAPI, () => new KickSharedApiHandler(this));
        }
        public class KickSharedApiHandler : IKickSharedApi
        {
            public Plugin plugin;

            public KickSharedApiHandler(Plugin plugin)
            {
                this.plugin = plugin;
            }

            public void _rewardItems()
            {
                this.plugin.rewardItems();
            }
            public void _WebGetReq(string msg)
            {
                this.plugin.SharedGetReq(msg);
            }
        }
    }
}

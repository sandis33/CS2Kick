using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

namespace Kick
{
    public partial class Plugin : BasePlugin
    {
        public static BaseMenu CreateMenu(string title)
        {
            new CenterHtmlMenu(title);
        }

        public static void OpenMenu(CCSPlayerController player, BaseMenu menu)
        {
            CenterHtmlMenu centerHtmlMenu;
            MenuManager.OpenCenterHtmlMenu(player, centerHtmlMenu);
        }

        public static void OpenMenu(CCSPlayerController player)
        {
            var menu = CreateMenu("CallAdmin");
            
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using Kick.Player;

namespace Kick
{
    public partial class KickCS2
    {
        public class MenuOptionData(string name, Action action)
        {
            public readonly string Name = name;
            public readonly Action Action = action;
        }
        public static BaseMenu CreateMenu(string title)
        {
            return new CenterHtmlMenu(title, KickCS2.Instance);
        }

        public static void OpenMenu(CCSPlayerController player, BaseMenu menu)
        {
            switch (menu)
            {
                case CenterHtmlMenu centerHtmlMenu:
                    MenuManager.OpenCenterHtmlMenu(KickCS2.Instance, player, centerHtmlMenu);
                    break;
                case ChatMenu chatMenu:
                    MenuManager.OpenChatMenu(player, chatMenu);
                    break;
            }
        }
        
        public static void OpenCallAdminPlayerMenu(CCSPlayerController caller)
        {
            var menu = CreateMenu("Spēlētājs / Player");

            foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
            {
                menu.AddMenuOption(p.PlayerName, (_, _) => { CallAdminReasonMenu(caller, p.PlayerName); });
            }
            OpenMenu(caller, menu);
        }
        public static void CallAdminReasonMenu(CCSPlayerController caller, string target)
        {
            if (!caller.IsValid)
                return;         

            var menu = CreateMenu("Iemesls / Reason");
            List<MenuOptionData> options =
            [
                new MenuOptionData("Cheats", () => CallAdmin(caller, target, "Cheats")),
                new MenuOptionData("Map Bug", () => CallAdmin(caller, target, "Map Bug")),
                new MenuOptionData("C4 Bug", () => CallAdmin(caller, target, "C4 Bug")),
                new MenuOptionData("Sadarbošanās / Teaming", () => CallAdmin(caller, target, "Sadarbošanās")),
                new MenuOptionData("Lamāšanās / Swearing", () => CallAdmin(caller, target, "Lamāšanās")),
                new MenuOptionData("Reklāma / Advertising", () => CallAdmin(caller, target, "Reklāma")),              
                new MenuOptionData("ACC savā labā / ACC Abuse", () => CallAdmin(caller, target, "ACC savā labā")),
                new MenuOptionData("Cits / Other", () => CallAdmin(caller, target, "Cits")),
            ];

            foreach (var menuOptionData in options)
            {
                var menuName = menuOptionData.Name;
                menu.AddMenuOption(menuName, (_, _) => { menuOptionData.Action.Invoke(); MenuManager.CloseActiveMenu(caller);});
            }

            OpenMenu(caller, menu);
        }
        public static void CallAdmin(CCSPlayerController caller, string target, string reason)
        {
            if(caller == null || target == null)
                return;
            caller.PrintToChat($" \x04[Kick]\x10 Izsaukums pēc admina nosūtīts, paldies un gaidi adminu!");
            caller.PrintToChat($" \x04[Kick]\x0F LASI ČATU UN NEEJ ĀRĀ NO SERVERA NĀKAMĀS 10 MINŪTES.");
            caller.PrintToChat($" \x04[Kick]\x0F ADMINS var uzdot jautājumus calladmin sakarā un tev ir pienākums viņam atbildēt!");
            string callerName = caller.PlayerName;
            string calladminmsg = $"[red]{HttpUtility.UrlEncode(callerName)}[/red] sauc adminu uz [red]{callAdminServer}[/red] serveri! Pārkāpējs: [red]{HttpUtility.UrlEncode(target)}[/red] Iemesls: [red]{reason}[/red]";
            string msg = $"/?&nick=CALLADMIN&msg={calladminmsg}&server=forums";
            _ = WebGetReqAsync(msg);
        }
    }
}

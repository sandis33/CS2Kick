using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kick.Player;
using Newtonsoft.Json;
using System.Web;

namespace Kick
{
    public partial class KickCS2
    {
        public void InitializeCommands()
        {
            AddCommand("webchat", "Send message from web", OnCommandWebChatRcon);
            AddCommand("web_pm_say", "Sends pm to player from web", OnCommadWebPmSay);
            AddCommand("css_calladmin", "Makes a call for admin", OnCommandCalladmin);
            AddCommand("css_help", "List available commands", OnCommandHelp);                         
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public static void OnCommandHelp(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid)
                return;
            info.ReplyToCommand($" \x0A[\x04Kick.lv\x0A] \x04Pieejamās komandas");
            info.ReplyToCommand($" --- \x0AStatu komadas: \x04!rank !top !time");
            info.ReplyToCommand($" --- \x0AVisi pieejamie rangi: \x04!ranks");
            info.ReplyToCommand($" --- \x0APaziņojumi par punktu izmaiņām: \x04!points");
            info.ReplyToCommand($" --- \x0AIzsaukt administratoru \x04!calladmin");
            info.ReplyToCommand($" --- \x0ARTV saistītās komandas \x04!rtv !nominate !timeleft !nextmap");            
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnCommandCalladmin(CCSPlayerController? player, CommandInfo info)
        {
            if (player is null)
                return;
            KickPlayer? kickplayer = GetKickPlayer(player!);
            if (kickplayer is null)
                return;
            if (kickplayer.CDTimer != null)
            {
                player.PrintToChat($" \x04[Kick]\x10 Tu jau nosūtiji izsaukumu pēc administratora!");
                return;
            }
            OpenCallAdminPlayerMenu(player);
            kickplayer.CDTimer = AddTimer(60, () =>
            {
                kickplayer.CDTimer?.Kill();
                kickplayer.CDTimer = null;
            });
        }

        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public static void OnCommandWebChatRcon(CCSPlayerController? player, CommandInfo info)
        {
            string name = info.GetArg(1);
            string msg = info.GetArg(2);
            Server.PrintToChatAll($" \x04[WEB] \x0F{name} \x10: {msg}");
        }

        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnCommadWebPmSay(CCSPlayerController? player, CommandInfo info)
        {
            if (info.GetArg(1) == null || info.GetArg(1).StartsWith(' ') || info.GetArg(2) == null || info.GetArg(2).StartsWith(' '))
                return;
            int Id = int.Parse(info.GetArg(1));
            string msg = info.GetArg(2);         
            foreach (var kickplayer in KickPlayers)
            {
                if (kickplayer is null)
                    continue;
                if (!kickplayer.WebData.hasWeb)
                    continue;
                if (kickplayer.WebData.webID == Id)
                {
                    kickplayer.Controller.PrintToChat($" \x0F[kick.lv] \x04: {msg}");
                }
            }
        }

        //public void OnCommandTestGet(CCSPlayerController pl, CommandInfo info)
        //{
        //    UserList userlist = new UserList
        //    {
        //        users = new List<User>(),
        //        guests = new List<Guest> { }
        //    };
        //    foreach (var player in Utilities.GetPlayers())
        //    {
        //        KickPlayer? kickplayer = GetKickPlayer(player!);
        //        if (!kickplayer.IsValid || !kickplayer.IsPlayer)
        //            continue;
        //        if (kickplayer.WebData.hasWeb)
        //        {
        //            User newUser = new User
        //            {
        //                Id = kickplayer.WebData.webID,
        //                chance = kickplayer.WebData.itemChance
        //            };
        //            userlist.users.Add(newUser);
        //            kickplayer.WebData.itemChance = 60;
        //        }
        //    }

        //    foreach (var player in Utilities.GetPlayers())
        //    {
        //        KickPlayer? kickplayer = GetKickPlayer(player!);
        //        if (!kickplayer.IsValid || !kickplayer.IsPlayer)
        //            continue;
        //        if (!kickplayer.WebData.hasWeb)
        //        {
        //            Guest newGuest = new Guest
        //            {
        //                name = HttpUtility.UrlEncode(kickplayer.PlayerName),
        //                chance = kickplayer.WebData.itemChance
        //            };
        //            userlist.guests.Add(newGuest);
        //            kickplayer.WebData.itemChance = 60;
        //        }
        //    }

        //    var usersJson = JsonConvert.SerializeObject(userlist);
        //    string type = "/?type=award_item_winners&server=ttt&list=";
        //    var msg = $"{type}{usersJson}";
        //    Server.PrintToChatAll(msg);
        //    _ = WebGetReqAsync(msg);
        //}
        //public void OnCommandWeb(CCSPlayerController player, CommandInfo info)
        //{
        //    KickPlayer? kp = GetKickPlayer(player!);
        //    if (kp is null)
        //        return;
        //    info.ReplyToCommand($"web: {kp.WebData.hasWeb}");
        //    info.ReplyToCommand(kp.WebData.webID.ToString());
        //    info.ReplyToCommand(kp.WebData.webNick);
        //    info.ReplyToCommand(kp.WebData.lvl.ToString());
        //    info.ReplyToCommand(kp.WebData.xp.ToString());
        //    info.ReplyToCommand(kp.WebData.xpTime.ToString());
        //    info.ReplyToCommand(kp.PlayerName);
        //    info.ReplyToCommand(kp.SteamID.ToString());
        //}
    }
}

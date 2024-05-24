namespace CS2Kick
{

    using CounterStrikeSharp.API.Core;
    using CS2Kick;
    using Serilog;
    using System.Text;
    using CounterStrikeSharp.API.Modules.Utils;
    using CounterStrikeSharp.API.Modules.Commands;
    using CounterStrikeSharp.API.Modules.Entities;



    public sealed partial class Plugin : BasePlugin
    {




        private string? playername { get; set; }
        private string? steamid { get; set; }
        private string? message { get; set; }



        private string? LogPath;
        private string? FileName;
        private string? FilePath;


        public HookResult OnPlayerSayPublic(CCSPlayerController? player, CommandInfo info)
        {
            message = info.GetArg(1);
            steamid = player.SteamID.ToString();
            playername = player.PlayerName;
            if (string.IsNullOrWhiteSpace(message)) return HookResult.Continue;

            if (player == null || !player.IsValid || player.IsBot || message == null)
                return HookResult.Continue;


            string chatmsg = ($" {steamid} {playername}: {message}");
            //WebChat chatAsync = new WebChat();
            _ = sendMessageAsync($" {player.SteamID} {player.PlayerName}:{message}");
            chatLog(chatmsg);
            return HookResult.Continue;
        }


        public async Task sendMessageAsync(string msg)
        {
            string webhook = "https://discord.com/api/webhooks/1232097682395238420/jrLMM9LN4C6UHCi4EBoj2bjr0AfTSmCQU0PLAB4F1WJXBw8pGZu3rhfYqN5Z4TdY7zyq";
            using (var httpClient = new HttpClient())
            {
                var payload = new
                {
                    content = msg
                };

                var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(webhook, content);

                if (!response.IsSuccessStatusCode)
                {
                    chatLog($"Neizdevās nosūtīt ziņu.: {response.StatusCode}");
                }
            }
        }
        public void chatLog(string msg)
        {
            LogPath = Path.Combine(_ModuleDirectory, "../../logs/kick/");
            FileName = ("chat-") + DateTime.Now.ToString("MM-dd-yyyy") + (".txt");
            FilePath = Path.Combine(_ModuleDirectory, "../../logs/kick/") + ($"{FileName}");

            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }
            if (!File.Exists(FilePath))
            {
                File.Create(FilePath);
            }



            using (StreamWriter writer = File.AppendText(FilePath))
            {
                writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + msg);
            }           
        }











        /*public override void Load(bool hotReload)
        {
            RegisterEventHandler<EventPlayerChat>((@event, info) =>
            {
                CCSPlayerController player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(@event.Userid));
                var playername = player.PlayerName;
                LogPath = Path.Combine(ModuleDirectory, "../../logs/");
                FileName = DateTime.Now.ToString("MM-dd-yyyy") + (".txt");
                FilePath = Path.Combine(ModuleDirectory, "../../logs/") + ($"{FileName}");

                if (player == null || !player.IsValid || player.IsBot || @event.Text == null)
                    return HookResult.Continue;

                string team = "ALL";
                if (@event.Teamonly)
                {

                    switch ((byte)(CsTeam)player.TeamNum)
                    {
                        case (byte)CsTeam.Terrorist:
                            {
                                team = "TERRORIST";
                                break;
                            }
                        case (byte)CsTeam.CounterTerrorist:
                            {
                                team = "COUNTER-TERRORIST";
                                break;
                            }
                        case (byte)CsTeam.Spectator:
                            {
                                team = "SPECTATOR";
                                break;
                            }
                        default:
                            {
                                team = "ALL";
                                break;
                            }
                    }
                }
                var chatmsg = ($"{playername}: {@event.Text} {player.SteamID}");
                //WebChat chatAsync = new WebChat();
                _ = sendMessageAsync(playername, @event.Text, player.SteamID);
                chatLog(chatmsg);
                return HookResult.Continue;
            });//WebChat end
        }

        public async Task sendMessageAsync(string playerName, string message, ulong steamID)
        {
            using (var httpClient = new HttpClient())
            {
                string webhook = "https://discord.com/api/webhooks/1232097682395238420/jrLMM9LN4C6UHCi4EBoj2bjr0AfTSmCQU0PLAB4F1WJXBw8pGZu3rhfYqN5Z4TdY7zyq";
                var embed = new
                {
                    title = playerName,
                    description = $"{message}\n[Steam Profile](https://steamcommunity.com/profiles/{steamID})",
                    color = 16711680,
                    footer = new
                    {
                        text = ("CS2Pub")
                    }
                };

                var payload = new
                {
                    embeds = new[] { embed }
                };

                var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(webhook, content);

                if (!response.IsSuccessStatusCode)
                {
                    chatLog($"Failed to send message to Discord webhook. Status code: {response.StatusCode}");
                }
            }
        }
        public async Task sendMessageAsync(string msg, string playerName, string message)
        {
            string webhook = "https://discord.com/api/webhooks/1232097682395238420/jrLMM9LN4C6UHCi4EBoj2bjr0AfTSmCQU0PLAB4F1WJXBw8pGZu3rhfYqN5Z4TdY7zyq";
            using (var httpClient = new HttpClient())
            {
                var payload = new
                {
                    content = message
                };

                var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(webhook, content);

                if (!response.IsSuccessStatusCode)
                {
                    chatLog($"Failed to send message to Discord webhook. Status code: {response.StatusCode}");
                }
            }
        }*/

    }
}
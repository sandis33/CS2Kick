namespace Kick
{
    using CounterStrikeSharp.API.Core;

    using System.Text.Json.Serialization;
    public sealed class DatabaseSettings
    {
        public string Host { get; set; } = "127.0.0.1";

        public string Username { get; set; } = "kick_cs2";

        public string Database { get; set; } = "kick_web";

        public string Password { get; set; } = "ymMxFd7DC#tBz";

        public int Port { get; set; } = 3306;

        public string Sslmode { get; set; } = "none";

    }

    public sealed class PluginConfig : BasePluginConfig
    {
        public DatabaseSettings DatabaseSettings { get; set; } = new DatabaseSettings();

    }
}
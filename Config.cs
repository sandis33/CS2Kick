namespace CS2Kick
{
    using CounterStrikeSharp.API.Core;

    using System.Text.Json.Serialization;
    public sealed class DatabaseSettings
    {
        [JsonPropertyName("host")]
        public string Host { get; set; } = "localhost";

        [JsonPropertyName("username")]
        public string Username { get; set; } = "root";

        [JsonPropertyName("database")]
        public string Database { get; set; } = "database";

        [JsonPropertyName("password")]
        public string Password { get; set; } = "password";

        [JsonPropertyName("port")]
        public int Port { get; set; } = 3306;

        [JsonPropertyName("sslmode")]
        public string Sslmode { get; set; } = "none";

        [JsonPropertyName("table-prefix")]
        public string TablePrefix { get; set; } = "";
    }

    public sealed class PluginConfig : BasePluginConfig
    {
        [JsonPropertyName("database-settings")]
        public DatabaseSettings DatabaseSettings { get; set; } = new DatabaseSettings();

    }
}
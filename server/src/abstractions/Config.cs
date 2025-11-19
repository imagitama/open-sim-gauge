using System.Text.Json;

namespace OpenGaugeAbstractions
{
    [GenerateMarkdownTable]
    /// <summary>
    /// Configuration of the server application that runs on the host machine.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Which data source to use. The default config uses SimConnect.
        /// </summary>
        public required string Source { get; set; }
        /// <summary>
        /// Override the default IP address and port of the server.
        /// <default>ServerConfig</default>
        /// </summary>
        public ServerConfig Server { get; set; } = new();
        /// <summary>
        /// Override the default poll rate the data source should use (which is also network send rate).
        /// 16.7ms = 60Hz.
        /// </summary>
        public double Rate { get; set; } = 16.7;
        /// <summary>
        /// Log extra output to help diagnose issues.
        /// </summary>
        public bool Debug { get; set; } = false;
        /// <summary>
        /// Arbitrary options provided to the data source. Currently used for configuring the emulator.
        /// </summary>
        public JsonElement? SourceOptions { get; set; }
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// Override the default IP address and port of the server.
    /// </summary>
    public class ServerConfig
    {
        public string IpAddress { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 1234;
    }
}
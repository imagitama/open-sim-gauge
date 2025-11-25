namespace OpenGaugeClient
{
    [GenerateMarkdownTable]
    /// <summary>
    /// Configure the server IP and port.
    /// </summary>
    public class ServerConfig
    {
        public string IpAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 1234;
    }
}
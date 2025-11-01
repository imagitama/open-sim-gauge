using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenGaugeClient
{
    public class Client
    {
        private string _host;
        private int _port;
        private TcpClient _tcp;
        private NetworkStream? _stream;

        public delegate void MessageHandler(ServerMessage<object> message);
        public event MessageHandler? OnMessage;

        public bool IsConnected = false;

        public Client(string? host, int? port)
        {
            if (host != null)
            {
                _host = host;
            }
            else
            {
                _host = "127.0.0.1";
            }

            if (port != null)
            {
                _port = (int)port;
            }
            else
            {
                _port = 1234;
            }

            _tcp = new TcpClient();
        }

        public async Task ConnectAsync()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine($"Connecting to server {_host}:{_port}...");
                    await _tcp.ConnectAsync(_host, _port);
                    _stream = _tcp.GetStream();
                    IsConnected = true;
                    Console.WriteLine("Connected to server");
                    _ = ListenAsync(); // background listener
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Client] Connect failed: {ex.Message}. Retrying in 2 seconds...");
                    await Task.Delay(2000);
                }
            }
        }

        public async Task SendMessage<TPayload>(MessageType type, TPayload payload)
        {
            if (!IsConnected)
                return;

            var message = new ServerMessage<TPayload>
            {
                Type = type,
                Payload = payload
            };

            if (ConfigManager.Debug)
            {
                Console.WriteLine($"[Client] Send message {message.ToString()}");
            }

            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            var json = JsonSerializer.Serialize(message, options) + "\n"; // must have NL
            var bytes = Encoding.UTF8.GetBytes(json);

            await _stream!.WriteAsync(bytes, 0, bytes.Length);
        }

        public async Task SendInitMessage(SimVarDef[] simVars, string[] simEvents)
        {
            await SendMessage(MessageType.Init, new InitPayload { SimVars = simVars, SimEvents = simEvents });
        }

        private async Task ListenAsync()
        {
            try
            {
                using var reader = new StreamReader(_stream!, Encoding.UTF8);

                while (true)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null)
                    {
                        IsConnected = false;
                        Console.WriteLine("[Client] Connection lost. Attempting reconnect...");
                        break;
                    }

                    try
                    {
                        var msg = JsonSerializer.Deserialize<ServerMessage<object>>(line);
                        if (msg != null)
                            OnMessage?.Invoke(msg);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Client] Error parsing message: {ex.Message}");
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[Client] Connection error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Unexpected error: {ex}");
            }

            await ReconnectAsync();
        }

        private async Task ReconnectAsync()
        {
            Console.WriteLine("[Client] Reconnecting...");
            _tcp?.Close();
            _tcp = new TcpClient();
            await ConnectAsync();
        }
    }

    public class SimVarDef
    {
        public required string Name { get; set; }
        public required string Unit { get; set; }
        public bool? Debug { get; set; } // if we want the server to print extra debugging stuff
    }

    public class ServerMessage<T>
    {
        public required MessageType Type { get; set; }
        public required T Payload { get; set; }

        public override string ToString()
        {
            return $"ServerMessage type={Type} payload={Payload}";
        }
    }

    public class SimVarPayload
    {
        public required string Name { get; set; }
        public required string Unit { get; set; }
        public required object Value { get; set; }

        public override string ToString()
        {
            return $"SimVarPayload name={Name} unit={Unit} value={Value}";
        }
    }

    public class InitPayload
    {
        public required SimVarDef[] SimVars { get; set; }
        public required string[] SimEvents { get; set; }
    }

    public enum MessageType
    {
        Init,
        Var,
        Event,
        Unknown
    }
}

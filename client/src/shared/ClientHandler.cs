using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenGaugeClient.Client
{
    public class ClientHandler
    {
        private string _host;
        private int _port;
        private TcpClient _tcp;
        private NetworkStream? _stream;

        public delegate void MessageHandler(ServerMessage<object> message);
        public event MessageHandler? OnMessage;

        public Action? OnConnect;
        public Action<Exception?> OnDisconnect;

        public bool IsConnecting = false;
        public bool IsConnected = false;

        public ClientHandler(string host, int port)
        {
            _host = host;
            _port = port;
            _tcp = new TcpClient();
        }

        public async Task ConnectAsync()
        {
            while (true)
            {
                try
                {
                    IsConnecting = true;
                    Console.WriteLine($"Connecting to server {_host}:{_port}...");
                    await _tcp.ConnectAsync(_host, _port);
                    _stream = _tcp.GetStream();
                    IsConnecting = false;
                    IsConnected = true;
                    Console.WriteLine("Connected to server");
                    OnConnect?.Invoke();
                    _ = ListenAsync();
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Client] Connect failed: {ex.Message}. Retrying in 2 seconds...");
                    IsConnecting = false;
                    OnDisconnect?.Invoke(ex);
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

            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[Client] Send message {message.ToString()}");

            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            var json = JsonSerializer.Serialize(message, options) + "\n"; // must end in NL
            var bytes = Encoding.UTF8.GetBytes(json);

            await _stream!.WriteAsync(bytes, 0, bytes.Length);
        }

        public async Task SendInitMessage(string? vehicleName, SimVarDef[] simVars, string[] simEvents)
        {
            await SendMessage(MessageType.Init, new InitPayload { VehicleName = vehicleName, SimVars = simVars, SimEvents = simEvents });
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
                        OnDisconnect?.Invoke(null);
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
                        Console.WriteLine($"[Client] Error parsing message or calling handler: {ex}");
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
        public required string? VehicleName { get; set; }
        public required SimVarDef[] SimVars { get; set; }
        public required string[] SimEvents { get; set; }
    }

    public enum MessageType
    {
        Init,
        ReInit,
        Var,
        Event,
        Unknown
    }
}

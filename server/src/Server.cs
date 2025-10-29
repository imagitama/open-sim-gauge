using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenGaugeServer
{
    public class Server
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<TcpClient, NetworkStream> _clients = new();

        public delegate void MessageHandler(ServerMessage<object> message);
        public event MessageHandler? OnMessage;

        public bool IsRunning = false;

        public Server(string? _ipAddress, int? _port)
        {
            IPAddress resolvedAddress;

            if (string.IsNullOrWhiteSpace(_ipAddress))
                resolvedAddress = IPAddress.Any;
            else
                resolvedAddress = IPAddress.Parse(_ipAddress);

            var port = _port ?? 1234;

            Console.WriteLine($"Starting server on {resolvedAddress}:{port}");

            _listener = new TcpListener(resolvedAddress, port);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _listener.Start();

            Console.WriteLine($"Server listening on port {_listener.LocalEndpoint}");

            IsRunning = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync();
                
                var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                if (endpoint != null)
                {
                    var ip = endpoint.Address.ToString();
                    var port = endpoint.Port;
                    Console.WriteLine($"Client connected: {ip}:{port}");
                }
                else
                {
                    Console.WriteLine($"Client connected");
                }
                
                _clients[client] = client.GetStream();
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
            var stream = client.GetStream();

            try
            {
                while (client.Connected)
                {
                    // detect socket closed by remote (Ctrl+C)
                    if (client.Client.Poll(0, SelectMode.SelectRead) && client.Available == 0)
                    {
                        Console.WriteLine("Client disconnected (poll)");
                        break;
                    }

                    if (stream.DataAvailable)
                    {
                        var line = await reader.ReadLineAsync();

                        if (line == null)
                            break;

                        try
                        {
                            var options = new JsonSerializerOptions
                            {
                                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                            };
                            var msg = JsonSerializer.Deserialize<ServerMessage<object>>(line, options);
                            if (msg != null)
                                OnMessage?.Invoke(msg);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"HandleClientAsync failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        await Task.Delay(100); // prevent tight loop
                    }
                }
            }
            catch (IOException)
            {
                Console.WriteLine("Client disconnected (IOException)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client error: {ex.Message}");
            }

            Console.WriteLine("Client disconnected");
            _clients.TryRemove(client, out _);
            client.Close();
        }

        public void BroadcastSimVar(string name, string unit, object value)
        {
            Broadcast<SimVarPayload>(MessageType.Var, new SimVarPayload { Name = name, Unit = unit, Value = value });
        }

        // public void BroadcastEvent(string name)
        // {
        //     Broadcast(new ServerMessage { Type = MessageType.Event, Name = name });
        // }

        public void Broadcast<TPayload>(MessageType type, TPayload payload)
        {
            var message = new ServerMessage<TPayload> {
                Type = type,
                Payload = payload
            };

            if (ConfigManager.Debug)
            {
                Console.WriteLine($"[Server] Broadcast {message.ToString()}");
            }

            var json = JsonSerializer.Serialize(message) + "\n";
            var bytes = Encoding.UTF8.GetBytes(json);

            foreach (var stream in _clients.Values)
            {
                try { stream.Write(bytes, 0, bytes.Length); }
                catch { /* ignore client failures */ }
            }
        }
    }

    public class SimVarDef
    {
        public required string Name { get; set; }
        public required string Unit { get; set; }
        public bool? Debug { get; set; } // if client wants us to print extra debugging stuff
    }

    public class ServerMessage<T>
    {
        public MessageType Type { get; set; }
        public required T Payload { get; set; }

        public override string ToString()
        {
            return $"ServerMessage type={Type} payload={Payload!.ToString()}";
        }
    }

    public class SimVarPayload
    {
        public required string Name { get; set; }
        public required string Unit { get; set; }
        public required object Value { get; set; }

        public override string ToString()
        {
            return $"SimVarPayload Name={Name} Unit={Unit} Value={Value}";
        }
    }

    public class InitPayload
    {
        public required SimVarDef[] SimVars { get; set; }
        public required string[] SimEvents { get; set; }

        public override string ToString()
        {
            var simVarsList = string.Join(", ", SimVars.Select(v => $"{v.Name} ({v.Unit})"));
            var eventsList = string.Join(", ", SimEvents);

            return $"InitPayload:\n" +
                $"  SimVars: [{simVarsList}]\n" +
                $"  SimEvents: [{eventsList}]";
        }
    }

    public enum MessageType
    {
        Init,
        Var,
        Event,
        Unknown
    }
}

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenGaugeServer
{
    public class ClientWrapper(TcpClient client)
    {
        public TcpClient Client = client;
        public override string ToString()
        {
            try
            {
                var socket = Client?.Client;
                if (socket == null)
                    return "[not_connected]";

                var endpoint = socket.RemoteEndPoint as IPEndPoint;
                if (endpoint == null)
                    return "[no_endpoint]";

                return endpoint.ToString();
            }
            catch
            {
                return "[unavailable]";
            }
        }
    }

    public class Server
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<ClientWrapper, NetworkStream> _clients = new();

        public Action<ClientWrapper, ClientMessage<object>>? OnMessage;
        public Action<ClientWrapper>? OnClientConnect;
        public Action<ClientWrapper>? OnClientDisconnect;

        public bool IsRunning = false;

        public Server(string _ipAddress, int _port)
        {
            var resolvedIpAddress = IPAddress.Parse(_ipAddress);

            Console.WriteLine($"Starting server on {resolvedIpAddress}:{_port}");

            _listener = new TcpListener(resolvedIpAddress, _port);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _listener.Start();

            Console.WriteLine($"Server listening on port {_listener.LocalEndpoint}");

            IsRunning = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync();
                var wrapper = new ClientWrapper(client);

                Console.WriteLine($"Client {wrapper} connected");

                _clients[wrapper] = client.GetStream();
                _ = HandleClientAsync(wrapper);
            }
        }

        private async Task HandleClientAsync(ClientWrapper wrapper)
        {
            var client = wrapper.Client;
            using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
            var stream = client.GetStream();

            try
            {
                while (client.Connected)
                {
                    // detect socket closed by remote (Ctrl+C)
                    if (client.Client.Poll(0, SelectMode.SelectRead) && client.Available == 0)
                    {
                        if (ConfigManager.Config.Debug)
                            Console.WriteLine($"[Server] Client {wrapper} disconnected (poll)");
                        OnClientDisconnect?.Invoke(wrapper);
                        break;
                    }

                    if (stream.DataAvailable)
                    {
                        var line = await reader.ReadLineAsync();

                        if (line == null)
                            break;

                        var options = new JsonSerializerOptions
                        {
                            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                        };
                        var msg = JsonSerializer.Deserialize<ClientMessage<object>>(line, options);
                        if (msg != null)
                            OnMessage?.Invoke(wrapper, msg);

                    }
                    else
                    {
                        await Task.Delay(100); // prevent tight loop
                    }
                }
            }
            catch (IOException)
            {
                if (ConfigManager.Config.Debug)
                    Console.WriteLine($"[Server] Client {wrapper} disconnected (IOException)");

                OnClientDisconnect?.Invoke(wrapper);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Client {wrapper} error: {ex.Message}");
            }

            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[Server] Client {wrapper} disconnected");

            OnClientDisconnect?.Invoke(wrapper);

            client.Close();
            _clients.TryRemove(wrapper, out _);
        }

        public void BroadcastVar(ClientWrapper? client, string name, string unit, object? value)
        {
            Broadcast(client, MessageType.Var, new VarPayload { Name = name, Unit = unit, Value = value });
        }

        public void BroadcastInit(ClientWrapper? client, string? currentVehicleName)
        {
            Broadcast(client, MessageType.Init, new InitPayload { VehicleName = currentVehicleName, Events = [], Vars = [] });
        }

        public void BroadcastReInit(ClientWrapper? client, string? currentVehicleName)
        {
            Broadcast(client, MessageType.ReInit, new InitPayload { VehicleName = currentVehicleName, Events = [], Vars = [] });
        }

        public void Broadcast<TPayload>(ClientWrapper? client, MessageType type, TPayload payload)
        {
            var message = new ServerMessage<TPayload>
            {
                Type = type,
                Payload = payload
            };

            var json = JsonSerializer.Serialize(message) + "\n";
            var bytes = Encoding.UTF8.GetBytes(json);

            // handle disconnections
            if (client != null && !_clients.ContainsKey(client))
                return;

            List<NetworkStream> streamsToSendTo = client != null ? [_clients[client]] : [.. _clients.Values];

            foreach (var stream in streamsToSendTo)
            {
                if (ConfigManager.Config.Debug)
                    Console.WriteLine($"[Server] Broadcast {message}");

                try { stream.Write(bytes, 0, bytes.Length); }
                catch { /* ignore client failures */ }
            }
        }
    }
}
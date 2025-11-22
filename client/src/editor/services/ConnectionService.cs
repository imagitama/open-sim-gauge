using System.Text.Json;
using OpenGaugeClient.Client;

// TODO: re-use with main client
namespace OpenGaugeClient.Editor.Services
{
    public class ConnectionService
    {
        public static ConnectionService Instance { get; } = new();

        private ClientHandler? _client;
        private string? _lastKnownVehicleName;
        public string? LastKnownVehicleName => _lastKnownVehicleName;
        private Dictionary<(string, string), object?> simVarValues = new Dictionary<(string, string), object?>();
        public bool IsConnecting => _client != null && _client.IsConnecting;
        public bool IsConnected => _client != null && _client.IsConnected;
        public Exception? LastFailReason => _client != null ? _client.LastFailReason : null;
        public Action? OnConnect;
        public Action<Exception?>? OnDisconnect;
        public Action<string?>? OnVehicle;

        public object? GetSimVarValue(string name, string unit)
        {
            simVarValues.TryGetValue((name, unit), out var v);
            return v;
        }

        public async Task Connect()
        {
            Console.WriteLine($"[ConnectionService] Connect");

            if (IsConnected)
            {
                Console.WriteLine($"[ConnectionService] Already connected");
                return;
            }
            if (IsConnecting)
            {
                Console.WriteLine($"[ConnectionService] Already connecting");
                return;
            }

            _client = new ClientHandler(ConfigManager.Config.Server.IpAddress, ConfigManager.Config.Server.Port);

            Func<Task> TellServerWeWantToInit = async () =>
            {
                if (ConfigManager.Config.Debug)
                    Console.WriteLine($"[ConnectionService] Tell server to initialize...");

                // ensure we pass null to subscribe to all
                // TODO: make this optional for re-use with client
                var varsToSubscribeTo = VarHelper.GetVarDefsToSubscribeTo(ConfigManager.Config, null);

                if (ConfigManager.Config.Debug)
                    Console.WriteLine($"[ConnectionService] Vars: {string.Join(", ", varsToSubscribeTo.Select(x => $"{x.Name} ({x.Unit})"))}");

                await _client.SendInitMessage(
                    _lastKnownVehicleName,
                    varsToSubscribeTo.ToArray(),
                    // TODO: finish events
                    new string[] { }
                );
            };

            _client.OnConnect += () =>
            {
                TellServerWeWantToInit();
                OnConnect?.Invoke();
            };

            _client.OnDisconnect += OnDisconnect;

            var hasSentAVar = false;
            _client.OnMessage += async (msg) =>
            {
                switch (msg.Type)
                {
                    case MessageType.ReInit:
                    case MessageType.Init:
                        if (ConfigManager.Config.Debug)
                            if (msg.Type == MessageType.ReInit)
                            {
                                Console.WriteLine("[ConnectionService] Re-initializing...");
                            }
                            else
                            {
                                Console.WriteLine("[ConnectionService] Initializing...");
                            }

                        var initPayload = ((JsonElement)msg.Payload).Deserialize<InitPayload>() ?? throw new Exception("Payload is null");

                        var newVehicleName = initPayload.VehicleName;

                        if (newVehicleName != _lastKnownVehicleName)
                            OnVehicle?.Invoke(newVehicleName);

                        _lastKnownVehicleName = newVehicleName;

                        if (msg.Type == MessageType.ReInit)
                        {
                            await TellServerWeWantToInit();
                        }
                        break;

                    case MessageType.Var:
                        var simVarPayload = ((JsonElement)msg.Payload).Deserialize<SimVarPayload>() ?? throw new Exception("Payload is null");

                        var key = (simVarPayload.Name, simVarPayload.Unit);
                        simVarValues[key] = ((JsonElement)simVarPayload.Value).GetDouble();

                        if (!hasSentAVar)
                        {
                            hasSentAVar = true;
                            Console.WriteLine($"Our first SimVar: '{simVarPayload.Name}' ({simVarPayload.Unit}) => {simVarPayload.Value}");
                        }

                        break;
                }
            };

            var connectTask = Task.Run(() => _client.ConnectAsync());

            await Task.WhenAll(connectTask);
        }
    }
}
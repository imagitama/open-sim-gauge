using System.Text.Json;
using Avalonia;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Themes.Fluent;
using ReactiveUI.Avalonia;
using OpenGaugeClient.Shared;

namespace OpenGaugeClient.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string logPath = Path.Combine(AppContext.BaseDirectory, "client.log");
            var fileWriter = new StreamWriter(logPath, append: true) { AutoFlush = true };
            Console.SetOut(new TeeTextWriter(Console.Out, fileWriter));
            Console.SetError(new TeeTextWriter(Console.Error, fileWriter));

            Console.WriteLine("Starting up...");
            Console.Out.Flush();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<ClientApp>()
            .UsePlatformDetect()
            .UseReactiveUI()
            .LogToTrace()
            .AfterSetup(_ =>
                {
                    // make textboxes work
                    Application.Current!.Styles.Add(new FluentTheme()
                    {
                        DensityStyle = DensityStyle.Compact
                    });
                });
    }

    public partial class ClientApp : Application
    {
        private PanelManager? _panelManager;
        private string? lastKnownVehicleName;

        private Dictionary<(string, string), object?> simVarValues = new Dictionary<(string, string), object?>();

        private object? GetSimVarValue(string name, string unit)
        {
            simVarValues.TryGetValue((name, unit), out var v);
            return v;
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                Console.WriteLine($"[Global] Unhandled exception: {e.ExceptionObject}");
            };
            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                Console.WriteLine($"[UI Thread] {e.Exception}");
                e.Handled = true;
            };

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }

            var config = await ConfigManager.LoadConfig();

            Console.WriteLine($"Loaded {config.Panels.Count} panels and {config.Gauges.Count} gauges");

            var persistedState = await PersistanceManager.LoadState();

            if (persistedState != null && persistedState.LastKnownVehicleName != null)
            {
                lastKnownVehicleName = persistedState.LastKnownVehicleName;
                Console.WriteLine($"Last known vehicle was '{lastKnownVehicleName}'");
            }

            _panelManager = new PanelManager();

            if (ConfigManager.Debug)
                _panelManager.Initialize(config, GetSimVarValue, lastKnownVehicleName);

            var client = new ClientHandler(config.Server.IpAddress, config.Server.Port);

            Func<Task> TellServerWeWantToInit = async () =>
            {
                if (config.Debug)
                    Console.WriteLine($"[Main] Tell server to initialize (vehicle '{lastKnownVehicleName}')...");

                simVarValues = await GetEmptySimVarValues(config, lastKnownVehicleName);
                var simVarDefs = await GetSimVarDefsToSubscribeTo(config, lastKnownVehicleName);

                if (config.Debug)
                    Console.WriteLine($"[Main] Sim vars: {string.Join(", ", simVarDefs.Select(x => $"{x.Name} ({x.Unit})"))}");

                await client.SendInitMessage(
                    lastKnownVehicleName,
                    simVarDefs.ToArray(),
                    // TODO: finish events
                    new string[] { }
                );
            };

            client.OnConnect += () =>
            {
                _panelManager.SetConnected(true);
                TellServerWeWantToInit();
            };

            client.OnDisconnect += () =>
            {
                _panelManager.SetConnected(false);
            };

            var hasSentAVar = false;
            client.OnMessage += async (msg) =>
            {
                switch (msg.Type)
                {
                    case MessageType.ReInit:
                    case MessageType.Init:
                        if (ConfigManager.Config.Debug)
                            if (msg.Type == MessageType.ReInit)
                            {
                                Console.WriteLine("[Main] Re-initializing...");
                            }
                            else
                            {
                                Console.WriteLine("[Main] Initializing...");
                            }

                        var initPayload = ((JsonElement)msg.Payload).Deserialize<InitPayload>() ?? throw new Exception("Payload is null");

                        if (initPayload.VehicleName != lastKnownVehicleName)
                        {
                            Console.WriteLine($"Vehicle changed to '{initPayload.VehicleName}'");

                            lastKnownVehicleName = initPayload.VehicleName;

                            _ = PersistanceManager.Persist("LastKnownVehicleName", lastKnownVehicleName);
                        }

                        if (msg.Type == MessageType.ReInit)
                        {
                            await TellServerWeWantToInit();
                        }
                        else
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                _panelManager.Initialize(config, GetSimVarValue, lastKnownVehicleName);
                            });
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

            var connectTask = Task.Run(() => client.ConnectAsync());

            await Task.WhenAll(connectTask);

            base.OnFrameworkInitializationCompleted();
        }

        // TODO: Move to some helper
        public async Task<List<SimVarDef>> GetSimVarDefsToSubscribeTo(Config config, string? vehicleName)
        {
            var simVarDefs = new List<SimVarDef>();

            if (config.Panels == null || config.Panels.Count == 0)
                throw new Exception("No panels");

            foreach (var panel in config.Panels)
            {
                if (vehicleName != null && panel.Vehicle != null && !Utils.GetIsVehicle(panel.Vehicle, vehicleName))
                    continue;

                if (panel.Skip == true)
                    continue;

                var gauges = await GetGaugesByNames(panel.Gauges, config);

                foreach (var gauge in gauges)
                {
                    var layers = gauge.Layers;

                    foreach (var layer in layers)
                    {
                        void AddSimVar(VarConfig varConfig)
                        {
                            simVarDefs.Add(new SimVarDef { Name = varConfig.Name, Unit = varConfig.Unit, Debug = layer.Debug == true });
                        }

                        if (layer.Text?.Var is not null)
                            AddSimVar(layer.Text.Var!);

                        if (layer.Transform is { } transform)
                        {
                            if (transform.Rotate?.Var is not null)
                                AddSimVar(transform.Rotate.Var!);

                            if (transform.TranslateX?.Var is not null)
                                AddSimVar(transform.TranslateX.Var!);

                            if (transform.TranslateY?.Var is not null)
                                AddSimVar(transform.TranslateY.Var!);

                            if (transform.Path?.Var is not null)
                                AddSimVar(transform.Path.Var!);
                        }
                    }
                }
            }

            return simVarDefs;
        }

        // TODO: Move to some helper
        public async Task<Dictionary<(string, string), object?>> GetEmptySimVarValues(Config config, string? vehicleName)
        {
            Dictionary<(string, string), object?> simVarValues = new();

            foreach (var panel in config.Panels)
            {
                if (vehicleName != null && panel.Vehicle != null && !Utils.GetIsVehicle(panel.Vehicle, vehicleName))
                    continue;

                if (panel.Skip == true)
                    continue;

                var gauges = await GetGaugesByNames(panel.Gauges, config);

                foreach (var gauge in gauges)
                {
                    var layers = gauge.Layers;

                    foreach (var layer in layers)
                    {
                        if (layer.Skip == true)
                            continue;

                        var transform = layer.Transform;

                        if (transform != null)
                        {
                            var varConfigs = new List<VarConfig>();

                            if (transform.Rotate?.Var != null)
                                varConfigs.Add(transform.Rotate.Var);

                            if (transform.TranslateX?.Var != null)
                                varConfigs.Add(transform.TranslateX.Var);

                            if (transform.TranslateY?.Var != null)
                                varConfigs.Add(transform.TranslateY.Var);

                            if (transform.Path?.Var != null)
                                varConfigs.Add(transform.Path.Var);

                            foreach (var varConfig in varConfigs)
                            {
                                var key = (varConfig.Name, varConfig.Unit);
                                simVarValues[key] = null;
                            }
                        }
                    }
                }
            }

            return simVarValues;
        }

        // TODO: Move to some helper
        public async Task<List<Gauge>> GetGaugesByNames(List<GaugeRef> gaugeRefs, Config config)
        {
            var gauges = new List<Gauge>();

            foreach (var gaugeRef in gaugeRefs)
            {
                Gauge? gauge;

                if (!string.IsNullOrEmpty(gaugeRef.Path))
                {
                    gauge = await ConfigManager.LoadTypedJson<Gauge>(gaugeRef.Path, forceToGitRoot: true);
                }
                else
                {
                    gauge = config.Gauges.Find(g => g.Name == gaugeRef.Name);
                }

                if (gauge == null)
                {
                    throw new Exception($"Gauge '{gaugeRef.Name ?? gaugeRef.Path}' not found");
                }

                gauges.Add(gauge);
            }

            return gauges;
        }
    }
}

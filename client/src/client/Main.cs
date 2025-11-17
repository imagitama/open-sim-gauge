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
        public static string[] StartupArgs = Array.Empty<string>();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public static StreamWriter FileLogger;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public static void Main(string[] args)
        {
            StartupArgs = args;

            string logPath = Path.Combine(AppContext.BaseDirectory, "client.log");
            FileLogger = new StreamWriter(logPath, append: true) { AutoFlush = true };
            Console.SetOut(new TeeTextWriter(Console.Out, FileLogger));
            Console.SetError(new TeeTextWriter(Console.Error, FileLogger));

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
                var msg = $"[Global] Unhandled exception: {e.ExceptionObject}";
#if DEBUG
                Console.WriteLine(msg);
#else
                Program.FileLogger.WriteLine(msg);
#endif
            };
            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                var msg = $"[UI Thread] {e.Exception}";
#if DEBUG
                Console.WriteLine(msg);
#else
                Program.FileLogger.WriteLine(msg);
#endif
                e.Handled = true;
            };

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }

            var configPath = GetConfigPathFromArgs();

            var config = await ConfigManager.LoadConfig(configPath);

            var cliArgs = Cli.ParseArgs(Program.StartupArgs);

            Cli.ApplyArgsToConfig(config, cliArgs);

            Console.WriteLine($"Loaded {config.Panels.Count} panels and {config.Gauges.Count} gauges");

            var persistedState = await PersistanceManager.LoadState();

            if (persistedState != null && persistedState.LastKnownVehicleName != null)
            {
                lastKnownVehicleName = persistedState.LastKnownVehicleName;
                Console.WriteLine($"Last known vehicle was '{lastKnownVehicleName}'");
            }

            _panelManager = new PanelManager();

            if (ConfigManager.Config.Debug)
                _panelManager.Initialize(config, GetSimVarValue, lastKnownVehicleName);

            var client = new ClientHandler(config.Server.IpAddress, config.Server.Port);

            Func<Task> TellServerWeWantToInit = async () =>
            {
                if (config.Debug)
                    Console.WriteLine($"[Main] Tell server to initialize (vehicle '{lastKnownVehicleName}')...");

                var simVarsToSubscribeTo = SimVarHelper.GetSimVarDefsToSubscribeTo(config, lastKnownVehicleName);

                if (config.Debug)
                    Console.WriteLine($"[Main] Sim vars: {string.Join(", ", simVarsToSubscribeTo.Select(x => $"{x.Name} ({x.Unit})"))}");

                await client.SendInitMessage(
                    lastKnownVehicleName,
                    simVarsToSubscribeTo.ToArray(),
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

        private static string? GetConfigPathFromArgs()
        {
            var args = Program.StartupArgs;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--config" && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}

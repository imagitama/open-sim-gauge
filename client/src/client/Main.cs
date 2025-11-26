using System.Text.Json;
using Avalonia;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
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

            Console.WriteLine("Starting client...");
            Console.Out.Flush();

            ClientAppBuilder.BuildAvaloniaApp(args).StartWithClassicDesktopLifetime(args);
        }
    }

    public partial class ClientApp : Application
    {
        private PanelManager? _panelManager;
        private SimVarManager? _simVarManager;
        private string? lastKnownVehicleName;

        private readonly string[] _args;

        public ClientApp(string[] args)
        {
            _args = args;
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

            try
            {
                var configPath = Cli.GetConfigPathFromArgs(_args);

                var config = await ConfigManager.LoadConfig(configPath);

                var cliArgs = Cli.ParseArgs(_args);

                Cli.ApplyArgsToConfig(config, cliArgs);

                Console.WriteLine($"Loaded {config.Panels.Count} panels and {config.Gauges.Count} gauges");

                var persistedState = await PersistanceManager.LoadState();

                if (persistedState != null && persistedState.LastKnownVehicleName != null)
                {
                    lastKnownVehicleName = persistedState.LastKnownVehicleName;
                    Console.WriteLine($"Last known vehicle was '{lastKnownVehicleName}'");
                }

                _simVarManager = new SimVarManager();
                _panelManager = new PanelManager();

                if (ConfigManager.Config.RequireConnection != true)
                    _panelManager.Initialize(config, _simVarManager.GetBestSimVarValue, lastKnownVehicleName);

                var client = new ClientHandler(config.Server.IpAddress, config.Server.Port);

                Func<Task> TellServerWeWantToInit = async () =>
                {
                    if (config.Debug)
                        Console.WriteLine($"[Main] Tell server to initialize (vehicle '{lastKnownVehicleName}')...");

                    var varsToSubscribeTo = SimVarHelper.GetSimVarDefsToSubscribeTo(config, lastKnownVehicleName);

                    if (config.Debug)
                        Console.WriteLine($"[Main] Vars: {string.Join(", ", varsToSubscribeTo.Select(x => $"{x.Name} ({x.Unit})"))}");

                    await client.SendInitMessage(
                        lastKnownVehicleName,
                        varsToSubscribeTo.ToArray(),
                        // TODO: finish events
                        new string[] { }
                    );
                };

                client.OnConnect += () =>
                {
                    _panelManager.SetConnected(true);
                    TellServerWeWantToInit();
                };

                client.OnDisconnect += (reason) =>
                {
                    _panelManager.SetConnected(false);
                };

                client.OnMessage += async (msg) =>
                {
                    try
                    {
                        switch (msg.Type)
                        {
                            case MessageType.ReInit:
                            case MessageType.Init:
                                {
                                    if (ConfigManager.Config.Debug)
                                        if (msg.Type == MessageType.ReInit)
                                            Console.WriteLine("[Main] Re-initializing...");
                                        else
                                            Console.WriteLine("[Main] Initializing...");

                                    var payload = ((JsonElement)msg.Payload).Deserialize<InitPayload>() ?? throw new Exception("Payload is null");

                                    if (payload.VehicleName != lastKnownVehicleName)
                                    {
                                        Console.WriteLine($"Vehicle changed to '{payload.VehicleName}'");

                                        lastKnownVehicleName = payload.VehicleName;

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
                                            _panelManager.Initialize(config, _simVarManager.GetBestSimVarValue, lastKnownVehicleName);
                                        });
                                    }
                                    break;
                                }

                            case MessageType.Var:
                                {
                                    var payload = ((JsonElement)msg.Payload)
                                        .Deserialize<SimVarPayload>()
                                        ?? throw new Exception("Payload is null");

                                    if (ConfigManager.Config.Debug)
                                        Console.WriteLine($"[Main] Var {payload}");

                                    _simVarManager.StoreSimVar(payload.Name, payload.Unit, payload.Value);
                                    break;
                                }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Main] OnMessage error: {ex}");
                    }
                };

                var connectTask = Task.Run(() => client.ConnectAsync());

                await Task.WhenAll(connectTask);

                base.OnFrameworkInitializationCompleted();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start: {ex.Message}");
                throw;
            }
        }
    }
}

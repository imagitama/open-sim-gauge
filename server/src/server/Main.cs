using System.Globalization;
using System.Text;
using System.Text.Json;
using OpenGaugeAbstractions;

namespace OpenGaugeServer
{
    public class Program
    {
        private static DataSourceManager _dataSourceManager;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting up...");

            var configPath = Cli.GetConfigPathFromArgs(args);

            var config = await ConfigManager.LoadConfig();

            var cliArgs = Cli.ParseArgs(args);

            Cli.ApplyArgsToConfig(config, cliArgs);

            var server = new Server(config.Server.IpAddress, config.Server.Port);

#if DEBUG
            DataSourceFactory.LoadDataSources(PathHelper.GetFilePath("server/data-sources", forceToGitRoot: false));
#else
            DataSourceFactory.LoadDataSources(PathHelper.GetFilePath("data-sources", forceToGitRoot: false));
#endif

            var dataSource = DataSourceFactory.Create(config.Source);
            _dataSourceManager = new DataSourceManager(dataSource);

            var initPayloads = new Dictionary<ClientWrapper, InitPayload>();

            server.OnClientConnect += (client) =>
            {
                Console.WriteLine($"Client {client} connected");
            };

            server.OnClientDisconnect += (client) =>
            {
                Console.WriteLine($"Client {client} disconnected");

                initPayloads.Remove(client);

                _dataSourceManager.UnsubscribeFromUnusedVars(initPayloads.Values.SelectMany(x => x.Vars).ToArray());
                _dataSourceManager.UnsubscribeFromUnusedEvents(initPayloads.Values.SelectMany(x => x.Events).ToArray());
            };

            server.OnMessage += (client, msg) =>
            {
                if (msg.Type == MessageType.Init)
                {
                    if (ConfigManager.Config.Debug)
                        Console.WriteLine($"Client {client} wants to initialize: {msg}");

                    var payload = ((JsonElement)msg.Payload).Deserialize<InitPayload>();

                    if (payload == null)
                        throw new Exception("Payload is null");

                    initPayloads[client] = payload;

                    if (payload.VehicleName != _dataSourceManager.GetCurrentVehicleName())
                    {
                        Console.WriteLine($"Client {client} has vehicle '{payload.VehicleName}' but it is currently '{_dataSourceManager.GetCurrentVehicleName()}' - telling them to re-init");

                        // tell client they need to start again
                        server.BroadcastReInit(client, _dataSourceManager.GetCurrentVehicleName());
                        return;
                    }
                    else
                    {
                        // tell client everything matches up and they can render panels
                        server.BroadcastInit(client, _dataSourceManager.GetCurrentVehicleName());
                    }

                    Console.WriteLine($"Subscribing to {payload.Vars.Length} vars");

                    foreach (var varInfo in payload.Vars)
                    {
                        _dataSourceManager.SubscribeToVar(varInfo.Name, varInfo.Unit, data =>
                        {
                            if (varInfo.Debug == true)
                                Console.WriteLine($"Var '{varInfo.Name}' ({varInfo.Unit}) => {data}");

                            server.BroadcastVar(client, varInfo.Name, varInfo.Unit, data);
                        });
                    }

                    _dataSourceManager.UnsubscribeFromUnusedVars(initPayloads.Values.SelectMany(x => x.Vars).ToArray());

                    initPayloads[client] = payload;
                }
            };

            _dataSourceManager.SubscribeToVehicle(vehicleName =>
            {
                Console.WriteLine($"New vehicle '{vehicleName}', informing clients...");

                server.BroadcastReInit(null, _dataSourceManager.GetCurrentVehicleName());
            });

            while (!dataSource.IsConnected)
            {
                try
                {
                    Console.WriteLine($"Connecting to data source '{config.Source}'...");
                    await dataSource.Connect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to connect to data source '{config.Source}': {ex.Message}");
                }

                if (!dataSource.IsConnected)
                {
                    Console.WriteLine($"Retrying connecting to data source '{config.Source}'...");
                    await Task.Delay((int)ConfigManager.Config.ReconnectDelay);
                }
            }

            Console.WriteLine($"Connected successfully");

            Console.WriteLine("Press Ctrl+C to to quit");

            var cts = new CancellationTokenSource();

            var httpTask = server.StartAsync(cts.Token);

            var simTask = Task.Run(async () =>
            {
                try
                {
                    await dataSource.Listen();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Listen failed: " + ex);
                }
            });

            var consoleTask = Task.Run(() => ReadConsoleInput(cts, dataSource, server));

            try
            {
                await Task.WhenAll(httpTask, simTask, consoleTask);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error: " + ex);
                cts.Cancel();
            }

            Console.WriteLine("Shutting down...");
            cts.Cancel();
        }

        private static void ReadConsoleInput(CancellationTokenSource cts, IDataSource dataSource, Server server)
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var line = Console.ReadLine();
                    if (line == null)
                        continue;

                    var allArgs = SplitArgs(line);
                    var command = allArgs[0].ToLowerInvariant();
                    var args = allArgs.Skip(1).ToArray();

                    if (ConfigManager.Config.Debug)
                        Console.WriteLine($"Command '{command}' args: {string.Join(",", args)}");

                    switch (command)
                    {
                        case "exit":
                        case "quit":
                            cts.Cancel();
                            break;

                        case "set":
                            {
                                var varName = args[0] ?? throw new Exception("Need a var name");
                                var unit = args[1] ?? throw new Exception("Need a unit (can be 'null')");
                                var value = args[2] ?? throw new Exception("Need a value");
                                _dataSourceManager.ForceVarValue(varName, unit, value);
                            }
                            break;

                        case "unset":
                            {
                                var varName = args[0] ?? throw new Exception("Need a var name");
                                var unit = args[1] ?? throw new Exception("Need a unit (can be 'null')");
                                _dataSourceManager.ClearForcedVar(varName, unit);
                            }
                            break;

                        case "watch":
                            {
                                var varName = args[0] ?? throw new Exception("Need a var name");
                                var unit = args.Length > 1 ? args[1] : null;
                                _dataSourceManager.WatchVar(varName, unit);
                            }
                            break;

                        case "vehicle":
                            {
                                var vehicleName = args[0] ?? null;

                                if (vehicleName == null)
                                    _dataSourceManager.ClearForcedVehicleName();
                                else
                                    _dataSourceManager.ForceVehicleName(vehicleName);
                            }
                            break;

                        default:
                            Console.WriteLine($"Unknown command: {line}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse command: {ex}");
                }
            }
        }

        private static string[] SplitArgs(string input)
        {
            var args = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            foreach (char c in input)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        args.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
                args.Add(current.ToString());

            return args.ToArray();
        }
    }
}

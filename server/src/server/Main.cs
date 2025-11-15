using System.Text;
using System.Text.Json;
using OpenGaugeAbstractions;

namespace OpenGaugeServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting up...");

            var config = await ConfigManager.LoadConfig();

            var cliArgs = Cli.ParseArgs(args);

            Cli.ApplyArgsToConfig(config, cliArgs);

            var server = new Server(config.Server.IpAddress, config.Server.Port);

            DataSourceFactory.LoadDataSources(PathHelper.GetFilePath("data-sources", forceToGitRoot: false));

            var dataSource = DataSourceFactory.Create(config.Source);

            server.OnMessage += (msg) =>
            {
                if (msg.Type == MessageType.Init)
                {
                    if (ConfigManager.Debug)
                        Console.WriteLine($"Client wants to initialize: {msg}");

                    var payload = ((JsonElement)msg.Payload).Deserialize<InitPayload>();

                    if (payload == null)
                        throw new Exception("Payload is null");

                    if (dataSource.CurrentVehicleName == null)
                        throw new Exception("Data source vehicle name is null");

                    if (payload.VehicleName != dataSource.CurrentVehicleName)
                    {
                        Console.WriteLine($"Client has vehicle '{payload.VehicleName}' but it is currently '{dataSource.CurrentVehicleName}' - telling them to re-init");

                        // TODO: Broadcast to this client specifically

                        // tell all clients they need to start again
                        server.BroadcastReInit(dataSource.CurrentVehicleName);
                        return;
                    }
                    else
                    {
                        // tell all clients everything matches up and they can render panels
                        server.BroadcastInit(dataSource.CurrentVehicleName);
                    }

                    // TODO: Handle unsubscribing

                    Console.WriteLine($"Subscribing to {payload!.SimVars.Length} SimVars");

                    foreach (var simVar in payload!.SimVars)
                    {
                        dataSource.SubscribeToVar(simVar.Name, simVar.Unit, data =>
                        {
                            if (simVar.Debug == true)
                                Console.WriteLine($"SimVar '{simVar.Name}' ({simVar.Unit}) => {data}");

                            server.BroadcastSimVar(simVar.Name, simVar.Unit, data);
                        });
                    }
                }
            };

            dataSource.SubscribeToVehicle(vehicleName =>
            {
                Console.WriteLine($"New vehicle '{vehicleName}', informing clients...");

                server.BroadcastReInit(dataSource.CurrentVehicleName!);
            });

            while (!dataSource.IsConnected)
            {
                try
                {
                    Console.WriteLine($"Connecting to data source '{config.Source}'...");
                    dataSource.Connect();
                    Console.WriteLine($"Connected to data source '{config.Source}' successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to connect to data source '{config.Source}': {ex.Message}");
                }

                if (!dataSource.IsConnected)
                {
                    Console.WriteLine($"Retrying connecting to data source '{config.Source}' in 2 seconds...");
                    await Task.Delay(2000);
                }
            }

            Console.WriteLine("Press Ctrl+C to to quit");

            var cts = new CancellationTokenSource();

            var httpTask = server.StartAsync(cts.Token);
            var simTask = Task.Run(() => dataSource.Listen(config), cts.Token);
            var consoleTask = Task.Run(() => ReadConsoleInput(cts, dataSource, server));

            await Task.WhenAll(httpTask, simTask, consoleTask);

            Console.WriteLine("Shutting down...");
            cts.Cancel();
        }

        private static void ReadConsoleInput(CancellationTokenSource cts, IDataSource dataSource, Server server)
        {
            while (!cts.IsCancellationRequested)
            {
                var line = Console.ReadLine();
                if (line == null)
                    continue;

                var args = SplitArgs(line);
                var command = args[0].ToLowerInvariant();

                switch (command)
                {
                    case "exit":
                    case "quit":
                        cts.Cancel();
                        break;

                    // case "set":
                    //     if (args.Length < 3)
                    //     {
                    //         Console.WriteLine("Usage: set <SimVarName> <Value>");
                    //         break;
                    //     }

                    //     if (dataSource != null && dataSource is EmulatorDataSource)
                    //     {
                    //         var simVar = args[1];
                    //         var value = args[2];
                    //         Console.WriteLine($"Setting SimVar {simVar} to {value}");
                    //         double doubleValue = double.Parse(value, CultureInfo.InvariantCulture);
                    //         dataSource.ForceVarValue(simVar, doubleValue);
                    //     }
                    //     else
                    //     {
                    //         Console.WriteLine("Only works in emulator");
                    //     }
                    //     break;

                    case "watch":
                        var varName = args[1];

                        if (dataSource != null)
                            dataSource.WatchVar(varName);

                        break;

                    default:
                        Console.WriteLine($"Unknown command: {line}");
                        break;
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

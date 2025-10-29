using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Globalization;

namespace OpenGaugeServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting up...");

            var config = await ConfigManager.LoadConfig();

            var server = new Server(config.Server?.IpAddress, config.Server?.Port);
            var dataSource = DataSourceFactory.Create(config.Source);

            server.OnMessage += (msg) =>
            {
                if (msg.Type == MessageType.Init)
                {
                    if (ConfigManager.Debug)
                        Console.WriteLine($"Initialize client: {msg}");

                    var payload = ((JsonElement)msg.Payload).Deserialize<InitPayload>();

                    foreach (var simVar in payload!.SimVars)
                    {
                        dataSource.SubscribeToVar(simVar.Name, simVar.Unit, data =>
                        {
                            server.BroadcastSimVar(simVar.Name, simVar.Unit, data);
                        });
                    }
                }
            };

            while (!dataSource.IsConnected)
            {
                try
                {
                    Console.WriteLine($"Connecting to data source '{config.Source}' {dataSource}...");
                    dataSource.Connect();
                    Console.WriteLine($"Connected to data source successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to connect to data source: {ex}");
                }

                if (!dataSource.IsConnected)
                {
                    Console.WriteLine("Retrying connecting to data source in 2 seconds...");
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

                    case "set":
                        if (args.Length < 3)
                        {
                            Console.WriteLine("Usage: set <SimVarName> <Value>");
                            break;
                        }

                        if (dataSource != null && dataSource is EmulatorDataSource)
                        {
                            var simVar = args[1];
                            var value = args[2];
                            Console.WriteLine($"Setting SimVar {simVar} to {value}");
                            double doubleValue = double.Parse(value, CultureInfo.InvariantCulture);
                            (dataSource as EmulatorDataSource)!.ForceVarValue(simVar, doubleValue);
                        }
                        else
                        {
                            Console.WriteLine("Only works in emulator");
                        }
                        break;

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

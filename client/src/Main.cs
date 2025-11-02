using System;
using System.Threading;
using System.Collections.Generic;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace OpenGaugeClient
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting up...");
            Console.Out.Flush();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }

    public class App : Application {
        private PanelManager _manager;

        private Dictionary<(string, string), object?> simVarValues = new Dictionary<(string, string), object?>();

        public App()
        {
            _manager = new PanelManager();
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            var config = await ConfigManager.LoadConfig();
            simVarValues = await GetEmptySimVarValues(config);
            var simVarDefs = await GetSimVarDefsToSubscribeTo(config);
            
            if (config.Debug)
                Console.WriteLine($"Subscribing to sim vars: {string.Join(", ", simVarDefs.Select(x => $"{x.Name} ({x.Unit})"))}");

            var client = new Client(config.Server.IpAddress, config.Server.Port);

            client.OnConnect += async () => {
                await client.SendInitMessage(
                    simVarDefs.ToArray(),
                    // TODO: finish events
                    new string[] {}
                );
            };

            var hasSentAVar = false;
            client.OnMessage += (msg) =>
            {
                switch (msg.Type)
                {
                    case MessageType.Init:
                        Console.WriteLine("Server has told us to init");
                        break;
                    case MessageType.Var:
                        var payload = ((JsonElement)msg.Payload).Deserialize<SimVarPayload>();

                        if (payload == null)
                            throw new Exception("Var payload invalid");
                        
                        var key = (payload.Name, payload.Unit);
                        simVarValues[key] = ((JsonElement)payload.Value).GetDouble();

                        if (!hasSentAVar)
                        {
                            hasSentAVar = true;
                            Console.WriteLine($"Server has sent us our first SimVar: '{payload.Name}' ({payload.Unit}) => {payload.Value}");
                        }

                        break;
                }
            };

            Func<string, string, object?> GetSimVarValue = (name, unit) =>
            {
                simVarValues.TryGetValue((name, unit), out var v);
                return v;
            };

            _manager.Initialize(config!, GetSimVarValue);

            var connectTask = Task.Run(() => client.ConnectAsync());
            var renderTask = _manager.RunRenderLoop(config, client);

            await Task.WhenAll(connectTask, renderTask);

            base.OnFrameworkInitializationCompleted();

            Console.WriteLine("Closed");
        }

        public async Task<List<SimVarDef>> GetSimVarDefsToSubscribeTo(Config config)
        {
            var simVarDefs = new List<SimVarDef>();

            if (config.Panels == null || config.Panels.Count == 0)
                throw new Exception("No panels");

            foreach (var panel in config.Panels)
            {
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

        public async Task<Dictionary<(string, string), object?>> GetEmptySimVarValues(Config config)
        {
            Dictionary<(string, string), object?> simVarValues = new();

            foreach (var panel in config.Panels)
            {
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

        public async Task<List<Gauge>> GetGaugesByNames(List<GaugeRef> gaugeRefs, Config config)
        {
            var gauges = new List<Gauge>();

            foreach (var gaugeRef in gaugeRefs)
            {
                Gauge? gauge;

                if (!string.IsNullOrEmpty(gaugeRef.Path))
                {
                    gauge = await ConfigManager.LoadJson<Gauge>(gaugeRef.Path);
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
 
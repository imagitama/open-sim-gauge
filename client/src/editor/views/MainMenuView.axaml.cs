using Avalonia.Controls;
using System.Collections.ObjectModel;
using ReactiveUI;
using System.Reactive;
using OpenGaugeClient.Editor.Services;
using System.Text.RegularExpressions;

namespace OpenGaugeClient.Editor
{
    public partial class MainMenuView : UserControl
    {
        MainMenuViewViewModel _vm;

        public MainMenuView()
        {
            InitializeComponent();
            _vm = new MainMenuViewViewModel();
            DataContext = _vm;

            _vm.OnCreateGauge = OnCreateGauge;
            _vm.OnDeletePanel = index => _ = OnDeletePanel(index);
            _vm.OnDeleteGauge = index => _ = OnDeleteGauge(index);
        }

        private async void OnCreateGauge()
        {
            Console.WriteLine($"[MainMenuView] On create gauge");

            if (VisualRoot is not Window window)
                throw new Exception("Window is null");

            var dialog = new CreateGaugeDialog();
            var ok = await dialog.ShowDialog<bool>(window);

            if (ok)
            {
                Console.WriteLine($"[MainMenuView] On create gauge - dialog ok lastValue={dialog.LastValue}");

                var (name, path) = dialog.LastValue;

                if (!string.IsNullOrEmpty(path))
                {
                    Console.WriteLine($"[MainMenuView] Create gauge into directory={path}");

                    var gaugeJsonPath = Path.Combine(path, "gauge.json");

                    Console.WriteLine($"[MainMenuView] Create gauge dir={path} json={gaugeJsonPath}");

                    var newGauge = new Gauge()
                    {
                        Name = $"Gauge #{ConfigManager.Config.Gauges.Count + 1}",
                        Width = 500,
                        Height = 500,
                        Layers = [],
                        Source = gaugeJsonPath
                    };

                    await GaugeHelper.SaveGaugeToFile(newGauge);
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    Console.WriteLine($"[MainMenuView] Create gauge with name={name}");

                    var newGauge = new Gauge()
                    {
                        Name = name,
                        Width = 500,
                        Height = 500,
                        Layers = []
                    };

                    await ConfigManager.AddGauge(newGauge);
                }
                else
                {
                    throw new Exception("Need a name or path");
                }

                _vm.Refresh();
            }
            else
            {
                Console.WriteLine($"[MainMenuView] On create gauge - dialog NOT ok");
            }
        }

        private async Task OnDeletePanel(int panelIndex)
        {
            Console.WriteLine($"[MainMenuViewViewModel] Delete panel index={panelIndex}");

            if (VisualRoot is not Window window)
                throw new Exception("Window is null");

            var dialog = new ConfirmDialog();
            var result = await dialog.ShowDialog<bool>(window);

            if (result)
            {
                await ConfigManager.DeletePanel(panelIndex);
                _vm.Refresh();
            }

            Console.WriteLine($"[MainMenuViewViewModel] Delete panel done result={result}");
        }

        private async Task OnDeleteGauge(int gaugeIndex)
        {
            Console.WriteLine($"[MainMenuViewViewModel] Delete gauge index={gaugeIndex}");

            if (VisualRoot is not Window window)
                throw new Exception("Window is null");

            var dialog = new ConfirmDialog();
            var result = await dialog.ShowDialog<bool>(window);

            if (result)
            {
                await ConfigManager.DeleteGauge(gaugeIndex);
                _vm.Refresh();
            }

            Console.WriteLine($"[MainMenuViewViewModel] Delete gauge done result={result}");
        }
    }

    public class PanelEntry
    {
        public int Index { get; }
        public Panel Panel { get; }

        public PanelEntry(int index, Panel panel)
        {
            Index = index;
            Panel = panel;
        }
    }

    public class GaugeEntry
    {
        public int Index { get; }
        public string? OutputPath { get; }
        public ReactiveGauge Gauge { get; }

        public GaugeEntry(int index, ReactiveGauge gauge, string? outputPath = null)
        {
            Index = index;
            Gauge = gauge;
            // need replace as avalonia strips underscores
            OutputPath = outputPath != null ? PathHelper.GetFileName(outputPath).Replace("_", "__") : null;
        }
    }

    public class MainMenuViewViewModel : ReactiveObject
    {
        private string _connectionStatus = GetConnectionStatus();
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
        }

        public ObservableCollection<PanelEntry> PanelsWithIndex { get; } = [];
        public ReactiveCommand<int, Unit> OpenPanelEditorCommand { get; }
        public ReactiveCommand<int, Unit> DeletePanelCommand { get; }
        public ReactiveCommand<int, Unit> DuplicatePanelCommand { get; }
        public ReactiveCommand<Panel?, Unit> CreatePanelCommand { get; }

        public ObservableCollection<GaugeEntry> GaugesWithIndex { get; } = [];
        public ObservableCollection<GaugeEntry> GaugesWithPath { get; } = [];
        public ReactiveCommand<int, Unit> DeleteGaugeCommand { get; }
        public ReactiveCommand<object, Unit> OpenGaugeEditorCommand { get; }
        public ReactiveCommand<Unit, Unit> CreateGaugeCommand { get; }
        public ReactiveCommand<Unit, Unit> ConnectToServerCommand { get; }
        public Action? OnCreateGauge;
        public Action<int>? OnDeletePanel;
        public Action<int>? OnDeleteGauge;

        public MainMenuViewViewModel()
        {
            OpenPanelEditorCommand = ReactiveCommand.Create<int>(OpenPanelEditor);
            DeletePanelCommand = ReactiveCommand.CreateFromTask<int>(DeletePanel);
            DuplicatePanelCommand = ReactiveCommand.CreateFromTask<int>(DuplicatePanel);
            CreatePanelCommand = ReactiveCommand.CreateFromTask<Panel?>(CreatePanel);
            DeleteGaugeCommand = ReactiveCommand.Create<int>(DeleteGauge);
            OpenGaugeEditorCommand = ReactiveCommand.CreateFromTask<object>(OpenGaugeEditor);
            CreateGaugeCommand = ReactiveCommand.Create(CreateGauge);
            ConnectToServerCommand = ReactiveCommand.Create(ConnectToServer);

            Refresh();
        }

        private void ConnectToServer()
        {
            Console.WriteLine($"[MainMenuViewModel] Connect to server");

            ConnectionStatus = GetConnectionStatus();

            Action OnConnect = () =>
            {
                Console.WriteLine("[MainMenuViewModel] We connected");

                ConnectionStatus = GetConnectionStatus();
            };

            Action<Exception?> OnDisconnect = (reason) =>
            {
                Console.WriteLine($"[MainMenuViewModel] We disconnected: {reason}");

                ConnectionStatus = GetConnectionStatus();
            };

            Action<string?> OnVehicle = (vehicleName) =>
            {
                Console.WriteLine($"[MainMenuViewModel] New vehicle '{vehicleName}'");

                ConnectionStatus = GetConnectionStatus(vehicleName);
            };

            ConnectionService.Instance.OnConnect += OnConnect;
            ConnectionService.Instance.OnDisconnect += OnDisconnect;
            ConnectionService.Instance.OnVehicle += OnVehicle;

            _ = ConnectionService.Instance.Connect();
        }

        private static string GetConnectionStatus(string? vehicleName = null)
        {
            if (ConnectionService.Instance.IsConnected && vehicleName != null)
                return $"Vehicle: {vehicleName}";

            if (ConnectionService.Instance.IsConnected && ConnectionService.Instance.LastKnownVehicleName is { } name)
                return $"Vehicle: {name}";

            if (ConnectionService.Instance.IsConnected)
                return "Connected successfully";

            if (ConnectionService.Instance.IsConnecting)
                return "Connecting..."; ;

            if (ConnectionService.Instance.LastFailReason != null)
                return $"Failed to connect: {ConnectionService.Instance.LastFailReason.Message}";

            return "";
        }

        public void Refresh()
        {
            PanelsWithIndex.Clear();

            var panels = ConfigManager.Config!.Panels;
            foreach (var (panel, i) in panels.Select((panel, i) => (panel, i)))
            {
                PanelsWithIndex.Add(new PanelEntry(i, panel));
            }

            GaugesWithIndex.Clear();

            var rootGauges = ConfigManager.Config!.Gauges;
            foreach (var (rootGauge, i) in rootGauges.Select((rootGauge, i) => (rootGauge, i)))
            {
                GaugesWithIndex.Add(new GaugeEntry(i, new ReactiveGauge(rootGauge)));
            }

            GaugesWithPath.Clear();

            var gaugesInPanels = GaugeHelper.FindGaugesReferencedByPathInAllPanels();
            foreach (var (gauge, i) in gaugesInPanels.Select((gauge, i) => (gauge, i)))
            {
                GaugesWithPath.Add(new GaugeEntry(i, new ReactiveGauge(gauge), gauge.Source));
            }

            Console.WriteLine($"[MainMenuViewViewModel] Loaded {PanelsWithIndex.Count} panels, {GaugesWithIndex.Count} root gauges, {GaugesWithPath.Count} referenced gauges");
        }

        // TODO: Move to window
        private async Task CreatePanel(Panel? existingPanel)
        {
            var newPanel = existingPanel ?? new Panel()
            {
                Name = $"Panel #{ConfigManager.Config.Panels.Count + 1}",
                Gauges = []
            };

            Console.WriteLine($"[MainMenuViewViewModel] Create panel={newPanel}");

            await ConfigManager.AddPanel(newPanel);

            Refresh();
        }

        // TODO: Move to window
        private void OpenPanelEditor(int panelIndex)
        {
            Console.WriteLine($"[MainMenuViewViewModel] Open panel editor index={panelIndex}");
            NavigationService.Instance.GoToView("PanelEditor", [panelIndex]);
        }

        // TODO: Move to window
        private async Task DeletePanel(int panelIndex)
        {
            Console.WriteLine($"[MainMenuViewViewModel] Delete index={panelIndex}");

            OnDeletePanel?.Invoke(panelIndex);
        }

        // TODO: Move to window
        private async Task DuplicatePanel(int panelIndex)
        {
            Console.WriteLine($"[MainMenuViewViewModel] Duplicate index={panelIndex}");

            var panelToDupe = ConfigManager.Config.Panels[panelIndex];
            var newPanel = panelToDupe.Clone();

            // increment number if present
            newPanel.Name = Regex.Replace(
                newPanel.Name,
                @"(\d+)$",
                m => (int.Parse(m.Value) + 1).ToString()
            );

            if (newPanel.Name == panelToDupe.Name)
                newPanel.Name += " 1";

            await CreatePanel(newPanel);

            Console.WriteLine($"[MainMenuViewViewModel] Duplicate success index={panelIndex} {panelToDupe.Name} => {newPanel.Name}");
        }

        // TODO: Move to window
        private void CreateGauge()
        {
            Console.WriteLine("[MainMenuViewViewModel] Clicked create gauge");

            OnCreateGauge?.Invoke();
        }

        private void DeleteGauge(int index)
        {
            Console.WriteLine($"[MainMenuViewViewModel] Clicked delete gauge index={index}");

            OnDeleteGauge?.Invoke(index);
        }

        // TODO: Move to window
        private async Task OpenGaugeEditor(object indexOrPath)
        {
            Console.WriteLine($"[MainMenuViewViewModel] Open gauge editor indexOrPath={indexOrPath}");

            if (indexOrPath is string gaugePath)
            {
                Console.WriteLine($"[MainMenuViewViewModel] Open gauge editor path={gaugePath}");

                var gauge = await GaugeHelper.GetGaugeByPath(gaugePath);

                if (gauge == null)
                    throw new Exception("Gauge is null");

                NavigationService.Instance.GoToView("GaugeEditor", [null, gauge]);
            }
            else if (indexOrPath is int gaugeIndex)
            {
                Console.WriteLine($"[MainMenuViewViewModel] Open gauge editor index={gaugeIndex}");

                NavigationService.Instance.GoToView("GaugeEditor", [gaugeIndex, null]);
            }
            else
            {
                throw new Exception("Need to provide an index or path");
            }
        }
    }
}

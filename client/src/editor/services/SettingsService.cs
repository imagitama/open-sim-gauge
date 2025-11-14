using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;

namespace OpenGaugeClient.Editor.Services
{
    public class SettingsService : ReactiveObject
    {
        public class SettingsData
        {
            public bool Snap { get; set; } = true;
            public int SnapAmount { get; set; } = 50;
            public bool GridVisible { get; set; } = true;
            public bool ClipVisually { get; set; } = true;
            public bool WindowBorderVisible { get; set; } = true;
            public bool SyncingWithWindow { get; set; } = true;
            public bool OverlayVisible { get; set; } = false;
        }

        private static readonly string SettingsPath = PathHelper.GetFilePath("settings.json", true);

        public static SettingsService Instance { get; } = Load();

        private bool _snap = true;
        public bool Snap
        {
            get => _snap;
            set => this.RaiseAndSetIfChanged(ref _snap, value);
        }

        private int _snapAmount = 10;
        public int SnapAmount
        {
            get => _snapAmount;
            set => this.RaiseAndSetIfChanged(ref _snapAmount, value);
        }

        private bool _gridVisible = true;
        public bool GridVisible
        {
            get => _gridVisible;
            set => this.RaiseAndSetIfChanged(ref _gridVisible, value);
        }

        private bool _clipVisually = true;
        public bool ClipVisually
        {
            get => _clipVisually;
            set => this.RaiseAndSetIfChanged(ref _clipVisually, value);
        }

        private bool _windowBorderVisible = true;
        public bool WindowBorderVisible
        {
            get => _windowBorderVisible;
            set => this.RaiseAndSetIfChanged(ref _windowBorderVisible, value);
        }

        private bool _syncingWithWindow = true;
        public bool SyncingWithWindow
        {
            get => _syncingWithWindow;
            set => this.RaiseAndSetIfChanged(ref _syncingWithWindow, value);
        }

        private bool _overlayVisible = false;
        public bool OverlayVisible
        {
            get => _overlayVisible;
            set => this.RaiseAndSetIfChanged(ref _overlayVisible, value);
        }

        public ReactiveCommand<Unit, Unit> ToggleSnapCommand { get; }
        public ReactiveCommand<int, Unit> SetSnapAmountCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleGridVisibleCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleClipVisuallyCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleWindowBorderVisibleCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleSyncingWithWindowCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleOverlayVisibleCommand { get; }

        private SettingsService()
        {
            ToggleSnapCommand = ReactiveCommand.Create(() => { Snap = !Snap; });
            SetSnapAmountCommand = ReactiveCommand.Create<int>(a => { SnapAmount = a; });
            ToggleGridVisibleCommand = ReactiveCommand.Create(() => { GridVisible = !GridVisible; });
            ToggleClipVisuallyCommand = ReactiveCommand.Create(() => { ClipVisually = !ClipVisually; });
            ToggleWindowBorderVisibleCommand = ReactiveCommand.Create(() => { WindowBorderVisible = !WindowBorderVisible; });
            ToggleSyncingWithWindowCommand = ReactiveCommand.Create(() => { SyncingWithWindow = !SyncingWithWindow; });
            ToggleOverlayVisibleCommand = ReactiveCommand.Create(() => { OverlayVisible = !OverlayVisible; });

            this.WhenAnyPropertyChanged()
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Subscribe(_ => Save());
        }

        private static SettingsService Load()
        {
            var service = new SettingsService();
            try
            {
                Console.WriteLine($"[SettingsService] Load: {SettingsPath}");
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var data = JsonSerializer.Deserialize<SettingsData>(json);
                    if (data != null)
                        service.Apply(data);
                }

                Console.WriteLine($"[SettingsService] Load failed - no file");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SettingsService] Failed to load settings: {ex}");
            }

            return service;
        }

        private void Save()
        {
            try
            {
                Console.WriteLine($"[SettingsService] Save: {SettingsPath}");

                var data = new SettingsData
                {
                    Snap = Snap,
                    SnapAmount = SnapAmount,
                    GridVisible = GridVisible,
                    ClipVisually = ClipVisually,
                    WindowBorderVisible = WindowBorderVisible,
                    SyncingWithWindow = SyncingWithWindow,
                    OverlayVisible = OverlayVisible
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsPath, json);
                Console.WriteLine($"[SettingsService] Saved settings to {SettingsPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SettingsService] Failed to save settings: {ex}");
            }
        }

        private void Apply(SettingsData data)
        {
            Snap = data.Snap;
            SnapAmount = data.SnapAmount;
            GridVisible = data.GridVisible;
            ClipVisually = data.ClipVisually;
            WindowBorderVisible = data.WindowBorderVisible;
            SyncingWithWindow = data.SyncingWithWindow;
            OverlayVisible = data.OverlayVisible;
        }
    }

    public static class ReactiveObjectExtensions
    {
        public static IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> WhenAnyPropertyChanged(this ReactiveObject obj)
        {
            return obj.Changed;
        }
    }
}

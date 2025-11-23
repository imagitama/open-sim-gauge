using Avalonia.Controls;
using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using DynamicData;
using OpenGaugeClient.Editor.Services;
using System.Reflection;
using OpenGaugeClient.Editor.Components;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Disposables;

namespace OpenGaugeClient.Editor
{
    // abstract any knowledge of the parent window by providing a *thing* that knows some numbers
    public interface IWindowGeometryProvider
    {
        double ScreenWidth { get; }
        double ScreenHeight { get; }
        double WindowWidth { get; }
        double WindowHeight { get; }
    }

    public class WindowGeometryProvider : IWindowGeometryProvider
    {
        private readonly Window _window;

        public WindowGeometryProvider(Window window)
        {
            _window = window;
        }

        public double WindowWidth => _window.Width;
        public double WindowHeight => _window.Height;

        public double ScreenWidth => _window.Screens.Primary?.Bounds.Width ?? 1920;
        public double ScreenHeight => _window.Screens.Primary?.Bounds.Height ?? 1080;
    }

    public partial class PanelEditorView : UserControl
    {
        private readonly CompositeDisposable _cleanup = new();
        public PanelEditorViewViewModel ViewModel { get; private set; }
        private Panel _panel;
        private ReactivePanel _reactivePanel;
        private int _panelIndex;
        private RenderingHelper? _renderer;
        private readonly GaugeCache _gaugeCache;
        private readonly ImageCache _imageCache;
        private readonly SKFontCache _skFontCache;
        private readonly SKFontProvider _skFontProvider;
        private readonly FontProvider _fontProvider;
        private readonly SvgCache _svgCache;
        private PanelRenderer _panelRenderer;
        private int _selectedGaugeRefIndex = -1;
        private bool _ignorePanelChanges = false;

        // for compiled bindings
        public PanelEditorView()
        {
            InitializeComponent();
        }

        public PanelEditorView(int panelIndex)
        {
            InitializeComponent();

            var panel = ConfigManager.Config!.Panels[panelIndex];

            Console.WriteLine($"Open panel editor #{panelIndex} {panel}");

            _panel = panel;
            _reactivePanel = new ReactivePanel(panel);
            _panelIndex = panelIndex;

            ViewModel = new PanelEditorViewViewModel(_reactivePanel);
            DataContext = ViewModel;

            ViewModel.GetGeometry = () =>
            {
                if (VisualRoot is not Window window)
                    throw new Exception("Window is null");

                return new WindowGeometryProvider(window);
            };

            _gaugeCache = new GaugeCache();
            _skFontCache = new SKFontCache();
            _skFontProvider = new SKFontProvider(_skFontCache);
            _svgCache = new SvgCache();
            _fontProvider = new FontProvider();
            _imageCache = new ImageCache(_skFontProvider);

            AttachedToVisualTree += (_, _) =>
            {
                SubscribeToPanelChanges();
                SubscribeToSettings();

                RebuildPanelRenderer();

                if (VisualRoot is not Window window)
                    throw new Exception("Window is null");

                // make main window overlap
                window.Activate();

                window.TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
                window.Background = Brushes.Transparent;
            };
            DetachedFromVisualTree += (_, _) =>
            {
                ViewModel.Dispose();
                _renderer?.Dispose();
                _cleanup.Dispose();
                _reactivePanel.Dispose();
                _panelRenderer?.Dispose();
                Console.WriteLine("[PanelEditorView] Cleaned up");
            };

            ViewModel.OnReset += OnReset;
            ViewModel.OnSave += OnSave;
            ViewModel.OnAddGaugeRef += () => _ = OnAddGaugeRef();
            ViewModel.OnCenter += OnCenter;
        }

        private void RebuildPanelRenderer()
        {
            if (_panelRenderer == null)
            {
                var panel = ViewModel.Panel.ToPanel();
                panel.Debug = true;

                _panelRenderer = new PanelRenderer(
                    panel,
                    _imageCache,
                    _fontProvider,
                    _svgCache,
                    GetSimVarValue,
                    isConnected: null,
                    disableRenderOnTop: true,
                    gridSize: SettingsService.Instance.GridVisible ? SettingsService.Instance.SnapAmount : null
                );

                _panelRenderer.OnMove = (newWindowPos) =>
                {
                    _ignorePanelChanges = true;

                    var newPanelPos = PanelHelper.GetPanelPositionFromWindow(ViewModel.Panel.ToPanel(), _panelRenderer.Window);

                    Console.WriteLine($"[PanelEditorView] Panel renderer moved pos={newWindowPos} panelPos={newPanelPos}");

                    ViewModel.Panel.Position = newPanelPos;

                    RebuildPanelRenderer();

                    _ignorePanelChanges = false;
                };
            }
            else
            {
                _panelRenderer.DebugGauge(ViewModel.SelectedGaugeRefIndex != -1 ? ViewModel.SelectedGaugeRefIndex : null);
                var panel = ViewModel.Panel.ToPanel();
                panel.Debug = true;
                _panelRenderer.ReplacePanel(panel);
            }
        }

        private void OnCenter()
        {
            Console.WriteLine("[PanelEditorView] On center");

            var oldPos = ViewModel.Panel.Position;

            var centeredPos = new FlexibleVector2() { X = "50%", Y = "50%" };

            ViewModel.Panel.Position = centeredPos;

            Console.WriteLine($"[PanelEditorView] On center done oldPos={oldPos} newPos={centeredPos}");
        }

        private async Task OnAddGaugeRef()
        {
            try
            {
                Console.WriteLine($"[PanelEditorView] Showing 'add gauge ref' dialog");

                if (VisualRoot is not Window window)
                    throw new Exception("Window is null");

                var dialog = new AddGaugeRefDialog();
                var ok = await dialog.ShowDialog<bool>(window!);

                if (ok)
                {
                    Console.WriteLine($"[PanelEditorView] AddGaugeRefDialog OK lastValue={dialog.LastValue}");

                    var (name, path) = dialog.LastValue;

                    Gauge gauge;

                    if (!string.IsNullOrEmpty(name))
                        gauge = GaugeHelper.GetGaugeByName(name);
                    else if (!string.IsNullOrEmpty(path))
                        gauge = await GaugeHelper.GetGaugeByPath(path);
                    else
                        throw new Exception("Need a name or path");

                    if (gauge == null)
                        throw new Exception("Need a gauge");

                    Console.WriteLine($"[PanelEditorView] Found gauge={gauge}");

                    _reactivePanel.Gauges.Edit(gauges =>
                    {
                        gauges.Add(new ReactiveGaugeRef(new GaugeRef
                        {
                            Name = name ?? null,
                            Path = path ?? null,
                            Position = new FlexibleVector2() { X = "50%", Y = "50%" },
                            Gauge = gauge
                        }));

                        ViewModel.SelectedGaugeRefIndex = gauges.Count - 1;
                    });

                    Console.WriteLine($"[PanelEditorView] Gauge ref added successfully");
                }
                else
                {
                    Console.WriteLine($"[PanelEditorView] AddGaugeRefDialog not OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PanelEditorView] Failed to add gauge ref: {ex}");
            }
        }

        private void SubscribeToPanelChanges()
        {
            if (VisualRoot is not Window window)
                throw new Exception("Window is null");

            void OnLayoutUpdated(object? sender, EventArgs e)
            {
                window.LayoutUpdated -= OnLayoutUpdated; // only run once

                RebuildPanelRenderer();
            }

            window.LayoutUpdated += OnLayoutUpdated;

            _reactivePanel.Changed.Subscribe(change =>
            {
                if (_ignorePanelChanges)
                    return;

                Console.WriteLine($"[PanelEditorView] Panel changed prop={change.PropertyName}");
                RebuildPanelRenderer();
            }).DisposeWith(_cleanup);

            ViewModel.WhenAnyValue(ViewModel => ViewModel.SelectedGaugeRefIndex)
               .Subscribe(index =>
               {
                   Console.WriteLine($"[PanelEditorView] Selected gauge ref changed index={index}");

                   RebuildPanelRenderer();
               })
               .DisposeWith(_cleanup);

            _reactivePanel.Gauges
                .Connect()
                .Publish(shared =>
                    Observable.Merge(
                        // fires when the collection structure changes
                        shared.Select(_ => "ListChanged"),

                        // fires when any property of any child changes
                        shared.MergeMany(g => g.Changed.Select(_ => "ItemChanged"))
                    )
                )
                .Subscribe(_ =>
                {
                    Console.WriteLine("[PanelEditorView] List of gauges changed");
                    RebuildPanelRenderer();
                })
                .DisposeWith(_cleanup);
        }

        private void SubscribeToSettings()
        {
            SettingsService.Instance
                    .WhenAnyValue(x => x.GridVisible, x => x.SnapAmount)
                    .Subscribe(vals =>
                    {
                        var (gridVisible, snapAmount) = vals;
                        Console.WriteLine($"[PanelEditorView] Settings changed grid={gridVisible} amount={snapAmount}");
                        _panelRenderer?.UpdateGrid(gridVisible == true ? snapAmount : null);
                    })
                    .DisposeWith(_cleanup);
        }

        private void OnReset()
        {
            Console.WriteLine($"[PanelEditorView] Reset panel");

            ViewModel.Panel.Replace(_panel);
        }

        private void OnSave(ReactivePanel panel)
        {
            Console.WriteLine($"[PanelEditorView] Save panel={panel}");

            _ = ConfigManager.SavePanel(_panelIndex, panel.ToPanel());
        }

        private object? GetSimVarValue(string name, string unit)
        {
            if (ConnectionService.Instance == null)
                return null;

            return ConnectionService.Instance.GetSimVarValue(name, unit);
        }
    }

    public class PanelEditorViewViewModel : ReactiveObject
    {
        public class GaugeRefEntry : ReactiveObject
        {
            public ReactiveGaugeRef GaugeRef { get; }

            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set => this.RaiseAndSetIfChanged(ref _isSelected, value);
            }

            public GaugeRefEntry(ReactiveGaugeRef gaugeRef)
            {
                GaugeRef = gaugeRef;
            }
        }

        private readonly IWindowGeometryProvider _geometry;
        private ReactivePanel _panel;
        public ReactivePanel Panel
        {
            get => _panel;
            set
            {
                this.RaiseAndSetIfChanged(ref _panel, value);
            }
        }
        private ReadOnlyObservableCollection<GaugeRefEntry> _gaugeRefsWithSelected;
        public ReadOnlyObservableCollection<GaugeRefEntry> GaugeRefsWithSelected => _gaugeRefsWithSelected;
        private readonly CompositeDisposable _cleanup = new();
        private int _selectedGaugeRefIndex = -1;
        public int SelectedGaugeRefIndex
        {
            get => _selectedGaugeRefIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedGaugeRefIndex, value);
        }
        [ObservableAsProperty] public ReactiveGaugeRef? SelectedGaugeRef { get; }
        public ObservableCollection<int> ScreenIndexes { get; } = new();
        public ReactiveCommand<(string name, object val, object), Unit> EditPanelCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> AddGaugeRefCommand { get; }
        public ReactiveCommand<GaugeRefEntry, Unit> SelectGaugeRefCommand { get; }
        public ReactiveCommand<GaugeRefEntry, Unit> MoveGaugeRefUpCommand { get; }
        public ReactiveCommand<GaugeRefEntry, Unit> MoveGaugeRefDownCommand { get; }
        public ReactiveCommand<(string, object, object), Unit> EditGaugeRefCommand { get; }
        public ReactiveCommand<int, Unit> MoveToScreenCommand { get; }
        public ReactiveCommand<Unit, Unit> FillScreenCommand { get; }
        public ReactiveCommand<ReactiveGaugeRef, Unit> OpenGaugeEditorCommand { get; }
        public ReactiveCommand<Unit, Unit> CenterCommand { get; }
        public ReactiveCommand<ReactiveGaugeRef, Unit> CenterGaugeRefCommand { get; }
        public ReactiveCommand<object, Unit> TransformGaugeRefCommand { get; }
        public event Action? OnGaugeRefsChanged;
        public event Action<ReactivePanel>? OnSave;
        public event Action? OnReset;
        public event Action? OnAddGaugeRef;
        public event Action? OnCenter;
        public Func<IWindowGeometryProvider> GetGeometry;

        public PanelEditorViewViewModel(ReactivePanel panel)
        {
            _panel = panel;

            ResetCommand = ReactiveCommand.Create(Reset);
            SaveCommand = ReactiveCommand.Create(Save);
            EditPanelCommand = ReactiveCommand.Create<(string, object, object)>(EditPanel);
            AddGaugeRefCommand = ReactiveCommand.Create(AddGaugeRef);
            SelectGaugeRefCommand = ReactiveCommand.Create<GaugeRefEntry>(SelectGaugeRef);
            MoveGaugeRefUpCommand = ReactiveCommand.Create<GaugeRefEntry>(MoveGaugeRefUp);
            MoveGaugeRefDownCommand = ReactiveCommand.Create<GaugeRefEntry>(MoveGaugeRefDown);
            EditGaugeRefCommand = ReactiveCommand.Create<(string, object, object)>(EditGaugeRef);
            OpenGaugeEditorCommand = ReactiveCommand.CreateFromTask<ReactiveGaugeRef>(OpenGaugeEditor);
            CenterGaugeRefCommand = ReactiveCommand.Create<ReactiveGaugeRef>(CenterGaugeRef);
            MoveToScreenCommand = ReactiveCommand.Create<int>(MoveToScreen);
            FillScreenCommand = ReactiveCommand.Create(FillScreen);
            CenterCommand = ReactiveCommand.Create(Center);
            TransformGaugeRefCommand = ReactiveCommand.Create<object>(obj => TransformGaugeRef((ReactiveGaugeRef)((dynamic)obj).Target, ((dynamic)obj).Action));

            UpdateScreens();
            BuildSelectedGaugeRef();
            BuildGaugeRefs();
        }

        public void Dispose()
        {
            _cleanup.Dispose();
        }

        private void TransformGaugeRef(ReactiveGaugeRef gaugeRef, TransformActionType type)
        {
            if (gaugeRef == null)
                throw new Exception("Gauge ref is null");

            var snapEnabled = SettingsService.Instance.Snap;
            var snapAmount = Math.Max(SettingsService.Instance.SnapAmount, 1);

            Console.WriteLine($"[PanelEditorViewViewModel] Transform gaugeRef={gaugeRef} type={type}");

            switch (type)
            {
                case TransformActionType.Upsize:
                case TransformActionType.Downsize:
                    var scale = gaugeRef.Scale;
                    var newScale = scale;
                    double scaleStep = snapEnabled ? (double)snapAmount / 100 : 0.1;

                    switch (type)
                    {
                        case TransformActionType.Upsize:
                            newScale += scaleStep;
                            break;
                        case TransformActionType.Downsize:
                            newScale -= scaleStep;
                            break;
                        default:
                            throw new Exception($"Unknown type {type}");
                    }

                    newScale = Math.Round(newScale, 1);

                    gaugeRef.Scale = newScale;

                    Console.WriteLine($"[PanelEditorViewViewModel] Transform GaugeRef scale {scale} => {newScale}");
                    break;

                case TransformActionType.Up:
                case TransformActionType.Down:
                case TransformActionType.Left:
                case TransformActionType.Right:
                    double moveStep = snapEnabled ? snapAmount : 1.0;

                    var pos = gaugeRef.Position.Resolve(Panel.Width ?? _geometry.WindowWidth, Panel.Height ?? _geometry.WindowHeight);

                    switch (type)
                    {
                        case TransformActionType.Up:
                            pos = pos with { Y = pos.Y - moveStep };
                            break;

                        case TransformActionType.Down:
                            pos = pos with { Y = pos.Y + moveStep };
                            break;

                        case TransformActionType.Left:
                            pos = pos with { X = pos.X - moveStep };
                            break;

                        case TransformActionType.Right:
                            pos = pos with { X = pos.X + moveStep };
                            break;

                        default:
                            throw new Exception($"Unknown type {type}");
                    }

                    if (snapEnabled)
                    {
                        pos = pos with
                        {
                            X = Math.Round(pos.X / snapAmount) * snapAmount,
                            Y = Math.Round(pos.Y / snapAmount) * snapAmount
                        };
                    }

                    var oldPos = gaugeRef.Position;

                    gaugeRef.Position = new FlexibleVector2()
                    {
                        X = pos.X,
                        Y = pos.Y
                    };

                    Console.WriteLine($"[PanelEditorViewViewModel] Transform gaugeref {gaugeRef} -> pos {oldPos} => {pos}");
                    break;
                default:
                    throw new Exception($"Unknown type {type}");
            }
        }

        private void BuildSelectedGaugeRef()
        {
            this.WhenAnyValue(vm => vm.SelectedGaugeRefIndex)
                .CombineLatest(
                    Panel.Gauges.Connect().ToCollection(),
                    (idx, layers) =>
                        (idx >= 0 && idx < layers.Count)
                            ? layers.ElementAt(idx)
                            : null)
                .ToPropertyEx(this, vm => vm.SelectedGaugeRef)
                .DisposeWith(_cleanup);

            this.WhenAnyValue(vm => vm.SelectedGaugeRef)
                .Subscribe(layer =>
                {
                    Console.WriteLine($"[PanelEditorViewViewModel] SelectedGaugeRef changed → {(layer != null ? layer : "(none)")}");
                })
                .DisposeWith(_cleanup);
        }

        private void BuildGaugeRefs()
        {
            var entries = _panel.Gauges
                .Connect()
                .Transform(gaugeRef => new GaugeRefEntry(gaugeRef))
                .Publish();

            entries
                .Bind(out _gaugeRefsWithSelected)
                .Subscribe()
                .DisposeWith(_cleanup);

            this.WhenAnyValue(vm => vm.SelectedGaugeRefIndex)
                .CombineLatest(entries.ToCollection())
                .Subscribe(tuple =>
                {
                    var (index, gaugeRefs) = tuple;
                    var list = gaugeRefs.ToList();
                    for (int i = 0; i < list.Count; i++)
                        list[i].IsSelected = i == index;
                })
                .DisposeWith(_cleanup);

            entries.Connect();
        }

        // TODO: Move to parent window and provide to viewmodel
        public void UpdateScreens()
        {
            Console.WriteLine($"[PanelEditorViewViewModel] Update screens");

            ScreenIndexes.Clear();

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow?.Screens is { } screens)
            {
                int screenCount = screens.ScreenCount;

                for (var i = 0; i < screenCount; i++)
                    ScreenIndexes.Add(i);
            }

            Console.WriteLine($"[PanelEditorViewViewModel] Found {ScreenIndexes.Count} screens");
        }

        private void EditPanel((string, object, object?) payload)
        {
            var (fieldName, newValue, obj) = payload;

            Console.WriteLine($"[PanelEditorViewViewModel] Edit panel {fieldName}={newValue}");

            var type = _panel.GetType();
            var prop = type.GetProperty(fieldName);

            if (prop == null)
                throw new Exception($"Could not find prop '{fieldName}' on type '{type}'");

            try
            {
                var targetType = prop.PropertyType;

                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    targetType = Nullable.GetUnderlyingType(targetType)!;

                object? convertedValue;

                if (newValue == null)
                {
                    convertedValue = null;
                }
                else
                {
                    convertedValue = Convert.ChangeType(newValue, targetType);
                }

                prop.SetValue(_panel, convertedValue);
                Console.WriteLine($"[PanelEditorViewViewModel] Set Panel.{fieldName} = {convertedValue}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PanelEditorViewViewModel] Failed to set property '{fieldName}': {ex.Message}");
            }
        }

        private void Center()
        {
            Console.WriteLine($"[PanelEditorViewViewModel] Center panel");

            // Window = true;

            // EditPanel(("Position", new FlexibleVector2() { X = "50%", Y = "50%" }, null));

            OnCenter?.Invoke();
        }

        private void FillScreen()
        {
            Console.WriteLine($"[PanelEditorViewViewModel] Make panel fill screen");

            var geometry = GetGeometry.Invoke();
            var w = geometry.ScreenWidth;
            var h = geometry.ScreenHeight;

            EditPanel(("Width", w, null));
            EditPanel(("Height", h, null));
        }

        private void Reset()
        {
            Console.WriteLine($"[PanelEditorViewViewModel] Reset panel");

            OnReset?.Invoke();
        }

        private void Save()
        {
            Console.WriteLine($"[PanelEditorViewViewModel] Save panel");

            OnSave?.Invoke(Panel);
        }

        private void CenterGaugeRef(ReactiveGaugeRef gaugeRef)
        {
            Console.WriteLine($"[PanelEditorViewViewModel] Center gaugeRef={gaugeRef}");

            EditGaugeRef(("Position", new FlexibleVector2() { X = "50%", Y = "50%" }, gaugeRef));
        }

        private void SelectGaugeRef(GaugeRefEntry entry)
        {
            Console.WriteLine($"[PanelEditorViewViewModel] Select gaugeRef entry={entry.GaugeRef}");

            var index = Panel.Gauges.Items.ToList().FindIndex(l => l.Equals(entry.GaugeRef));

            if (index == -1)
                throw new Exception($"Failed to select gauge ref: not found");

            SelectedGaugeRefIndex = index == SelectedGaugeRefIndex ? -1 : index;
        }

        private void AddGaugeRef()
        {
            Console.WriteLine($"[PanelEditorViewViewModel] Add gauge ref");

            OnAddGaugeRef?.Invoke();
        }

        private void MoveGaugeRefUp(GaugeRefEntry entry)
        {
            Console.WriteLine($"[GaugeEditorWindowViewModel] Move up gaugeRef entry={entry}");

            var newIndex = -1;

            Panel.Gauges.Edit(list =>
            {
                var index = list.IndexOf(entry.GaugeRef);
                if (index > 0)
                {
                    list.RemoveAt(index);
                    newIndex = index - 1;
                    list.Insert(newIndex, entry.GaugeRef);
                }
            });

            SelectedGaugeRefIndex = newIndex;
        }

        private void MoveGaugeRefDown(GaugeRefEntry entry)
        {
            Console.WriteLine($"[GaugeEditorWindowViewModel] Move down gaugeRef entry={entry}");

            var newIndex = -1;

            Panel.Gauges.Edit(list =>
            {
                var index = list.IndexOf(entry.GaugeRef);
                if (index >= 0 && index < list.Count - 1)
                {
                    list.RemoveAt(index);
                    newIndex = index + 1;
                    list.Insert(newIndex, entry.GaugeRef);
                }
            });

            SelectedGaugeRefIndex = newIndex;
        }

        private static PropertyInfo? GetNestedProperty(Type type, string propertyPath)
        {
            var parts = propertyPath.Split('.');
            PropertyInfo? prop = null;
            foreach (var part in parts)
            {
                prop = type.GetProperty(part);
                if (prop == null)
                    return null;

                type = prop.PropertyType;
            }

            return prop;
        }

        private static void SetNestedValue(object target, string propertyPath, object? value)
        {
            var parts = propertyPath.Split('.');
            object? current = target;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                var prop = current!.GetType().GetProperty(parts[i]);
                if (prop == null)
                    throw new Exception($"Property '{parts[i]}' not found on {current.GetType().Name}");
                current = prop.GetValue(current);
            }

            var finalProp = current!.GetType().GetProperty(parts[^1]);
            if (finalProp == null)
                throw new Exception($"Property '{parts[^1]}' not found on {current.GetType().Name}");

            finalProp.SetValue(current, value);
        }

        private void EditGaugeRef((string, object, object) payload)
        {
            var (fieldName, newValue, gaugeRef) = payload;

            if (gaugeRef == null || gaugeRef is not ReactiveGaugeRef)
                throw new Exception("Need a (reactive) gaugeRef");

            var gaugeRefIndex = SelectedGaugeRefIndex;

            Console.WriteLine($"[GaugeEditorWindowViewModel] Edit gaugeRef={gaugeRef} {fieldName}={newValue}");

            var type = gaugeRef.GetType();
            var gaugeRefProp = GetNestedProperty(type, fieldName);

            if (gaugeRefProp == null)
                throw new Exception($"[GaugeEditorWindowViewModel] Property '{fieldName}' not found on {type.Name}");

            var targetType = gaugeRefProp.PropertyType;

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                targetType = Nullable.GetUnderlyingType(targetType)!;

            object? convertedValue;

            if (newValue == null)
            {
                convertedValue = null;
            }
            else
            {
                convertedValue = Convert.ChangeType(newValue, targetType);
            }

            Console.WriteLine($"[PanelEditorViewViewModel] Setting prop {type.Name}.{fieldName} = '{convertedValue}' (originally '{newValue}')...");

            SetNestedValue(gaugeRef, fieldName, convertedValue);

            Panel.Gauges.Edit(list =>
            {
                if (gaugeRefIndex >= 0 && gaugeRefIndex < list.Count)
                {
                    list[gaugeRefIndex] = (gaugeRef as ReactiveGaugeRef)!;
                }
            });
        }

        // TODO: move to window
        private async Task OpenGaugeEditor(ReactiveGaugeRef gaugeRef)
        {
            Console.WriteLine($"[PanelEditorViewViewModel] Open gauge editor for gaugeRef={gaugeRef}");

            if (gaugeRef.Path == null && gaugeRef.Name == null)
                throw new Exception("Need a path or name");

            if (gaugeRef.Path != null)
            {
                var gauge = await GaugeHelper.GetGaugeByPath(gaugeRef.Path);

                if (gauge == null)
                    throw new Exception($"Could not find gauge by path: {gaugeRef.Path}");

                Console.WriteLine($"[PanelEditorViewViewModel] Go to gauge editor gauge={gauge}");

                NavigationService.Instance.GoToView("GaugeEditor", [null, gauge]);
            }

            if (gaugeRef.Name != null)
            {
                var rootGaugeIndex = GaugeHelper.GetIndexByName(gaugeRef.Name);

                if (rootGaugeIndex == -1)
                    throw new Exception($"Could not find gauge by name: {gaugeRef.Name}");

                Console.WriteLine($"[PanelEditorViewViewModel] Go to gauge editor rootGaugeIndex={rootGaugeIndex}");

                NavigationService.Instance.GoToView("GaugeEditor", [rootGaugeIndex, null]);
            }
        }

        private void MoveToScreen(int screenIndex)
        {
            Console.WriteLine($"[PanelEditorViewViewModel] Move to screen index={screenIndex}");

            _panel.Screen = screenIndex;

            // TODO: actually move the panel to the new screen
        }
    }
}

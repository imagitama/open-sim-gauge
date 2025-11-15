using Avalonia.Controls;
using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using DynamicData;
using OpenGaugeClient.Shared;
using OpenGaugeClient.Editor.Components;
using OpenGaugeClient.Editor.Services;
using System.Reflection;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace OpenGaugeClient.Editor
{
    public class LayerEntry : ReactiveObject
    {
        public ReactiveLayer Layer { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public LayerEntry(ReactiveLayer layer)
        {
            Layer = layer;
        }
    }

    public partial class GaugeEditorView : UserControl
    {
        public GaugeEditorViewViewModel ViewModel { get; private set; }
        private Gauge _gauge;
        private int? _gaugeIndex;
        private ReactiveGauge _reactiveGauge;
        private readonly Image _imageControl;
        private RenderingHelper? _renderer;
        private GaugeRenderer? _gaugeRenderer;
        private bool _isRendering;
        private readonly GaugeCache _gaugeCache;
        private readonly ImageCache _imageCache;
        private readonly FontCache _fontCache;
        private readonly FontProvider _fontProvider;
        private readonly SvgCache _svgCache;
        private readonly CompositeDisposable _cleanup = new();

        // for compiled bindings
        public GaugeEditorView()
        {
            InitializeComponent();
        }

        public GaugeEditorView(int? gaugeIndex, Gauge? gaugeFromPath)
        {
            InitializeComponent();

            var gauge = gaugeFromPath != null ? gaugeFromPath : gaugeIndex != null ? ConfigManager.Config!.Gauges[(int)gaugeIndex] : throw new Exception("Need an index or gauge from path");
            _gaugeIndex = gaugeIndex;
            _gauge = gauge;
            _reactiveGauge = new ReactiveGauge(gauge);

            Console.WriteLine($"[GaugeEditorView] Initialize gauge={gauge} gaugeIndex={gaugeIndex} gaugeFromPath={gaugeFromPath}");

            ViewModel = new GaugeEditorViewViewModel(_reactiveGauge);
            DataContext = ViewModel;

            _gaugeCache = new GaugeCache();
            _fontCache = new FontCache();
            _fontProvider = new FontProvider(_fontCache);
            _svgCache = new SvgCache();
            _imageCache = new ImageCache(_fontProvider);

            var image = this.FindControl<Image>("RenderTargetImage") ?? throw new Exception("No image");
            _renderer = new RenderingHelper(image, RenderFrameAsync, ConfigManager.Config!.Fps, VisualRoot as Window);

            AttachedToVisualTree += (_, _) =>
            {
                if (VisualRoot is not Window window)
                    throw new Exception("Window is null");

                WindowHelper.CenterWindowWithoutFrame(window);

                RebuildGaugeRenderer();

                SubscribeToGaugeChanges();

                _renderer.Start();
            };
            DetachedFromVisualTree += (_, _) =>
            {
                _renderer.Dispose();
                _cleanup.Dispose();
                _reactiveGauge.Dispose();

                Console.WriteLine("[GaugeEditorView] Cleaned up");
            };

            ViewModel.OnReset += OnReset;
            ViewModel.OnSave += OnSave;
            ViewModel.OnOpenSource += OnOpenSource;
        }

        private void SubscribeToGaugeChanges()
        {
            _reactiveGauge
                .WhenAnyChange()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => RebuildGaugeRenderer())
                .DisposeWith(_cleanup);

            ViewModel.WhenAnyValue(ViewModel => ViewModel.SelectedLayerIndex)
               .Subscribe(index =>
               {
                   Console.WriteLine($"[GaugeEditorView] Selected layer changed index={index}");

                   RebuildGaugeRenderer();
               })
               .DisposeWith(_cleanup);
        }

        private async void OnReset()
        {
            Console.WriteLine($"[GaugeEditorView] Reset gauge");

            ViewModel.Gauge.Replace(_gauge);
        }

        private async void OnSave(ReactiveGauge gauge)
        {
            Console.WriteLine($"[GaugeEditorView] Save '{gauge}'");

            if (_gaugeIndex != null)
            {
                _ = ConfigManager.SaveGauge((int)_gaugeIndex, gauge.ToModel());
            }
            else
            {
                _ = GaugeHelper.SaveGaugeToFile(gauge.ToModel());
            }
        }

        private async void OnOpenSource()
        {
            Console.WriteLine($"[GaugeEditorView] Open gauge source");

            if (_gauge.Source == null)
                throw new Exception("Gauge source is null");

            FileHelper.RevealFile(_gauge.Source);
        }

        private object? GetSimVarValue(string name, string unit)
        {
            // simVarValues.TryGetValue((name, unit), out var v);
            // return v;
            return null;
        }

        private void RebuildGaugeRenderer()
        {
            if (VisualRoot is not Window window)
                return;

            var halfX = window.Bounds.Width / 2;
            var halfY = window.Bounds.Height / 2;
            var snappedX = Math.Round(halfX / SettingsService.Instance.SnapAmount) * SettingsService.Instance.SnapAmount;
            var snappedY = Math.Round(halfY / SettingsService.Instance.SnapAmount) * SettingsService.Instance.SnapAmount;

            Console.WriteLine($"[GaugeEditorView] Rebuild renderer at {snappedX},{snappedY}");

            var gaugeToRender = ViewModel.Gauge.ToModel();

            gaugeToRender.Layers = gaugeToRender.Layers.Select((x, i) => { x.Debug = ViewModel.SelectedLayerIndex == i; return x; }).ToList();

            _gaugeRenderer = new GaugeRenderer(
                gaugeToRender,
                new GaugeRef() { Position = { X = snappedX, Y = snappedY }, Gauge = null, Path = ViewModel.Gauge.Source },
                (int)Width,
                (int)Height,
                renderScaling: window.RenderScaling,
                _imageCache,
                _fontProvider,
                _svgCache,
                GetSimVarValue,
                debug: true
            );
        }

        public async Task RenderFrameAsync(DrawingContext ctx)
        {
            // handle unmount
            if (VisualRoot is not Window window)
                return;

            if (SettingsService.Instance.GridVisible)
                RenderingHelper.DrawGrid(ctx, (int)window.Width, (int)window.Height, SettingsService.Instance.SnapAmount);

            if (_gaugeRenderer == null)
                return;

            _gaugeRenderer?.DrawGaugeLayers(
                ctx,
                useCachedPositions: false,
                disableClipping: !SettingsService.Instance.ClipVisually
            );
        }
    }

    public class GaugeEditorViewViewModel : ReactiveObject
    {
        private ReactiveGauge _gauge;
        public ReactiveGauge Gauge
        {
            get => _gauge;
            set => this.RaiseAndSetIfChanged(ref _gauge, value);
        }
        private ReadOnlyObservableCollection<LayerEntry> _layersWithSelected;
        public ReadOnlyObservableCollection<LayerEntry> LayersWithSelected => _layersWithSelected;
        private readonly CompositeDisposable _cleanup = new();
        private int _selectedLayerIndex = -1;
        public int SelectedLayerIndex
        {
            get => _selectedLayerIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedLayerIndex, value);
        }
        [ObservableAsProperty] public ReactiveLayer? SelectedLayer { get; }
        public ObservableCollection<int> ScreenIndexes { get; } = new();
        public ReactiveCommand<(string name, object val, object), Unit> EditGaugeCommand { get; }
        public ReactiveCommand<Unit, Unit> AddLayerCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<LayerEntry, Unit> SelectLayerCommand { get; }
        public ReactiveCommand<LayerEntry, Unit> DuplicateLayerCommand { get; }
        public ReactiveCommand<LayerEntry, Unit> DeleteLayerCommand { get; }
        public ReactiveCommand<LayerEntry, Unit> MoveLayerUpCommand { get; }
        public ReactiveCommand<LayerEntry, Unit> MoveLayerDownCommand { get; }
        public ReactiveCommand<(string, object, object), Unit> EditLayerCommand { get; }
        public ReactiveCommand<object, Unit> TransformLayerCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenSourceCommand { get; }
        public ReactiveCommand<ReactiveLayer, Unit> MakeLayerFillCommand { get; }
        public ReactiveCommand<ReactiveLayer, Unit> CenterLayerCommand { get; }
        public event Action? OnReset;
        public event Action? OnOpenSource;
        public event Action<ReactiveGauge>? OnSave;
        private TransformDef? _transformDef;
        public TransformDef? TransformDef
        {
            get => _transformDef;
            set => this.RaiseAndSetIfChanged(ref _transformDef, value);
        }
        public bool TransformEnabled
        {
            get => TransformDef != null;
            set
            {
                if (value && TransformDef == null)
                    TransformDef = new TransformDef()
                    {
                        TranslateX = null,
                        TranslateY = null,
                        Rotate = null,
                        Path = null
                    };
                else if (!value)
                    TransformDef = null;

                this.RaisePropertyChanged(nameof(TransformEnabled));
            }
        }

        public GaugeEditorViewViewModel(ReactiveGauge reactiveGauge)
        {
            _gauge = reactiveGauge;

            AddLayerCommand = ReactiveCommand.Create(AddLayer);
            ResetCommand = ReactiveCommand.Create(Reset);
            SaveCommand = ReactiveCommand.Create(Save);
            SelectLayerCommand = ReactiveCommand.Create<LayerEntry>(SelectLayer);
            DuplicateLayerCommand = ReactiveCommand.Create<LayerEntry>(DuplicateLayer);
            DeleteLayerCommand = ReactiveCommand.Create<LayerEntry>(DeleteLayer);
            MoveLayerUpCommand = ReactiveCommand.Create<LayerEntry>(MoveLayerUp);
            MoveLayerDownCommand = ReactiveCommand.Create<LayerEntry>(MoveLayerDown);
            EditLayerCommand = ReactiveCommand.Create<(string, object, object)>(EditLayer);
            EditGaugeCommand = ReactiveCommand.Create<(string, object, object)>(EditGauge);
            TransformLayerCommand = ReactiveCommand.Create<object>(obj => TransformLayer((ReactiveLayer)((dynamic)obj).Target, ((dynamic)obj).Action));
            OpenSourceCommand = ReactiveCommand.Create(OpenSource);
            MakeLayerFillCommand = ReactiveCommand.Create<ReactiveLayer>(MakeLayerFill);
            CenterLayerCommand = ReactiveCommand.Create<ReactiveLayer>(CenterLayer);

            BuildLayers();
            BuildSelectedLayer();
        }

        private void MakeLayerFill(ReactiveLayer layer)
        {
            Console.WriteLine($"[GaugeEditorViewViewModel] Fill layer={layer}");

            layer.Width = null;
            layer.Height = null;
        }

        private void CenterLayer(ReactiveLayer layer)
        {
            Console.WriteLine($"[GaugeEditorViewViewModel] Center layer={layer}");

            layer.Position = new FlexibleVector2() { X = "50%", Y = "50%" };
        }

        private void BuildSelectedLayer()
        {
            this.WhenAnyValue(vm => vm.SelectedLayerIndex)
                .CombineLatest(
                    Gauge.Layers.Connect().ToCollection(),
                    (idx, layers) =>
                        (idx >= 0 && idx < layers.Count)
                            ? layers.ElementAt(idx)
                            : null)
                .ToPropertyEx(this, vm => vm.SelectedLayer)
                .DisposeWith(_cleanup);

            // this.WhenAnyValue(vm => vm.SelectedLayer)
            //     .Subscribe(layer =>
            //     {
            //         Console.WriteLine($"[GaugeEditorViewViewModel] SelectedLayer changed → {(layer != null ? layer : "(none)")}");
            //     })
            //     .DisposeWith(_cleanup);
        }

        private void BuildLayers()
        {
            var layerEntries = _gauge.Layers
                .Connect()
                .Transform(layer => new LayerEntry(layer))
                .Publish();

            layerEntries
                .Bind(out _layersWithSelected)
                .Subscribe()
                .DisposeWith(_cleanup);

            this.WhenAnyValue(vm => vm.SelectedLayerIndex)
                .CombineLatest(layerEntries.ToCollection())
                .Subscribe(tuple =>
                {
                    var (index, layers) = tuple;
                    var list = layers.ToList();
                    for (int i = 0; i < list.Count; i++)
                        list[i].IsSelected = i == index;
                })
                .DisposeWith(_cleanup);

            layerEntries.Connect();
        }

        private void OpenSource()
        {
            Console.WriteLine($"[GaugeEditorViewViewModel] Open source");

            OnOpenSource?.Invoke();
        }

        private void Reset()
        {
            Console.WriteLine($"[GaugeEditorViewViewModel] Reset gauge");

            OnReset?.Invoke();
        }

        private void Save()
        {
            Console.WriteLine($"[GaugeEditorViewViewModel] Save gauge");

            OnSave?.Invoke(Gauge);
        }

        private void EditGauge((string, object, object) payload)
        {
            var (fieldName, newValue, obj) = payload;

            Console.WriteLine($"[GaugeEditorViewViewModel] Edit gauge {fieldName}={newValue}");

            var type = _gauge.GetType();
            var (target, prop) = GetNestedPropertyAndTarget(_gauge, fieldName);


            if (target == null)
            {
                Console.WriteLine($"[GaugeEditorViewViewModel] Target not found for property '{fieldName}' on type {type.Name}");
                return;
            }
            if (prop == null)
            {
                Console.WriteLine($"[GaugeEditorViewViewModel] Property '{fieldName}' not found on type {type.Name}");
                return;
            }

            var targetType = prop.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                object? convertedValue;
                if (newValue == null || (newValue is string s && string.IsNullOrWhiteSpace(s)))
                {
                    convertedValue = null;
                }
                else if (underlyingType.IsEnum)
                {
                    convertedValue = Enum.Parse(underlyingType, newValue.ToString()!, ignoreCase: true);
                }
                else
                {
                    convertedValue = Convert.ChangeType(newValue, underlyingType);
                }

                Console.WriteLine($"[GaugeEditorViewViewModel] Converting value '{newValue}' to type '{prop.PropertyType}' convertedValue='{convertedValue}'");

                Console.WriteLine($"[GaugeEditorViewViewModel] Setting '{fieldName}' = {convertedValue}");

                prop.SetValue(target, convertedValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GaugeEditorViewViewModel] Failed to change type and set gauge property '{fieldName}': {ex.Message}");
            }
        }

        private void SelectLayer(LayerEntry layer)
        {
            Console.WriteLine($"[GaugeEditorViewViewModel] Select layer={layer.Layer}");

            var index = _gauge.Layers.Items.ToList().FindIndex(l => l.Equals(layer.Layer));

            if (index == -1)
                throw new Exception($"Failed to select layer: not found");

            SelectedLayerIndex = index == SelectedLayerIndex ? -1 : index;
        }

        private void AddLayer()
        {
            var newLayer = new ReactiveLayer(new Layer { Name = $"Layer {_gauge.Layers.Count + 1}" });

            Console.WriteLine($"[GaugeEditorViewViewModel] Add layer={newLayer}");

            _gauge.Layers.Edit(list =>
            {
                list.Add(newLayer);
                int index = list.Count - 1;
                SelectedLayerIndex = index;
            });
        }

        private void DeleteLayer(LayerEntry layerEntry)
        {
            Console.WriteLine($"[GaugeEditorViewViewModel] Delete layer={layerEntry.Layer}");

            _gauge.Layers.Edit(list =>
            {
                var index = list.IndexOf(layerEntry.Layer);

                list.RemoveAt(index);
            });
        }

        private void DuplicateLayer(LayerEntry layerEntry)
        {
            Console.WriteLine($"[GaugeEditorViewViewModel] Duplicate layer={layerEntry.Layer}");

            _gauge.Layers.Edit(list =>
            {
                var index = list.IndexOf(layerEntry.Layer);

                if (index == -1)
                    throw new Exception($"Cannot duplicate layer: not found");

                var newLayer = layerEntry.Layer.Clone();

                list.Add(newLayer);
            });
        }

        private void MoveLayerUp(LayerEntry layer)
        {
            Console.WriteLine($"[GaugeEditorViewViewModel] Move up layer={layer}");

            var newIndex = -1;

            _gauge.Layers.Edit(list =>
            {
                var index = list.IndexOf(layer.Layer);
                if (index > 0)
                {
                    list.RemoveAt(index);
                    newIndex = index - 1;
                    list.Insert(newIndex, layer.Layer);
                }
            });

            SelectedLayerIndex = newIndex;
        }

        private void MoveLayerDown(LayerEntry layer)
        {
            Console.WriteLine($"[GaugeEditorViewViewModel] Move down layer={layer}");

            var newIndex = -1;

            _gauge.Layers.Edit(list =>
            {
                var index = list.IndexOf(layer.Layer);
                if (index >= 0 && index < list.Count - 1)
                {
                    list.RemoveAt(index);
                    newIndex = index + 1;
                    list.Insert(newIndex, layer.Layer);
                }
            });

            SelectedLayerIndex = newIndex;
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

        private static (object? target, PropertyInfo? prop) GetNestedPropertyAndTarget(object root, string propertyPath)
        {
            var parts = propertyPath.Split('.');
            object? current = root;
            Type currentType = root.GetType();
            PropertyInfo? prop = null;

            foreach (var part in parts)
            {
                prop = currentType.GetProperty(part);
                if (prop == null)
                    return (null, null);

                if (part != parts.Last())
                {
                    current = prop.GetValue(current);
                    if (current == null)
                        return (null, null);
                    currentType = current.GetType();
                }
            }

            return (current, prop);
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

        private void EditLayer((string, object, object) payload)
        {
            var (fieldName, newValue, layer) = payload;

            if (layer == null || layer is not ReactiveLayer)
                throw new Exception("Need a (reactive) layer");

            var layerIndex = SelectedLayerIndex;

            Console.WriteLine($"[GaugeEditorViewViewModel] Edit layer={layer} {fieldName}={newValue}");

            var type = layer.GetType();
            var layerProp = GetNestedProperty(type, fieldName);

            if (layerProp == null)
                throw new Exception($"[GaugeEditorViewViewModel] Property '{fieldName}' not found on {type.Name}");

            var targetType = layerProp.PropertyType;

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                targetType = Nullable.GetUnderlyingType(targetType)!;

            object? convertedValue;

            if (newValue == null)
            {
                convertedValue = null;
            }
            else if (newValue is string s && string.IsNullOrWhiteSpace(s))
            {
                convertedValue = null;
            }
            else
            {
                convertedValue = Convert.ChangeType(newValue, targetType);
            }

            Console.WriteLine($"[GaugeEditorViewViewModel] Setting prop '{type.Name}.{fieldName}' = '{convertedValue}' ('{newValue}')...");

            SetNestedValue(layer, fieldName, convertedValue);

            _gauge.Layers.Edit(list =>
            {
                if (layerIndex >= 0 && layerIndex < list.Count)
                {
                    list[layerIndex] = (layer as ReactiveLayer)!;
                }
            });
        }

        private void TransformLayer(ReactiveLayer layer, TransformActionType type)
        {
            if (layer == null)
                throw new Exception("Layer is null");

            var snapEnabled = SettingsService.Instance.Snap;
            var snapAmount = Math.Max(SettingsService.Instance.SnapAmount, 1);

            switch (type)
            {
                case TransformActionType.Upsize:
                case TransformActionType.Downsize:
                    var width = layer.Width ?? _gauge.Width;
                    var height = layer.Height ?? _gauge.Height;
                    var newWidth = width;
                    var newHeight = height;
                    double sizeStep = snapEnabled ? snapAmount : 1;

                    switch (type)
                    {
                        case TransformActionType.Upsize:
                            newWidth += sizeStep;
                            newHeight += sizeStep;
                            break;
                        case TransformActionType.Downsize:
                            newWidth -= sizeStep;
                            newHeight -= sizeStep;
                            break;
                    }

                    layer.Width = newWidth;
                    layer.Height = newHeight;

                    Console.WriteLine($"[GaugeEditorViewViewModel] Scale layer={layer} {width}x{height} => {newWidth}x{newHeight}");
                    break;

                case TransformActionType.Up:
                case TransformActionType.Down:
                case TransformActionType.Left:
                case TransformActionType.Right:
                    double moveStep = snapEnabled ? snapAmount : 1.0;

                    var pos = layer.Position.Resolve(_gauge.Width, _gauge.Height);

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
                    }

                    if (snapEnabled)
                    {
                        pos = pos with
                        {
                            X = Math.Round(pos.X / snapAmount) * snapAmount,
                            Y = Math.Round(pos.Y / snapAmount) * snapAmount
                        };
                    }

                    var oldPos = layer.Position;

                    layer.Position = new FlexibleVector2()
                    {
                        X = pos.X,
                        Y = pos.Y
                    };

                    Console.WriteLine($"[GaugeEditorViewViewModel] Re-position layer={layer} {oldPos} => {pos}");
                    break;

                case TransformActionType.CW:
                case TransformActionType.CCW:
                    var oldRotate = layer.Rotate;
                    var newRotate = oldRotate;
                    double rotateStep = snapEnabled ? snapAmount : 1.0;

                    switch (type)
                    {
                        case TransformActionType.CW:
                            newRotate += rotateStep;
                            break;

                        case TransformActionType.CCW:
                            newRotate -= rotateStep;
                            break;
                    }

                    if (snapEnabled)
                    {
                        newRotate = Math.Round(newRotate / snapAmount) * snapAmount;
                    }

                    layer.Rotate = newRotate;

                    Console.WriteLine($"[GaugeEditorViewViewModel] Rotate layer={layer} {oldRotate} => {newRotate}");
                    break;
            }
        }
    }
}

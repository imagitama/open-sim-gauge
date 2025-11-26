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
    public class SvgOperationEntry : ReactiveObject
    {
        public ReactiveSvgOperation SvgOperation { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public SvgOperationEntry(ReactiveSvgOperation svgOperation)
        {
            SvgOperation = svgOperation;
        }
    }

    public class SvgLayerEntry : ReactiveObject
    {
        public ReactiveSvgLayer SvgLayer { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public SvgLayerEntry(ReactiveSvgLayer svgLayer)
        {
            SvgLayer = svgLayer;
        }
    }

    public partial class SvgCreatorEditorView : UserControl
    {
        public SvgCreatorEditorViewViewModel ViewModel { get; private set; }
        private SvgCreator _svgCreator;
        private ReactiveSvgCreator _reactiveSvgCreator;
        private RenderingHelper? _renderer;
        private SvgCreatorRenderer? _svgCreatorRenderer;
        private readonly ImageCache _imageCache;
        private readonly SKFontCache _skFontCache;
        private readonly SKFontProvider _skFontProvider;
        private readonly FontProvider _fontProvider;
        private readonly SvgCache _svgCache;
        private readonly CompositeDisposable _cleanup = [];

        // for compiled bindings
        public SvgCreatorEditorView()
        {
            InitializeComponent();
        }

        public SvgCreatorEditorView(SvgCreator svgCreator)
        {
            InitializeComponent();

            _svgCreator = svgCreator;
            _reactiveSvgCreator = new ReactiveSvgCreator(svgCreator);

            Console.WriteLine($"[SvgCreatorEditorView] Initialize creator={svgCreator}");

            ViewModel = new SvgCreatorEditorViewViewModel(_reactiveSvgCreator);
            DataContext = ViewModel;

            _skFontCache = new SKFontCache();
            _skFontProvider = new SKFontProvider(_skFontCache);
            _fontProvider = new FontProvider();
            _svgCache = new SvgCache();
            _imageCache = new ImageCache(_skFontProvider);

            var image = this.FindControl<Image>("RenderTargetImage") ?? throw new Exception("No image");
            _renderer = new RenderingHelper(image, RenderFrameAsync, ConfigManager.Config!.Fps, VisualRoot as Window);

            AttachedToVisualTree += (_, _) =>
            {
                if (VisualRoot is not Window window)
                    throw new Exception("Window is null");

                WindowHelper.CenterWindowWithoutFrame(window);

                RebuildSvgCreatorRenderer();

                SubscribeToSvgCreatorChanges();
                SubscribeToSettings();

                _renderer.Start();
            };
            DetachedFromVisualTree += (_, _) =>
            {
                _renderer.Dispose();
                _cleanup.Dispose();
                _reactiveSvgCreator.Dispose();

                Console.WriteLine("[SvgCreatorEditorView] Cleaned up");
            };

            ViewModel.OnReset += OnReset;
            ViewModel.OnSave += OnSave;
            ViewModel.OnGenerate += OnGenerate;
        }

        private void SubscribeToSettings()
        {
            SettingsService.Instance
                .WhenAnyValue(x => x.GridVisible, x => x.SnapAmount)
                .Subscribe(vals =>
                {
                    var (gridVisible, snapAmount) = vals;

                    Console.WriteLine($"[SvgCreatorEditorView] Settings changed grid={gridVisible} amount={snapAmount}");

                    RebuildSvgCreatorRenderer();
                })
                .DisposeWith(_cleanup);
        }

        private void SubscribeToSvgCreatorChanges()
        {
            _reactiveSvgCreator.Changed
                .Subscribe(_ => RebuildSvgCreatorRenderer())
                .DisposeWith(_cleanup);

            var layers = _reactiveSvgCreator.Layers.Connect();

            var layerChanges = ObservableListEx
                .MergeMany(
                    layers,
                    layer =>
                    {
                        return layer.Changed.Select(_ => Unit.Default);
                    }
                );
            var operationChanges = ObservableListEx
                .MergeMany(
                    layers,
                    layer => layer.Operations.Connect().Select(_ => Unit.Default)
                );
            var operationPropertyChanges = ObservableListEx.MergeMany(
                    layers,
                    layer => layer.Operations
                        .Connect()
                        .MergeMany(op => op.Changed.Select(_ => Unit.Default))
                );

            layerChanges
                .Merge(operationChanges)
                .Merge(operationPropertyChanges)
                .Subscribe(_ =>
                {
                    Console.WriteLine($"[SvgCreatorEditorView] Layers or operations changed");
                    RebuildSvgCreatorRenderer();
                })
                .DisposeWith(_cleanup);

            ViewModel.WhenAnyValue(ViewModel => ViewModel.SelectedSvgLayerIndex, ViewModel => ViewModel.SelectedSvgOperationIndex)
               .Subscribe(indexes =>
               {
                   Console.WriteLine($"[SvgCreatorEditorView] Selected layer changed indexes={indexes}");
                   RebuildSvgCreatorRenderer();
               })
               .DisposeWith(_cleanup);
        }

        private async void OnReset()
        {
            Console.WriteLine($"[SvgCreatorEditorView] Reset svg creator");

            ViewModel.SvgCreator.Replace(_svgCreator);
        }

        private async void OnSave(ReactiveSvgCreator reactiveSvgCreator)
        {
            Console.WriteLine($"[SvgCreatorEditorView] Save creator={reactiveSvgCreator}");

            if (reactiveSvgCreator.Source == null)
                throw new Exception("Source is null");

            _ = SvgCreatorUtils.SaveSvgCreator(reactiveSvgCreator.ToModel(), reactiveSvgCreator.Source);
        }

        private async void OnGenerate(ReactiveSvgCreator reactiveSvgCreator)
        {
            Console.WriteLine($"[SvgCreatorEditorView] Generate creator={reactiveSvgCreator}");

            var layers = reactiveSvgCreator.Layers.Items.Select(x => x.ToModel()).ToArray();
            var outputPath = reactiveSvgCreator.Source ?? throw new Exception("Need a source");
            var width = reactiveSvgCreator.Width;
            var height = reactiveSvgCreator.Height;

            foreach (var layer in layers)
            {
                Console.WriteLine($"[SvgCreatorEditorView] Generate layer={layer}");
                var ops = layer.Operations.ToArray();

                var dirPath = Path.GetDirectoryName(reactiveSvgCreator.Source);
                var svgPath = Path.Combine(dirPath!, $"{layer.Name}.svg");

                await SvgBuilder.BuildAndOutput(ops, svgPath, width, height, layer.Shadow?.ToConfig());
            }
        }

        // private async void OnOpenSource()
        // {
        //     Console.WriteLine($"[SvgCreatorEditorView] Open svg creator source");

        //     if (_svgCreator.Source == null)
        //         throw new Exception("Gauge source is null");

        //     FileHelper.RevealFile(_svgCreator.Source);
        // }

        private void RebuildSvgCreatorRenderer()
        {
            if (VisualRoot is not Window window)
                return;

            var svgCreator = ViewModel.SvgCreator.ToModel();

            // svgCreator.Grid = SettingsService.Instance.GridVisible ? SettingsService.Instance.SnapAmount : null;

            // svgCreator.Operations = svgCreator.Operations.Select((x, i) => { x.Debug = ViewModel.SelectedSvgLayerIndex == i; return x; }).ToList();

            Console.WriteLine($"[SvgCreatorEditorView] Rebuild renderer");

            _svgCreatorRenderer = new SvgCreatorRenderer(
                svgCreator,
                position: new FlexibleVector2 { X = "50%", Y = "50%" },
                (int)window.Width,
                (int)window.Height,
                renderScaling: window.RenderScaling,
                _imageCache,
                _fontProvider,
                _svgCache,
                debug: true
            );
        }

        public async Task RenderFrameAsync(DrawingContext ctx)
        {
            // handle unmount
            if (VisualRoot is not Window window)
                return;

            if (_svgCreatorRenderer == null)
                return;

            var layerForceRotations = ViewModel.SvgCreator.Layers.Items.Select(layer => layer.ForceRotate).ToList();

            _svgCreatorRenderer?.DrawSvgLayers(
                ctx,
                layerForceRotations
            );
        }
    }

    public class SvgCreatorEditorViewViewModel : ReactiveObject
    {
        private ReactiveSvgCreator _svgCreator;
        public ReactiveSvgCreator SvgCreator
        {
            get => _svgCreator;
            set => this.RaiseAndSetIfChanged(ref _svgCreator, value);
        }
        private ReadOnlyObservableCollection<SvgOperationEntry> _operationsWithSelected;
        public ReadOnlyObservableCollection<SvgOperationEntry> OperationsWithSelected => _operationsWithSelected;
        private ReadOnlyObservableCollection<SvgLayerEntry> _layersWithSelected;
        public ReadOnlyObservableCollection<SvgLayerEntry> LayersWithSelected => _layersWithSelected;
        private readonly CompositeDisposable _cleanup = new();
        private int _selectedLayerIndex = -1;
        public int SelectedSvgLayerIndex
        {
            get => _selectedLayerIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedLayerIndex, value);
        }
        private int _selectedOperationIndex = -1;
        public int SelectedSvgOperationIndex
        {
            get => _selectedOperationIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedOperationIndex, value);
        }
        [ObservableAsProperty] public ReactiveSvgLayer? SelectedSvgLayer { get; }
        [ObservableAsProperty] public ReactiveSvgOperation? SelectedSvgOperation { get; }
        public ReactiveCommand<(string name, object val, object), Unit> EditSvgCreatorCommand { get; }
        public ReactiveCommand<Unit, Unit> AddSvgLayerCommand { get; }
        public ReactiveCommand<SvgOperationType, Unit> AddSvgOperationCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> GenerateCommand { get; }
        public ReactiveCommand<SvgLayerEntry, Unit> SelectSvgLayerCommand { get; }
        public ReactiveCommand<SvgLayerEntry, Unit> DuplicateSvgLayerCommand { get; }
        public ReactiveCommand<SvgLayerEntry, Unit> DeleteSvgLayerCommand { get; }
        public ReactiveCommand<SvgLayerEntry, Unit> MoveSvgLayerUpCommand { get; }
        public ReactiveCommand<SvgLayerEntry, Unit> MoveSvgLayerDownCommand { get; }
        public ReactiveCommand<(string, object, object), Unit> EditSvgLayerCommand { get; }
        public ReactiveCommand<SvgOperationEntry, Unit> SelectSvgOperationCommand { get; }
        public ReactiveCommand<SvgOperationEntry, Unit> DuplicateSvgOperationCommand { get; }
        public ReactiveCommand<SvgOperationEntry, Unit> DeleteSvgOperationCommand { get; }
        public ReactiveCommand<SvgOperationEntry, Unit> MoveSvgOperationUpCommand { get; }
        public ReactiveCommand<SvgOperationEntry, Unit> MoveSvgOperationDownCommand { get; }
        public ReactiveCommand<(string, object, object), Unit> EditSvgOperationCommand { get; }
        public ReactiveCommand<object, Unit> TransformSvgOperationCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenSourceCommand { get; }
        public ReactiveCommand<ReactiveSvgOperation, Unit> MakeSvgOperationFillCommand { get; }
        public ReactiveCommand<ReactiveSvgOperation, Unit> CenterSvgOperationCommand { get; }
        public event Action? OnReset;
        public event Action? OnOpenSource;
        public event Action<ReactiveSvgCreator>? OnSave;
        public event Action<ReactiveSvgCreator>? OnGenerate;

        public SvgCreatorEditorViewViewModel(ReactiveSvgCreator reactiveGauge)
        {
            _svgCreator = reactiveGauge;

            AddSvgLayerCommand = ReactiveCommand.Create(AddSvgLayer);
            AddSvgOperationCommand = ReactiveCommand.Create<SvgOperationType>(AddSvgOperation);
            ResetCommand = ReactiveCommand.Create(Reset);
            SaveCommand = ReactiveCommand.Create(Save);
            GenerateCommand = ReactiveCommand.Create(Generate);
            SelectSvgLayerCommand = ReactiveCommand.Create<SvgLayerEntry>(SelectSvgLayer);
            DuplicateSvgLayerCommand = ReactiveCommand.Create<SvgLayerEntry>(DuplicateSvgLayer);
            DeleteSvgLayerCommand = ReactiveCommand.Create<SvgLayerEntry>(DeleteSvgLayer);
            MoveSvgLayerUpCommand = ReactiveCommand.Create<SvgLayerEntry>(MoveSvgLayerUp);
            MoveSvgLayerDownCommand = ReactiveCommand.Create<SvgLayerEntry>(MoveSvgLayerDown);
            EditSvgLayerCommand = ReactiveCommand.Create<(string, object, object)>(EditSvgLayer);
            SelectSvgOperationCommand = ReactiveCommand.Create<SvgOperationEntry>(SelectSvgOperation);
            DuplicateSvgOperationCommand = ReactiveCommand.Create<SvgOperationEntry>(DuplicateSvgOperation);
            DeleteSvgOperationCommand = ReactiveCommand.Create<SvgOperationEntry>(DeleteSvgOperation);
            MoveSvgOperationUpCommand = ReactiveCommand.Create<SvgOperationEntry>(MoveSvgOperationUp);
            MoveSvgOperationDownCommand = ReactiveCommand.Create<SvgOperationEntry>(MoveSvgOperationDown);
            EditSvgOperationCommand = ReactiveCommand.Create<(string, object, object)>(EditSvgOperation);
            EditSvgCreatorCommand = ReactiveCommand.Create<(string, object, object)>(EditSvgCreator);
            TransformSvgOperationCommand = ReactiveCommand.Create<object>(obj => TransformSvgOperation((ReactiveSvgOperation)((dynamic)obj).Target, ((dynamic)obj).Action));
            OpenSourceCommand = ReactiveCommand.Create(OpenSource);
            MakeSvgOperationFillCommand = ReactiveCommand.Create<ReactiveSvgOperation>(MakeSvgOperationFill);
            CenterSvgOperationCommand = ReactiveCommand.Create<ReactiveSvgOperation>(CenterSvgOperation);

            BuildLayers();
            BuildSelectedSvgLayer();
        }

        private void MakeSvgOperationFill(ReactiveSvgOperation operation)
        {
            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Fill operation={operation}");

            operation.Width = null;
            operation.Height = null;
        }

        private void CenterSvgOperation(ReactiveSvgOperation operation)
        {
            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Center operation={operation}");

            operation.Position = new FlexibleVector2() { X = "50%", Y = "50%" };
        }

        private void BuildSelectedSvgLayer()
        {
            this.WhenAnyValue(vm => vm.SelectedSvgLayerIndex)
                .CombineLatest(
                    SvgCreator.Layers.Connect().ToCollection(),
                    (idx, operations) =>
                        (idx >= 0 && idx < operations.Count)
                            ? operations.ElementAt(idx)
                            : null)
                .ToPropertyEx(this, vm => vm.SelectedSvgLayer)
                .DisposeWith(_cleanup);

            this.WhenAnyValue(vm => vm.SelectedSvgOperationIndex, vm => vm.SelectedSvgLayer)
                .SelectMany(tuple =>
                {
                    var (opIdx, layer) = tuple;

                    if (layer == null)
                        return Observable.Return<ReactiveSvgOperation?>(null);

                    return layer.Operations
                        .Connect()
                        .ToCollection()
                        .Select(ops =>
                            (opIdx >= 0 && opIdx < ops.Count)
                                ? ops.ToList()[opIdx]
                                : null);
                })
                .ToPropertyEx(this, vm => vm.SelectedSvgOperation)
                .DisposeWith(_cleanup);
        }

        private void BuildLayers()
        {
            var layerEntries = _svgCreator.Layers
                .Connect()
                .Transform(layer => new SvgLayerEntry(layer))
                .Publish();

            layerEntries
                .Bind(out _layersWithSelected)
                .Subscribe()
                .DisposeWith(_cleanup);

            this.WhenAnyValue(vm => vm.SelectedSvgLayerIndex)
                .CombineLatest(layerEntries.ToCollection())
                .Subscribe(tuple =>
                {
                    var (index, layers) = tuple;
                    var list = layers.ToList();

                    for (int i = 0; i < list.Count; i++)
                        list[i].IsSelected = i == index;
                })
                .DisposeWith(_cleanup);

            var operationsForLayer = this.WhenAnyValue(vm => vm.SelectedSvgLayerIndex)
                .Select(idx =>
                {
                    if (idx < 0 || idx >= _layersWithSelected.Count)
                        return Observable.Empty<IChangeSet<SvgOperationEntry>>();

                    return _layersWithSelected[idx]
                        .SvgLayer
                        .Operations
                        .Connect()
                        .Transform(op => new SvgOperationEntry(op));
                })
                .Switch()
                .Publish();

            operationsForLayer
                .Bind(out _operationsWithSelected)
                .Subscribe()
                .DisposeWith(_cleanup);

            this.WhenAnyValue(vm => vm.SelectedSvgOperationIndex)
                .CombineLatest(operationsForLayer.ToCollection())
                .Subscribe(tuple =>
                {
                    var (index, ops) = tuple;
                    var list = ops.ToList();

                    for (int i = 0; i < list.Count; i++)
                        list[i].IsSelected = i == index;
                })
                .DisposeWith(_cleanup);

            operationsForLayer.Connect().DisposeWith(_cleanup);
            layerEntries.Connect().DisposeWith(_cleanup);
        }

        private void OpenSource()
        {
            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Click open source");

            OnOpenSource?.Invoke();
        }

        private void Reset()
        {
            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Click reset");

            OnReset?.Invoke();
        }

        private void Save()
        {
            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Click save");

            OnSave?.Invoke(SvgCreator);
        }

        private void Generate()
        {
            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Click generate");

            OnGenerate?.Invoke(SvgCreator);
        }

        private void EditSvgCreator((string, object, object) payload)
        {
            var (fieldName, newValue, obj) = payload;

            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Edit svg creator {fieldName}={newValue}");

            var type = _svgCreator.GetType();
            var (target, prop) = GetNestedPropertyAndTarget(_svgCreator, fieldName);

            if (target == null)
            {
                Console.WriteLine($"[SvgCreatorEditorViewViewModel] Target not found for property '{fieldName}' on type {type.Name}");
                return;
            }
            if (prop == null)
            {
                Console.WriteLine($"[SvgCreatorEditorViewViewModel] Property '{fieldName}' not found on type {type.Name}");
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

                Console.WriteLine($"[SvgCreatorEditorViewViewModel] Converting value '{newValue}' to type '{prop.PropertyType}' convertedValue='{convertedValue}'");

                Console.WriteLine($"[SvgCreatorEditorViewViewModel] Setting '{fieldName}' = {convertedValue}");

                prop.SetValue(target, convertedValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SvgCreatorEditorViewViewModel] Failed to change type and set svg creator property '{fieldName}': {ex.Message}");
            }
        }

        private void SelectSvgLayer(SvgLayerEntry svgLayerEntry)
        {
            var index = _svgCreator.Layers.Items.ToList().FindIndex(l => l.Equals(svgLayerEntry.SvgLayer));

            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Select layer index={index}");

            if (index == -1)
                throw new Exception($"Failed to select layer entry: not found");

            SelectedSvgOperationIndex = -1;
            SelectedSvgLayerIndex = index == SelectedSvgLayerIndex ? -1 : index;
        }

        private void AddSvgLayer()
        {
            var newLayer = new SvgLayer()
            {
                Name = $"My layer"
            };
            var newReactiveLayer = new ReactiveSvgLayer(newLayer);

            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Add layer={newLayer}");

            _svgCreator.Layers.Edit(list =>
            {
                list.Add(newReactiveLayer);
                int index = list.Count - 1;
                SelectedSvgLayerIndex = index;
                Console.WriteLine($"[SvgCreatorEditorViewViewModel] After add select index={SelectedSvgLayerIndex}");
            });
        }

        private void DeleteSvgLayer(SvgLayerEntry svgLayerEntry)
        {
            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Delete operation={svgLayerEntry.SvgLayer}");

            _svgCreator.Layers.Edit(list =>
            {
                var index = list.IndexOf(svgLayerEntry.SvgLayer);

                list.RemoveAt(index);
            });
        }

        private void DuplicateSvgLayer(SvgLayerEntry svgLayerEntry)
        {
            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Duplicate operation={svgLayerEntry.SvgLayer}");

            _svgCreator.Layers.Edit(list =>
            {
                var index = list.IndexOf(svgLayerEntry.SvgLayer);

                if (index == -1)
                    throw new Exception($"Cannot duplicate operation: not found");

                var newOperation = svgLayerEntry.SvgLayer.Clone();

                list.Add(newOperation);
            });
        }

        private void MoveSvgLayerUp(SvgLayerEntry operation)
        {
            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Move up operation={operation}");

            if (SelectedSvgLayer == null)
                throw new Exception("No selected SVG layer");

            var newIndex = -1;

            _svgCreator.Layers.Edit(list =>
            {
                var index = list.IndexOf(operation.SvgLayer);
                if (index > 0)
                {
                    list.RemoveAt(index);
                    newIndex = index - 1;
                    list.Insert(newIndex, operation.SvgLayer);
                }
            });

            SelectedSvgLayerIndex = newIndex;
        }

        private void MoveSvgLayerDown(SvgLayerEntry operation)
        {
            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Move down operation={operation}");

            var newIndex = -1;

            _svgCreator.Layers.Edit(list =>
            {
                var index = list.IndexOf(operation.SvgLayer);
                if (index >= 0 && index < list.Count - 1)
                {
                    list.RemoveAt(index);
                    newIndex = index + 1;
                    list.Insert(newIndex, operation.SvgLayer);
                }
            });

            SelectedSvgLayerIndex = newIndex;
        }

        private void EditSvgLayer((string, object, object) payload)
        {
            var (fieldName, newValue, layer) = payload;

            if (layer == null || layer is not ReactiveSvgLayer)
                throw new Exception("Need a (reactive) svg layer");

            var layerIndex = SelectedSvgOperationIndex;

            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Edit layer={layer} {fieldName}={newValue}");

            var type = layer.GetType();
            var layerProp = GetNestedProperty(type, fieldName);

            if (layerProp == null)
                throw new Exception($"[SvgCreatorEditorViewViewModel] Property '{fieldName}' not found on {type.Name}");

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

            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Setting prop '{type.Name}.{fieldName}' = '{convertedValue}' ('{newValue}')...");

            Console.WriteLine(layer.GetHashCode());

            SetNestedValue(layer, fieldName, convertedValue);
        }

        private void SelectSvgOperation(SvgOperationEntry operationEntry)
        {
            if (SelectedSvgLayer == null)
                throw new Exception("No selected SVG layer");

            var index = SelectedSvgLayer.Operations.Items.ToList().FindIndex(l => l.Equals(operationEntry.SvgOperation));

            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Select index={index} entry={operationEntry} operation={operationEntry.SvgOperation}");

            if (index == -1)
                throw new Exception($"Failed to select operation entry: not found");

            SelectedSvgOperationIndex = index == SelectedSvgOperationIndex ? -1 : index;
        }

        private void AddSvgOperation(SvgOperationType svgOperationType)
        {
            if (SelectedSvgLayer == null)
                throw new Exception("No selected SVG layer");

            var newOperation = SvgOperationFactory.Create(svgOperationType);
            var newReactiveOperation = ReactiveSvgOperationFactory.Create(newOperation);

            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Add operation={newOperation}");

            SelectedSvgLayer.Operations.Edit(list =>
            {
                list.Add(newReactiveOperation);
                int index = list.Count - 1;
                SelectedSvgOperationIndex = index;
                Console.WriteLine($"[SvgCreatorEditorViewViewModel] After add select index={SelectedSvgOperationIndex}");
            });
        }

        private void DeleteSvgOperation(SvgOperationEntry svgOperationEntry)
        {
            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Delete operation={svgOperationEntry.SvgOperation}");

            if (SelectedSvgLayer == null)
                throw new Exception("No selected SVG layer");

            SelectedSvgLayer.Operations.Edit(list =>
            {
                var index = list.IndexOf(svgOperationEntry.SvgOperation);

                list.RemoveAt(index);
            });
        }

        private void DuplicateSvgOperation(SvgOperationEntry svgOperationEntry)
        {
            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Duplicate operation={svgOperationEntry.SvgOperation}");

            if (SelectedSvgLayer == null)
                throw new Exception("No selected SVG layer");

            SelectedSvgLayer.Operations.Edit(list =>
            {
                var index = list.IndexOf(svgOperationEntry.SvgOperation);

                if (index == -1)
                    throw new Exception($"Cannot duplicate operation: not found");

                var newOperation = svgOperationEntry.SvgOperation.Clone();

                list.Add(newOperation);
            });
        }

        private void MoveSvgOperationUp(SvgOperationEntry operation)
        {
            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Move up operation={operation}");

            if (SelectedSvgLayer == null)
                throw new Exception("No selected SVG layer");

            var newIndex = -1;

            SelectedSvgLayer.Operations.Edit(list =>
            {
                var index = list.IndexOf(operation.SvgOperation);
                if (index > 0)
                {
                    list.RemoveAt(index);
                    newIndex = index - 1;
                    list.Insert(newIndex, operation.SvgOperation);
                }
            });

            SelectedSvgOperationIndex = newIndex;
        }

        private void MoveSvgOperationDown(SvgOperationEntry operation)
        {
            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Move down operation={operation}");

            if (SelectedSvgLayer == null)
                throw new Exception("No selected SVG layer");

            var newIndex = -1;

            SelectedSvgLayer.Operations.Edit(list =>
            {
                var index = list.IndexOf(operation.SvgOperation);
                if (index >= 0 && index < list.Count - 1)
                {
                    list.RemoveAt(index);
                    newIndex = index + 1;
                    list.Insert(newIndex, operation.SvgOperation);
                }
            });

            SelectedSvgOperationIndex = newIndex;
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

            var setter = finalProp.SetMethod;
            if (setter == null)
                throw new Exception($"Property '{finalProp.Name}' has no setter");

            setter.Invoke(current, [value]);
        }

        private void EditSvgOperation((string, object, object) payload)
        {
            if (SelectedSvgLayer == null)
                throw new Exception("No selected SVG layer");

            var (fieldName, newValue, operation) = payload;

            if (operation == null || operation is not ReactiveSvgOperation)
                throw new Exception("Need a (reactive) svg operation");

            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Edit operation={operation} {fieldName}={newValue}");

            var type = operation.GetType();
            var operationProp = GetNestedProperty(type, fieldName);

            if (operationProp == null)
                throw new Exception($"[SvgCreatorEditorViewViewModel] Property '{fieldName}' not found on {type.Name}");

            var targetType = operationProp.PropertyType;

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
            else if (targetType.IsAssignableFrom(newValue.GetType()))
            {
                convertedValue = newValue;
            }
            else
            {
                convertedValue = Convert.ChangeType(newValue, targetType);
            }

            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Setting prop '{type.Name}.{fieldName}' = '{convertedValue}' ('{newValue}')...");

            SetNestedValue(operation, fieldName, convertedValue);
        }

        private void TransformSvgOperation(ReactiveSvgOperation operation, TransformActionType type)
        {
            if (operation == null)
                throw new Exception("Operation is null");

            var snapEnabled = SettingsService.Instance.Snap;
            var snapAmount = Math.Max(SettingsService.Instance.SnapAmount, 1);

            Console.WriteLine($"[SvgCreatorEditorViewViewModel] Transform SVG opration op={operation}");

            switch (type)
            {
                case TransformActionType.Upsize:
                case TransformActionType.Downsize:
                    var width = operation.Width != null ? operation.Width.Resolve(_svgCreator.Width) : _svgCreator.Width;
                    var height = operation.Height != null ? operation.Height.Resolve(_svgCreator.Height) : _svgCreator.Height;
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

                    operation.Width = new FlexibleDimension(width);
                    operation.Height = new FlexibleDimension(height);

                    Console.WriteLine($"[SvgCreatorEditorViewViewModel] Scale operation={operation} {width}x{height} => {newWidth}x{newHeight}");
                    break;

                case TransformActionType.Up:
                case TransformActionType.Down:
                case TransformActionType.Left:
                case TransformActionType.Right:
                    double moveStep = snapEnabled ? snapAmount : 1.0;

                    var pos = operation.Position.Resolve(_svgCreator.Width, _svgCreator.Height);

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

                    var oldPos = operation.Position;

                    operation.Position = new FlexibleVector2()
                    {
                        X = pos.X,
                        Y = pos.Y
                    };

                    Console.WriteLine($"[SvgCreatorEditorViewViewModel] Re-position operation={operation} {oldPos} => {pos}");
                    break;

                case TransformActionType.CW:
                case TransformActionType.CCW:
                    var oldRotate = operation.Rotate;
                    var newRotate = oldRotate ?? 0;
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
                        newRotate = Math.Round((double)(newRotate / snapAmount)) * snapAmount;
                    }

                    operation.Rotate = newRotate;

                    Console.WriteLine($"[SvgCreatorEditorViewViewModel] Rotate operation={operation} {oldRotate} => {newRotate}");
                    break;
            }
        }
    }
}

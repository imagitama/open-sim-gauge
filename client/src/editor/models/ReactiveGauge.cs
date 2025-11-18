using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia.Controls;
using DynamicData;
using ReactiveUI;

namespace OpenGaugeClient
{
    public class ReactiveGauge : ReactiveObject
    {
        private readonly CompositeDisposable _cleanup = new();

        public ReactiveGauge(Gauge gauge)
        {
            _gauge = gauge ?? throw new ArgumentNullException(nameof(gauge));

            Name = gauge.Name;
            Width = gauge.Width;
            Height = gauge.Height;
            Origin = gauge.Origin;
            Clip = gauge.Clip != null ? new ReactiveClipConfig(gauge.Clip) : null;
            OldClip = Clip;
            Grid = gauge.Grid;
            Source = gauge.Source;

            Layers.Edit(inner =>
            {
                inner.Clear();
                inner.AddRange(gauge.Layers.Select(l => new ReactiveLayer(l)));
            });

            Changed.Subscribe(change =>
            {
                var prop = change.Sender?.GetType().GetProperty(change.PropertyName ?? "");
                var newValue = prop?.GetValue(change.Sender);

                Console.WriteLine($"[ReactiveGauge] Changed {change.PropertyName} = {newValue}");
            }).DisposeWith(_cleanup);

            this.WhenAnyValue(x => x.Name)
                // .StartWith(gauge.Name)
                .Select(name =>
                {
                    if (!string.IsNullOrEmpty(name))
                        return name.Replace("_", "__");
                    return "(unnamed)";
                })
                .ToProperty(this, x => x.Label, out _label, initialValue: "(unnamed)")
                .DisposeWith(_cleanup);
        }

        public void Dispose()
        {
            _cleanup.Dispose();
            foreach (var layer in Layers.Items)
            {
                layer.Dispose();
            }
        }

        private readonly Gauge _gauge;

        private string _name;
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        private int _width;
        public int Width
        {
            get => _width;
            set => this.RaiseAndSetIfChanged(ref _width, value);
        }

        private int _height;
        public int Height
        {
            get => _height;
            set => this.RaiseAndSetIfChanged(ref _height, value);
        }

        private FlexibleVector2 _origin;
        public FlexibleVector2 Origin
        {
            get => _origin;
            set => this.RaiseAndSetIfChanged(ref _origin, value);
        }

        private ReactiveClipConfig? _clip;
        public ReactiveClipConfig? Clip
        {
            get => _clip;
            set
            {
                if (value != null)
                    OldClip = value;
                this.RaiseAndSetIfChanged(ref _clip, value);
            }
        }

        private SourceList<ReactiveLayer> _layers = new();
        public SourceList<ReactiveLayer> Layers
        {
            get => _layers;
            set => this.RaiseAndSetIfChanged(ref _layers, value);
        }

        private string? _source;
        public string? Source
        {
            get => _source;
            set => this.RaiseAndSetIfChanged(ref _source, value);
        }


        private double? _grid;
        public double? Grid
        {
            get => _height;
            set => this.RaiseAndSetIfChanged(ref _grid, value);
        }

        public void Replace(Gauge newGauge)
        {
            if (newGauge == null)
                throw new ArgumentNullException(nameof(newGauge));

            Console.WriteLine($"[ReactiveGauge.Replace] newGauge={newGauge}");

            // Copy simple values
            Name = newGauge.Name;
            Width = newGauge.Width;
            Height = newGauge.Height;
            Origin = newGauge.Origin;
            Clip = newGauge.Clip != null ? new ReactiveClipConfig(newGauge.Clip) : null;
            OldClip = Clip;

            Layers.Edit(inner =>
            {
                inner.Clear();
                inner.AddRange(newGauge.Layers.Select(l => new ReactiveLayer(l)));
            });

            Console.WriteLine($"[ReactiveGauge] Replaced with Gauge '{newGauge.Name}'");
        }

        private readonly ObservableAsPropertyHelper<string> _label;
        public string Label => _label?.Value ?? string.Empty;

        public Gauge ToModel()
        {
            return new Gauge
            {
                Name = Name,
                Width = Width,
                Height = Height,
                Origin = Origin,
                Clip = Clip?.ToModel(),
                Layers = Layers.Items.Select(x => x.ToModel()).ToList(),
                Source = _gauge.Source
            };
        }

        public override string ToString()
        {
            return $"ReactiveGauge {{ " +
                   $"Name={Name ?? "null"}, " +
                   $"Width={Width.ToString() ?? "null"}, " +
                   $"Height={Height.ToString() ?? "null"}, " +
                   $"Origin={Origin}, " +
                   $"Clip={Clip}, " +
                   $"Layers=[{string.Join(", ", Layers.Items)}] " +
                   $"Source='{Source}', " +
                "}}";
        }

        private ReactiveClipConfig? OldClip;

        public bool HasClip
        {
            get => Clip != null;
            set
            {
                if (value && Clip == null)
                {
                    if (OldClip != null)
                    {
                        Console.WriteLine("[ReactiveGauge] HasClip - re-use old clip");
                        Clip = OldClip;
                    }
                    else
                    {
                        Console.WriteLine("[ReactiveGauge] HasClip - create empty");
                        Clip = new ReactiveClipConfig();
                    }
                }
                else if (!value)
                {
                    Console.WriteLine("[ReactiveGauge] HasClip - to null");

                    if (Clip != null)
                        OldClip = Clip;

                    Clip = null;
                }

                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(Clip));
            }
        }
    }
}

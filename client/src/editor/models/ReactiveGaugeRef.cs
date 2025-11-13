using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveUI;

namespace OpenGaugeClient
{
    public class ReactiveGaugeRef : ReactiveObject
    {
        private readonly CompositeDisposable _cleanup = new();

        public ReactiveGaugeRef() { }

        public ReactiveGaugeRef(GaugeRef gauge)
        {
            Gauge = gauge.Gauge;
            Name = gauge.Name;
            Path = gauge.Path;
            Position = gauge.Position;
            Scale = gauge.Scale;
            Width = gauge.Width;
            Skip = gauge.Skip;
            Gauge = gauge.Gauge;

            this.WhenAnyValue(x => x.Name, x => x.Path)
                .Select(tuple =>
                {
                    var (name, path) = tuple;

                    if (!string.IsNullOrEmpty(name))
                        return name!;
                    if (!string.IsNullOrEmpty(path))
                        return $"File: {System.IO.Path.GetFileName(path)}".Replace("_", "__");
                    return "(unnamed)";
                })
                .ToProperty(this, x => x.Label, out _label, initialValue: "(unnamed)")
                .DisposeWith(_cleanup);
        }

        public void Dispose() => _cleanup.Dispose();

        public Gauge Gauge;

        private string? _name;
        public string? Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        private string? _path;
        public string? Path
        {
            get => _path;
            set => this.RaiseAndSetIfChanged(ref _path, value);
        }

        private FlexibleVector2 _position = new();
        public FlexibleVector2 Position
        {
            get => _position;
            set => this.RaiseAndSetIfChanged(ref _position, value);
        }

        private double _scale = 1.0;
        public double Scale
        {
            get => _scale;
            set => this.RaiseAndSetIfChanged(ref _scale, value);
        }

        private double? _width;
        public double? Width
        {
            get => _width;
            set => this.RaiseAndSetIfChanged(ref _width, value);
        }

        private bool _skip = false;
        public bool Skip
        {
            get => _skip;
            set => this.RaiseAndSetIfChanged(ref _skip, value);
        }

        private readonly ObservableAsPropertyHelper<string> _label;
        public string Label => _label?.Value ?? string.Empty;

        public GaugeRef ToGaugeRef()
        {
            return new GaugeRef
            {
                Name = Name,
                Path = Path,
                Position = Position,
                Scale = Scale,
                Width = Width,
                Skip = Skip,
                Gauge = Gauge
            };
        }

        public override string ToString()
            => $"ReactiveGaugeRef(Name={Name}, Path={Path}, Scale={Scale}, Skip={Skip})";
    }
}

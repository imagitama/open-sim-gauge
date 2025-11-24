using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using DynamicData;
using OpenGaugeClient.Editor;
using ReactiveUI;

namespace OpenGaugeClient
{
    public class ReactiveSvgCreator : ReactiveObject
    {
        private readonly CompositeDisposable _cleanup = new();

        public ReactiveSvgCreator(SvgCreator svgCreator)
        {
            _svgCreator = svgCreator ?? throw new ArgumentNullException(nameof(svgCreator));

            Width = svgCreator.Width;
            Height = svgCreator.Height;
            Source = svgCreator.Source;

            Layers.Edit(inner =>
            {
                inner.Clear();
                inner.AddRange(svgCreator.Layers.Select(x => new ReactiveSvgLayer(x)));
            });

            Changed.Subscribe(change =>
            {
                var prop = change.Sender?.GetType().GetProperty(change.PropertyName ?? "");
                var newValue = prop?.GetValue(change.Sender);

                Console.WriteLine($"[ReactiveSvgCreator] Changed {change.PropertyName} = {newValue}");
            }).DisposeWith(_cleanup);
        }

        public void Dispose()
        {
            _cleanup.Dispose();
            foreach (var layer in Layers.Items)
            {
                layer.Dispose();
            }
        }

        private readonly SvgCreator _svgCreator;

        private double _width;
        public double Width
        {
            get => _width;
            set => this.RaiseAndSetIfChanged(ref _width, value);
        }

        private double _height;
        public double Height
        {
            get => _height;
            set => this.RaiseAndSetIfChanged(ref _height, value);
        }

        private SourceList<ReactiveSvgLayer> _layers = new();
        public SourceList<ReactiveSvgLayer> Layers
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

        public void Replace(SvgCreator newSvgCreator)
        {
            if (newSvgCreator == null)
                throw new ArgumentNullException(nameof(newSvgCreator));

            Console.WriteLine($"[ReactiveSvgCreator.Replace] newSvgCreator={newSvgCreator}");

            Width = newSvgCreator.Width;
            Height = newSvgCreator.Height;

            Layers.Edit(inner =>
            {
                inner.Clear();
                inner.AddRange(newSvgCreator.Layers.Select(x => new ReactiveSvgLayer(x)));
            });

            Console.WriteLine($"[ReactiveSvgCreator] Replaced with svgCreator={newSvgCreator}");
        }

        private readonly ObservableAsPropertyHelper<string> _label;
        public string Label => _label?.Value ?? string.Empty;

        public SvgCreator ToModel()
        {
            return new SvgCreator
            {
                Width = Width,
                Height = Height,
                Layers = Layers.Items.Select(x => x.ToModel()).ToList()
            };
        }

        public override string ToString()
        {
            return $"ReactiveSvgCreator {{ " +
                   $"Width={Width.ToString() ?? "null"}, " +
                   $"Height={Height.ToString() ?? "null"}, " +
                   $"Layers=[{string.Join(", ", Layers.Items)}] " +
                   $"Source='{Source}'" +
                "}}";
        }
    }
}

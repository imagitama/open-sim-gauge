using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using DynamicData;
using OpenGaugeClient.Editor;
using ReactiveUI;

namespace OpenGaugeClient
{
    public class ReactiveSvgLayer : ReactiveObject
    {
        private readonly SvgLayer _svgLayer;
        private readonly CompositeDisposable _cleanup = new();

        public ReactiveSvgLayer(SvgLayer svgLayer)
        {
            _svgLayer = svgLayer ?? throw new ArgumentNullException(nameof(svgLayer));

            Console.WriteLine(svgLayer.GetHashCode());

            Name = svgLayer.Name;
            Shadow = svgLayer.Shadow;
            Operations.Edit(inner =>
            {
                inner.Clear();
                inner.AddRange(svgLayer.Operations.Select(ReactiveSvgOperationFactory.Create));
            });

            Changed.Subscribe(change =>
            {
                var prop = change.Sender?.GetType().GetProperty(change.PropertyName ?? "");
                var newValue = prop?.GetValue(change.Sender);

                Console.WriteLine($"[ReactiveSvgLayer] Changed {change.PropertyName} = {newValue}");
            }).DisposeWith(_cleanup);

            this.WhenAnyValue(x => x.Name)
                .Select(name =>
                {
                    return name.Replace("_", "__");
                })
                .ToProperty(this, x => x.Label, out _label, initialValue: "(unnamed)")
                .DisposeWith(_cleanup);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }
        private ShadowUnion? _shadow;
        public ShadowUnion? Shadow
        {
            get => _shadow;
            set => this.RaiseAndSetIfChanged(ref _shadow, value);
        }
        private SourceList<ReactiveSvgOperation> _svgOperations = new();
        public SourceList<ReactiveSvgOperation> Operations
        {
            get => _svgOperations;
            set => this.RaiseAndSetIfChanged(ref _svgOperations, value);
        }

        public bool HasShadow
        {
            get => Shadow != null;
            set
            {
                if (value)
                    Shadow = new ShadowUnion()
                    {
                        IsTrue = true
                    };
                else
                    Shadow = null;

                this.RaisePropertyChanged();
            }
        }

        public double? _forceRotate;
        public double? ForceRotate
        {
            get => _forceRotate;
            set => this.RaiseAndSetIfChanged(ref _forceRotate, value);
        }

        public void Dispose()
        {
            _cleanup.Dispose();
            foreach (var layer in Operations.Items)
            {
                layer.Dispose();
            }
        }

        public void Replace(SvgLayer newSvgLayer)
        {
            if (newSvgLayer == null)
                throw new ArgumentNullException(nameof(newSvgLayer));

            Console.WriteLine($"[ReactiveSvgLayer.Replace] newSvgLayer={newSvgLayer}");

            Name = newSvgLayer.Name;
            Shadow = newSvgLayer.Shadow;
            Operations.Edit(inner =>
            {
                inner.Clear();
                inner.AddRange(newSvgLayer.Operations.Select(ReactiveSvgOperationFactory.Create));
            });

            Console.WriteLine($"[ReactiveSvgLayer] Replaced with SvgCreator '{newSvgLayer.Name}'");
        }

        private readonly ObservableAsPropertyHelper<string> _label;
        public string Label => _label?.Value ?? string.Empty;

        public SvgLayer ToModel()
        {
            return new SvgLayer
            {
                Name = Name,
                Shadow = Shadow,
                Operations = Operations.Items.Select(x => x.ToModel()).ToList()
            };
        }

        public ReactiveSvgLayer Clone()
        {
            return new ReactiveSvgLayer(ToModel());
        }

        public override string ToString()
        {
            return $"ReactiveSvgLayer {{ " +
                   $"Name={Name ?? "null"}, " +
                   $"Shadow={Shadow}, " +
                   $"Operations=[{string.Join(", ", Operations.Items)}] " +
                "}}";
        }
    }
}

using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using OpenGaugeClient.Editor;
using ReactiveUI;

namespace OpenGaugeClient
{
    public abstract class ReactiveSvgOperation : ReactiveObject, IDisposable
    {
        private readonly CompositeDisposable _cleanup = new();

        protected ReactiveSvgOperation(SvgOperation model)
        {
            Name = model.Name;
            Type = model.Type;
            Width = model.Width;
            Height = model.Height;
            Position = model.Position;
            Origin = model.Origin;
            Rotate = model.Rotate;
            Skip = model.Skip;

            Changed.Subscribe(change =>
            {
                var prop = change.Sender?.GetType().GetProperty(change.PropertyName ?? "");
                var newValue = prop?.GetValue(change.Sender);

                Console.WriteLine($"[ReactiveSvgOperation] Changed {change.PropertyName} = {newValue}");
            }).DisposeWith(_cleanup);

            this.WhenAnyValue(x => x.Name, x => x.Type)
                 .Select(tuple =>
                 {
                     var (name, type) = tuple;

                     if (!string.IsNullOrEmpty(name))
                         return name!;
                     return type.ToString();
                 })
                 .ToProperty(this, x => x.Label, out _label, initialValue: "(unnamed)")
                 .DisposeWith(_cleanup);
        }

        public void Dispose() => _cleanup.Dispose();

        private string? _name;
        public string? Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        private SvgOperationType _type;
        public SvgOperationType Type
        {
            get => _type;
            set => this.RaiseAndSetIfChanged(ref _type, value);
        }

        private FlexibleDimension? _width;
        public FlexibleDimension? Width
        {
            get => _width;
            set => this.RaiseAndSetIfChanged(ref _width, value);
        }

        private FlexibleDimension? _height;
        public FlexibleDimension? Height
        {
            get => _height;
            set => this.RaiseAndSetIfChanged(ref _height, value);
        }

        private FlexibleVector2 _position;
        public FlexibleVector2 Position
        {
            get => _position;
            set => this.RaiseAndSetIfChanged(ref _position, value);
        }

        private FlexibleVector2 _origin;
        public FlexibleVector2 Origin
        {
            get => _origin;
            set => this.RaiseAndSetIfChanged(ref _origin, value);
        }

        private double? _rotate;
        public double? Rotate
        {
            get => _rotate;
            set => this.RaiseAndSetIfChanged(ref _rotate, value);
        }

        private bool _debug;
        public bool Debug
        {
            get => _debug;
            set => this.RaiseAndSetIfChanged(ref _debug, value);
        }

        private bool _skip;
        public bool Skip
        {
            get => _skip;
            set => this.RaiseAndSetIfChanged(ref _skip, value);
        }

        private readonly ObservableAsPropertyHelper<string> _label;
        public string Label => _label?.Value ?? string.Empty;

        public virtual void Replace(ReactiveSvgOperation other)
        {
            Name = other.Name;
            Type = other.Type;
            Width = other.Width;
            Height = other.Height;
            Position = other.Position;
            Origin = other.Origin;
            Rotate = other.Rotate;
            Debug = other.Debug;
            Skip = other.Skip;
        }

        protected SvgOperation FillBaseProps(SvgOperation model)
        {
            model.Name = Name;
            model.Width = Width;
            model.Height = Height;
            model.Position = Position;
            model.Origin = Origin;
            model.Rotate = Rotate;
            model.Skip = Skip;
            return model;
        }

        public abstract SvgOperation ToModel();
        public ReactiveSvgOperation Clone()
        {
            var newModel = ToModel();
            return ReactiveSvgOperationFactory.Create(newModel);
        }
        public override string ToString()
        {
            return $"ReactiveSvgOperation(" +
                   $"Type={Type}," +
                   $"Name={Name}, " +
                   $"Width={Width}, " +
                   $"Height={Height}, " +
                   $"Position={Position}, " +
                   $"Origin={Origin}, " +
                   $"Rotate={Rotate}, " +
                   $"Skip={Skip}" +
            ")";
        }
    }

    public class ReactiveCircleSvgOperation : ReactiveSvgOperation
    {
        public ReactiveCircleSvgOperation(CircleSvgOperation model)
            : base(model)
        {
            Fill = model.Fill;
            StrokeFill = model.StrokeFill;
            StrokeWidth = model.StrokeWidth;
            Radius = model.Radius;
        }

        private string _fill;
        public string Fill
        {
            get => _fill;
            set => this.RaiseAndSetIfChanged(ref _fill, value);
        }

        private string? _strokeFill;
        public string? StrokeFill
        {
            get => _strokeFill;
            set => this.RaiseAndSetIfChanged(ref _strokeFill, value);
        }

        private double? _strokeWidth;
        public double? StrokeWidth
        {
            get => _strokeWidth;
            set => this.RaiseAndSetIfChanged(ref _strokeWidth, value);
        }

        private double _radius;
        public double Radius
        {
            get => _radius;
            set => this.RaiseAndSetIfChanged(ref _radius, value);
        }

        public override void Replace(ReactiveSvgOperation other)
        {
            base.Replace(other);
            var o = (ReactiveCircleSvgOperation)other;
            Fill = o.Fill;
            StrokeFill = o.StrokeFill;
            StrokeWidth = o.StrokeWidth;
            Radius = o.Radius;
        }

        public override SvgOperation ToModel() =>
            FillBaseProps(new CircleSvgOperation()
            {
                Fill = Fill,
                StrokeFill = StrokeFill,
                StrokeWidth = StrokeWidth,
                Radius = Radius
            });
    }

    public class ReactiveSquareSvgOperation : ReactiveSvgOperation
    {
        public ReactiveSquareSvgOperation(SquareSvgOperation model)
            : base(model)
        {
            Fill = model.Fill;
            StrokeFill = model.StrokeFill;
            StrokeWidth = model.StrokeWidth;
            Round = model.Round;
        }

        private string _fill;
        public string Fill
        {
            get => _fill;
            set => this.RaiseAndSetIfChanged(ref _fill, value);
        }

        private string? _strokeFill;
        public string? StrokeFill
        {
            get => _strokeFill;
            set => this.RaiseAndSetIfChanged(ref _strokeFill, value);
        }

        private double? _strokeWidth;
        public double? StrokeWidth
        {
            get => _strokeWidth;
            set => this.RaiseAndSetIfChanged(ref _strokeWidth, value);
        }

        private double? _round;
        public double? Round
        {
            get => _round;
            set => this.RaiseAndSetIfChanged(ref _round, value);
        }

        public override void Replace(ReactiveSvgOperation other)
        {
            base.Replace(other);
            var o = (ReactiveSquareSvgOperation)other;
            Fill = o.Fill;
            StrokeFill = o.StrokeFill;
            StrokeWidth = o.StrokeWidth;
            Round = o.Round;
        }

        public override SvgOperation ToModel() =>
            FillBaseProps(new SquareSvgOperation()
            {
                Fill = Fill,
                StrokeFill = StrokeFill,
                StrokeWidth = StrokeWidth,
                Round = Round
            });
    }

    public class ReactiveTriangleSvgOperation : ReactiveSvgOperation
    {
        public ReactiveTriangleSvgOperation(TriangleSvgOperation model)
            : base(model)
        {
            Fill = model.Fill;
            StrokeFill = model.StrokeFill;
            StrokeWidth = model.StrokeWidth;
            Round = model.Round;
        }

        private string _fill;
        public string Fill
        {
            get => _fill;
            set => this.RaiseAndSetIfChanged(ref _fill, value);
        }

        private string? _strokeFill;
        public string? StrokeFill
        {
            get => _strokeFill;
            set => this.RaiseAndSetIfChanged(ref _strokeFill, value);
        }

        private double? _strokeWidth;
        public double? StrokeWidth
        {
            get => _strokeWidth;
            set => this.RaiseAndSetIfChanged(ref _strokeWidth, value);
        }

        private double? _round;
        public double? Round
        {
            get => _round;
            set => this.RaiseAndSetIfChanged(ref _round, value);
        }

        public override void Replace(ReactiveSvgOperation other)
        {
            base.Replace(other);
            var o = (ReactiveTriangleSvgOperation)other;
            Fill = o.Fill;
            StrokeFill = o.StrokeFill;
            StrokeWidth = o.StrokeWidth;
            Round = o.Round;
        }

        public override SvgOperation ToModel() =>
            FillBaseProps(new TriangleSvgOperation()
            {
                Fill = Fill,
                StrokeFill = StrokeFill,
                StrokeWidth = StrokeWidth,
                Round = Round
            });
    }

    public class ReactiveArcSvgOperation : ReactiveSvgOperation
    {
        public ReactiveArcSvgOperation(ArcSvgOperation model)
            : base(model)
        {
            Radius = model.Radius;
            DegreesStart = model.DegreesStart;
            DegreesEnd = model.DegreesEnd;
            InnerThickness = model.InnerThickness;
            Fill = model.Fill;
        }

        private double _radius;
        public double Radius
        {
            get => _radius;
            set => this.RaiseAndSetIfChanged(ref _radius, value);
        }

        private double _degStart;
        public double DegreesStart
        {
            get => _degStart;
            set => this.RaiseAndSetIfChanged(ref _degStart, value);
        }

        private double _degEnd;
        public double DegreesEnd
        {
            get => _degEnd;
            set => this.RaiseAndSetIfChanged(ref _degEnd, value);
        }

        private double _innerThickness;
        public double InnerThickness
        {
            get => _innerThickness;
            set => this.RaiseAndSetIfChanged(ref _innerThickness, value);
        }

        private string _fill;
        public string Fill
        {
            get => _fill;
            set => this.RaiseAndSetIfChanged(ref _fill, value);
        }

        public override void Replace(ReactiveSvgOperation other)
        {
            base.Replace(other);
            var o = (ReactiveArcSvgOperation)other;
            Radius = o.Radius;
            DegreesStart = o.DegreesStart;
            DegreesEnd = o.DegreesEnd;
            InnerThickness = o.InnerThickness;
            Fill = o.Fill;
        }

        public override SvgOperation ToModel() =>
            FillBaseProps(new ArcSvgOperation()
            {
                Radius = Radius,
                DegreesStart = DegreesStart,
                DegreesEnd = DegreesEnd,
                InnerThickness = InnerThickness,
                Fill = Fill
            });
    }

    public class ReactiveGaugeTicksSvgOperation : ReactiveSvgOperation
    {
        public ReactiveGaugeTicksSvgOperation(GaugeTicksSvgOperation model)
            : base(model)
        {
            Radius = model.Radius;
            DegreesStart = model.DegreesStart;
            DegreesEnd = model.DegreesEnd;
            DegreesGap = model.DegreesGap;
            TickLength = model.TickLength;
            TickWidth = model.TickWidth;
            TickFill = model.TickFill;
        }

        private double _radius;
        public double Radius
        {
            get => _radius;
            set => this.RaiseAndSetIfChanged(ref _radius, value);
        }

        private double _degStart;
        public double DegreesStart
        {
            get => _degStart;
            set => this.RaiseAndSetIfChanged(ref _degStart, value);
        }

        private double _degEnd;
        public double DegreesEnd
        {
            get => _degEnd;
            set => this.RaiseAndSetIfChanged(ref _degEnd, value);
        }

        private double _gap;
        public double DegreesGap
        {
            get => _gap;
            set => this.RaiseAndSetIfChanged(ref _gap, value);
        }

        private double _tickLength;
        public double TickLength
        {
            get => _tickLength;
            set => this.RaiseAndSetIfChanged(ref _tickLength, value);
        }

        private double _tickWidth;
        public double TickWidth
        {
            get => _tickWidth;
            set => this.RaiseAndSetIfChanged(ref _tickWidth, value);
        }

        private string _tickFill;
        public string TickFill
        {
            get => _tickFill;
            set => this.RaiseAndSetIfChanged(ref _tickFill, value);
        }

        public override void Replace(ReactiveSvgOperation other)
        {
            base.Replace(other);
            var o = (ReactiveGaugeTicksSvgOperation)other;
            Radius = o.Radius;
            DegreesStart = o.DegreesStart;
            DegreesEnd = o.DegreesEnd;
            DegreesGap = o.DegreesGap;
            TickLength = o.TickLength;
            TickWidth = o.TickWidth;
            TickFill = o.TickFill;
        }

        public override SvgOperation ToModel() =>
            FillBaseProps(new GaugeTicksSvgOperation()
            {
                Radius = Radius,
                DegreesStart = DegreesStart,
                DegreesEnd = DegreesEnd,
                DegreesGap = DegreesGap,
                TickLength = TickLength,
                TickWidth = TickWidth,
                TickFill = TickFill
            });
    }

    public class ReactiveGaugeTickLabelsSvgOperation : ReactiveSvgOperation
    {
        public ReactiveGaugeTickLabelsSvgOperation(GaugeTickLabelsSvgOperation model)
            : base(model)
        {
            Radius = model.Radius;
            DegreesStart = model.DegreesStart;
            DegreesEnd = model.DegreesEnd;
            DegreesGap = model.DegreesGap;
            Labels = model.Labels;
            LabelFill = model.LabelFill;
            LabelSize = model.LabelSize;
            LabelFont = model.LabelFont;
        }

        private double _radius;
        public double Radius
        {
            get => _radius;
            set => this.RaiseAndSetIfChanged(ref _radius, value);
        }

        private double _degStart;
        public double DegreesStart
        {
            get => _degStart;
            set => this.RaiseAndSetIfChanged(ref _degStart, value);
        }

        private double _degEnd;
        public double DegreesEnd
        {
            get => _degEnd;
            set => this.RaiseAndSetIfChanged(ref _degEnd, value);
        }

        private double _gap;
        public double DegreesGap
        {
            get => _gap;
            set => this.RaiseAndSetIfChanged(ref _gap, value);
        }

        private IList<string> _labels;
        public IList<string> Labels
        {
            get => _labels;
            set => this.RaiseAndSetIfChanged(ref _labels, value);
        }

        private string _labelFill;
        public string LabelFill
        {
            get => _labelFill;
            set => this.RaiseAndSetIfChanged(ref _labelFill, value);
        }

        private double _labelSize;
        public double LabelSize
        {
            get => _labelSize;
            set => this.RaiseAndSetIfChanged(ref _labelSize, value);
        }

        private string _labelFont;
        public string LabelFont
        {
            get => _labelFont;
            set => this.RaiseAndSetIfChanged(ref _labelFont, value);
        }

        public override void Replace(ReactiveSvgOperation other)
        {
            base.Replace(other);
            var o = (ReactiveGaugeTickLabelsSvgOperation)other;
            Radius = o.Radius;
            DegreesStart = o.DegreesStart;
            DegreesEnd = o.DegreesEnd;
            DegreesGap = o.DegreesGap;
            Labels = o.Labels;
            LabelFill = o.LabelFill;
            LabelSize = o.LabelSize;
            LabelFont = o.LabelFont;
        }

        public override SvgOperation ToModel() =>
            FillBaseProps(new GaugeTickLabelsSvgOperation()
            {
                Radius = Radius,
                DegreesStart = DegreesStart,
                DegreesEnd = DegreesEnd,
                DegreesGap = DegreesGap,
                Labels = Labels,
                LabelFill = LabelFill,
                LabelSize = LabelSize,
                LabelFont = LabelFont
            });
    }

    public class ReactiveTextSvgOperation : ReactiveSvgOperation
    {
        public ReactiveTextSvgOperation(TextSvgOperation model)
            : base(model)
        {
            Text = model.Text;
            Size = model.Size;
            Font = model.Font;
            Fill = model.Fill;
        }

        private string _text;
        public string Text
        {
            get => _text;
            set => this.RaiseAndSetIfChanged(ref _text, value);
        }

        private double _size;
        public double Size
        {
            get => _size;
            set => this.RaiseAndSetIfChanged(ref _size, value);
        }

        private string _font;
        public string Font
        {
            get => _font;
            set => this.RaiseAndSetIfChanged(ref _font, value);
        }

        private string _fill;
        public string Fill
        {
            get => _fill;
            set => this.RaiseAndSetIfChanged(ref _fill, value);
        }

        public override void Replace(ReactiveSvgOperation other)
        {
            base.Replace(other);
            var o = (ReactiveTextSvgOperation)other;
            Text = o.Text;
            Size = o.Size;
            Font = o.Font;
            Fill = o.Fill;
        }

        public override SvgOperation ToModel() =>
            FillBaseProps(new TextSvgOperation()
            {
                Text = Text,
                Size = Size,
                Font = Font,
                Fill = Fill
            });
    }

    public static class ReactiveSvgOperationFactory
    {
        public static ReactiveSvgOperation Create(SvgOperation op)
        {
            return op switch
            {
                CircleSvgOperation c => new ReactiveCircleSvgOperation(c),
                SquareSvgOperation s => new ReactiveSquareSvgOperation(s),
                TriangleSvgOperation t => new ReactiveTriangleSvgOperation(t),
                ArcSvgOperation arc => new ReactiveArcSvgOperation(arc),
                GaugeTicksSvgOperation ticks => new ReactiveGaugeTicksSvgOperation(ticks),
                GaugeTickLabelsSvgOperation lbl => new ReactiveGaugeTickLabelsSvgOperation(lbl),
                TextSvgOperation txt => new ReactiveTextSvgOperation(txt),

                // If something unexpected appears
                _ => throw new NotSupportedException(
                    $"Unsupported SvgOperation type: {op.GetType().Name}"
                )
            };
        }
    }
}

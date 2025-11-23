using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveUI;

namespace OpenGaugeClient
{
    public class ReactiveLayer : ReactiveObject
    {
        private readonly CompositeDisposable _cleanup = new();

        public ReactiveLayer(Layer layer)
        {
            Name = layer.Name;
            Text = layer.Text != null ? new ReactiveTextDef(layer.Text) : null;
            Image = layer.Image;
            Width = layer.Width;
            Height = layer.Height;
            Origin = layer.Origin;
            Position = layer.Position;
            Transform = layer.Transform != null ? new ReactiveTransformDef(layer.Transform) : null;
            Rotate = layer.Rotate;
            TranslateX = layer.TranslateX;
            TranslateY = layer.TranslateY;
            Fill = layer.Fill;
            Debug = layer.Debug;
            Skip = layer.Skip;

            Changed
                .Where(change =>
                {
                    if (change.PropertyName is null)
                        return false;

                    var prop = change.Sender?.GetType().GetProperty(change.PropertyName);
                    if (prop == null)
                        return false;

                    return prop.SetMethod != null;
                })
                .Subscribe(change =>
                {
                    var prop = change.Sender!.GetType().GetProperty(change.PropertyName!);
                    var newValue = prop?.GetValue(change.Sender);

                    Console.WriteLine($"[ReactiveLayer] Changed {change.PropertyName} = {newValue}");
                })
                .DisposeWith(_cleanup);


            this.WhenAnyValue(x => x.Name, x => x.Image, x => x.Text)
                     .Select(tuple =>
                     {
                         var (name, image, text) = tuple;

                         if (!string.IsNullOrEmpty(name))
                             return name!;
                         if (!string.IsNullOrEmpty(image))
                             return $"{Path.GetFileName(image)}".Replace("_", "__");
                         if (text != null)
                             return $"Text Layer";
                         return "(unnamed)";
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

        private ReactiveTextDef? _text;
        public ReactiveTextDef? Text
        {
            get => _text;
            set => this.RaiseAndSetIfChanged(ref _text, value);
        }

        private string? _image;
        public string? Image
        {
            get => _image;
            set => this.RaiseAndSetIfChanged(ref _image, value);
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

        private FlexibleVector2 _origin;
        public FlexibleVector2 Origin
        {
            get => _origin;
            set => this.RaiseAndSetIfChanged(ref _origin, value);
        }

        private FlexibleVector2 _position;
        public FlexibleVector2 Position
        {
            get => _position;
            set => this.RaiseAndSetIfChanged(ref _position, value);
        }

        private ReactiveTransformDef? _transform;
        public ReactiveTransformDef? Transform
        {
            get => _transform;
            set => this.RaiseAndSetIfChanged(ref _transform, value);
        }

        private double _rotate;
        public double Rotate
        {
            get => _rotate;
            set => this.RaiseAndSetIfChanged(ref _rotate, value);
        }

        private double _translateX;
        public double TranslateX
        {
            get => _translateX;
            set => this.RaiseAndSetIfChanged(ref _translateX, value);
        }

        private double _translateY;
        public double TranslateY
        {
            get => _translateY;
            set => this.RaiseAndSetIfChanged(ref _translateY, value);
        }

        private ColorDef? _fill;
        public ColorDef? Fill
        {
            get => _fill;
            set => this.RaiseAndSetIfChanged(ref _fill, value);
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

        public ReactiveLayer Clone()
        {
            return new ReactiveLayer(new Layer()
            {
                Name = Name,
                Text = Text?.ToModel(),
                Image = Image,
                Width = Width,
                Height = Height,
                Origin = Origin,
                Position = Position,
                Transform = Transform?.ToModel(),
                Rotate = Rotate,
                TranslateX = TranslateX,
                TranslateY = TranslateY,
                Fill = Fill,
                Debug = Debug,
                Skip = Skip
            });
        }

        public Layer ToModel()
        {
            return new Layer()
            {
                Name = Name,
                Text = Text?.ToModel(),
                Image = Image,
                Width = Width,
                Height = Height,
                Origin = Origin,
                Position = Position,
                Transform = Transform?.ToModel(),
                Rotate = Rotate,
                TranslateX = TranslateX,
                TranslateY = TranslateY,
                Fill = Fill,
                Debug = Debug,
                Skip = Skip
            };
        }

        public override string ToString()
        {
            return $"ReactiveLayer {{ " +
                   $"Name = '{Name}', " +
                   $"Image = '{Image ?? "null"}', " +
                   $"Width = {Width?.ToString() ?? "null"}, " +
                   $"Height = {Height?.ToString() ?? "null"}, " +
                   $"Origin = {Origin}, " +
                   $"Position = {Position}, " +
                   $"Transform = {Transform} " +
            " }}";
        }

        private ReactiveTextDef? OldText;

        public bool HasText
        {
            get => Text != null;
            set
            {
                if (value && Text == null)
                {
                    if (OldText != null)
                    {
                        Text = OldText;
                    }
                    else
                    {
                        Text = new ReactiveTextDef();
                    }
                }
                else if (!value)
                {
                    if (Text != null)
                        OldText = Text;

                    Text = null;
                }

                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(Text));
            }
        }

        private ReactiveTransformDef? OldTransform;

        public bool HasTransform
        {
            get => Transform != null;
            set
            {
                if (value && Transform == null)
                {
                    if (OldTransform != null)
                    {
                        Transform = OldTransform;
                    }
                    else
                    {
                        Transform = new ReactiveTransformDef();
                    }
                }
                else if (!value)
                {
                    if (Transform != null)
                        OldTransform = Transform;

                    Transform = null;
                }

                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(Transform));
            }
        }
    }
}
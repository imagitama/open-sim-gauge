using ReactiveUI;

namespace OpenGaugeClient
{
    public class ReactiveClipConfig : ReactiveObject
    {
        public ReactiveClipConfig() { }

        public ReactiveClipConfig(ClipConfig clip)
        {
            Replace(clip);
        }

        private string _image = string.Empty;
        public string Image
        {
            get => _image;
            set => this.RaiseAndSetIfChanged(ref _image, value);
        }

        private double? _width;
        public double? Width
        {
            get => _width;
            set => this.RaiseAndSetIfChanged(ref _width, value);
        }

        private double? _height;
        public double? Height
        {
            get => _height;
            set => this.RaiseAndSetIfChanged(ref _height, value);
        }

        private FlexibleVector2 _origin = new() { X = "50%", Y = "50%" };
        public FlexibleVector2 Origin
        {
            get => _origin;
            set => this.RaiseAndSetIfChanged(ref _origin, value);
        }

        private FlexibleVector2 _position = new() { X = "50%", Y = "50%" };
        public FlexibleVector2 Position
        {
            get => _position;
            set => this.RaiseAndSetIfChanged(ref _position, value);
        }

        private bool _debug;
        public bool Debug
        {
            get => _debug;
            set => this.RaiseAndSetIfChanged(ref _debug, value);
        }

        public ClipConfig ToModel()
        {
            return new ClipConfig
            {
                Image = Image,
                Width = Width,
                Height = Height,
                Origin = Origin,
                Position = Position,
                Debug = Debug
            };
        }

        public void Replace(ClipConfig newClipConfig)
        {
            Image = newClipConfig.Image;
            Width = newClipConfig.Width;
            Height = newClipConfig.Height;
            Origin = newClipConfig.Origin ?? new FlexibleVector2() { X = "50%", Y = "50%" };
            Position = newClipConfig.Position ?? new FlexibleVector2() { X = "50%", Y = "50%" };
            Debug = newClipConfig.Debug;
        }

        public override string ToString()
        {
            return $"ReactiveClipConfig {{ " +
                   $"Image='{Image}', " +
                   $"Width={Width?.ToString() ?? "null"}, " +
                   $"Height={Height?.ToString() ?? "null"}, " +
                   $"Origin={Origin}, " +
                   $"Position={Position}, " +
                   $"Debug={Debug} }}";
        }
    }
}

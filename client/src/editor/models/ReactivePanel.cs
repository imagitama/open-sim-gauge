using DynamicData;
using ReactiveUI;
using System.Reactive.Linq;

namespace OpenGaugeClient
{
    public class ReactivePanel : ReactiveObject
    {
        public ReactivePanel() { }

        public ReactivePanel(Panel panel)
        {
            Name = panel.Name;
            Vehicle = panel.Vehicle;
            Skip = panel.Skip;
            Screen = panel.Screen;
            Width = panel.Width;
            Height = panel.Height;
            Fullscreen = panel.Fullscreen;
            Position = panel.Position;
            Origin = panel.Origin;
            Background = panel.Background;
            Transparent = panel.Transparent;
            Debug = panel.Debug;

            Gauges.Edit(inner =>
            {
                inner.Clear();
                inner.AddRange(panel.Gauges.Select(l => new ReactiveGaugeRef(l)));
            });
        }

        public void Dispose() { }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        private string? _vehicle;
        public string? Vehicle
        {
            get => _vehicle;
            set => this.RaiseAndSetIfChanged(ref _vehicle, value);
        }

        private SourceList<ReactiveGaugeRef> _gauges = new();
        public SourceList<ReactiveGaugeRef> Gauges
        {
            get => _gauges;
            set => this.RaiseAndSetIfChanged(ref _gauges, value);
        }

        private bool? _skip = false;
        public bool? Skip
        {
            get => _skip;
            set => this.RaiseAndSetIfChanged(ref _skip, value);
        }

        private int? _screen = 0;
        public int? Screen
        {
            get => _screen;
            set => this.RaiseAndSetIfChanged(ref _screen, value);
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

        private bool _fullscreen;
        public bool Fullscreen
        {
            get => _fullscreen;
            set => this.RaiseAndSetIfChanged(ref _fullscreen, value);
        }

        private FlexibleVector2 _position = new() { X = "50%", Y = "50%" };
        public FlexibleVector2 Position
        {
            get => _position;
            set => this.RaiseAndSetIfChanged(ref _position, value);
        }

        private FlexibleVector2 _origin = new() { X = "50%", Y = "50%" };
        public FlexibleVector2 Origin
        {
            get => _origin;
            set => this.RaiseAndSetIfChanged(ref _origin, value);
        }

        private ColorDef? _background = new(0, 0, 0);
        public ColorDef? Background
        {
            get => _background;
            set => this.RaiseAndSetIfChanged(ref _background, value);
        }

        private bool? _transparent;
        public bool? Transparent
        {
            get => _transparent;
            set => this.RaiseAndSetIfChanged(ref _transparent, value);
        }

        private bool? _debug = false;
        public bool? Debug
        {
            get => _debug;
            set => this.RaiseAndSetIfChanged(ref _debug, value);
        }

        public void Replace(Panel panel)
        {
            Name = panel.Name;
            Vehicle = panel.Vehicle;
            Skip = panel.Skip;
            Screen = panel.Screen;
            Width = panel.Width;
            Height = panel.Height;
            Fullscreen = panel.Fullscreen;
            Position = panel.Position;
            Origin = panel.Origin;
            Background = panel.Background;
            Transparent = panel.Transparent;
            Debug = panel.Debug;

            Gauges.Edit(inner =>
            {
                inner.Clear();
                inner.AddRange(panel.Gauges.Select(l => new ReactiveGaugeRef(l)));
            });
        }

        public Panel ToPanel()
        {
            return new Panel
            {
                Name = Name,
                Vehicle = Vehicle,
                Gauges = Gauges.Items.Select(g => g.ToGaugeRef()).ToList(),
                Skip = Skip,
                Screen = Screen,
                Width = Width,
                Height = Height,
                Fullscreen = Fullscreen,
                Position = Position,
                Origin = Origin,
                Background = Background,
                Transparent = Transparent,
                Debug = Debug
            };
        }

        public override string ToString()
        {
            return $"ReactivePanel(Name={Name}, Gauges={Gauges.Count}, Screen={Screen})";
        }
    }
}

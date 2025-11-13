using ReactiveUI;

namespace OpenGaugeClient
{
    public class ReactiveTransformDef : ReactiveObject
    {
        public ReactiveTransformDef() { }

        public ReactiveTransformDef(TransformDef def)
        {
            if (def.Rotate != null)
                Rotate = new ReactiveRotateConfig(def.Rotate);

            if (def.TranslateX != null)
                TranslateX = new ReactiveTranslateConfig(def.TranslateX);

            if (def.TranslateY != null)
                TranslateY = new ReactiveTranslateConfig(def.TranslateY);

            if (def.Path != null)
                Path = new ReactivePathConfig(def.Path);
        }

        private ReactiveRotateConfig? _rotate;
        public ReactiveRotateConfig? Rotate
        {
            get => _rotate;
            set => this.RaiseAndSetIfChanged(ref _rotate, value);
        }

        private ReactiveTranslateConfig? _translateX;
        public ReactiveTranslateConfig? TranslateX
        {
            get => _translateX;
            set => this.RaiseAndSetIfChanged(ref _translateX, value);
        }

        private ReactiveTranslateConfig? _translateY;
        public ReactiveTranslateConfig? TranslateY
        {
            get => _translateY;
            set => this.RaiseAndSetIfChanged(ref _translateY, value);
        }

        private ReactivePathConfig? _path;
        public ReactivePathConfig? Path
        {
            get => _path;
            set => this.RaiseAndSetIfChanged(ref _path, value);
        }

        public TransformDef ToModel()
        {
            return new TransformDef
            {
                Rotate = Rotate?.ToModel() as RotateConfig,
                TranslateX = TranslateX?.ToModel() as TranslateConfig,
                TranslateY = TranslateY?.ToModel() as TranslateConfig,
                Path = Path?.ToModel() as PathConfig
            };
        }

        public override string ToString()
        {
            return $"ReactiveTransformDef(Rotate={(Rotate != null)}, TranslateX={(TranslateX != null)}, TranslateY={(TranslateY != null)}, Path={(Path != null)})";
        }

        private ReactiveRotateConfig? OldRotate;

        public bool HasRotate
        {
            get => Rotate != null;
            set
            {
                if (value && Rotate == null)
                {
                    if (OldRotate != null)
                    {
                        Console.WriteLine("[ReactiveTransformDef] HasRotate - re-use old");
                        Rotate = OldRotate;
                    }
                    else
                    {
                        Console.WriteLine("[ReactiveTransformDef] HasRotate - create empty");
                        Rotate = new ReactiveRotateConfig();
                    }
                }
                else if (!value)
                {
                    Console.WriteLine("[ReactiveTransformDef] HasRotate - to null");

                    if (Rotate != null)
                        OldRotate = Rotate;

                    Rotate = null;
                }

                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(Rotate));
            }
        }
        private ReactiveTranslateConfig? OldTranslateX;

        public bool HasTranslateX
        {
            get => TranslateX != null;
            set
            {
                if (value && TranslateX == null)
                {
                    if (OldTranslateX != null)
                    {
                        Console.WriteLine("[ReactiveTransformDef] HasTranslateX - re-use old");
                        TranslateX = OldTranslateX;
                    }
                    else
                    {
                        Console.WriteLine("[ReactiveTransformDef] HasTranslateX - create empty");
                        TranslateX = new ReactiveTranslateConfig();
                    }
                }
                else if (!value)
                {
                    Console.WriteLine("[ReactiveTransformDef] HasTranslateX - to null");

                    if (TranslateX != null)
                        OldTranslateX = TranslateX;

                    TranslateX = null;
                }

                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(TranslateX));
            }
        }
        private ReactiveTranslateConfig? OldTranslateY;

        public bool HasTranslateY
        {
            get => TranslateY != null;
            set
            {
                if (value && TranslateY == null)
                {
                    if (OldTranslateY != null)
                    {
                        Console.WriteLine("[ReactiveTransformDef] HasTranslateY - re-use old");
                        TranslateY = OldTranslateY;
                    }
                    else
                    {
                        Console.WriteLine("[ReactiveTransformDef] HasTranslateY - create empty");
                        TranslateY = new ReactiveTranslateConfig();
                    }
                }
                else if (!value)
                {
                    Console.WriteLine("[ReactiveTransformDef] HasTranslateY - to null");

                    if (TranslateY != null)
                        OldTranslateY = TranslateY;

                    TranslateY = null;
                }

                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(TranslateY));
            }
        }
        private ReactivePathConfig? OldPath;

        public bool HasPath
        {
            get => Path != null;
            set
            {
                if (value && Path == null)
                {
                    if (OldPath != null)
                    {
                        Console.WriteLine("[ReactiveTransformDef] HasPath - re-use old");
                        Path = OldPath;
                    }
                    else
                    {
                        Console.WriteLine("[ReactiveTransformDef] HasPath - create empty");
                        Path = new ReactivePathConfig();
                    }
                }
                else if (!value)
                {
                    Console.WriteLine("[ReactiveTransformDef] HasPath - to null");

                    if (Path != null)
                        OldPath = Path;

                    Path = null;
                }

                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(Path));
            }
        }
    }


    public class ReactiveTransformConfig : ReactiveObject
    {
        public ReactiveTransformConfig(TransformConfig cfg)
        {
            Var = cfg.Var;
            From = cfg.From;
            To = cfg.To;
            Min = cfg.Min;
            Max = cfg.Max;
            Invert = cfg.Invert;
            Multiply = cfg.Multiply;
            Calibration = cfg.Calibration;
            Skip = cfg.Skip;
            Debug = cfg.Debug;
            Override = cfg.Override;
        }

        private VarConfig _var;
        public VarConfig Var
        {
            get => _var;
            set => this.RaiseAndSetIfChanged(ref _var, value);
        }

        private double? _from;
        public double? From
        {
            get => _from;
            set => this.RaiseAndSetIfChanged(ref _from, value);
        }

        private double? _to;
        public double? To
        {
            get => _to;
            set => this.RaiseAndSetIfChanged(ref _to, value);
        }

        private double? _min;
        public double? Min
        {
            get => _min;
            set => this.RaiseAndSetIfChanged(ref _min, value);
        }

        private double? _max;
        public double? Max
        {
            get => _max;
            set => this.RaiseAndSetIfChanged(ref _max, value);
        }

        private bool _invert;
        public bool Invert
        {
            get => _invert;
            set => this.RaiseAndSetIfChanged(ref _invert, value);
        }

        private double? _multiply;
        public double? Multiply
        {
            get => _multiply;
            set => this.RaiseAndSetIfChanged(ref _multiply, value);
        }

        private List<CalibrationPoint>? _calibration;
        public List<CalibrationPoint>? Calibration
        {
            get => _calibration;
            set => this.RaiseAndSetIfChanged(ref _calibration, value);
        }

        private bool? _skip;
        public bool? Skip
        {
            get => _skip;
            set => this.RaiseAndSetIfChanged(ref _skip, value);
        }

        private bool? _debug;
        public bool? Debug
        {
            get => _debug;
            set => this.RaiseAndSetIfChanged(ref _debug, value);
        }

        private double? _override;
        public double? Override
        {
            get => _override;
            set => this.RaiseAndSetIfChanged(ref _override, value);
        }

        public virtual TransformConfig ToModel() => new TransformConfig
        {
            Var = Var,
            From = From,
            To = To,
            Min = Min,
            Max = Max,
            Invert = Invert,
            Multiply = Multiply,
            Calibration = Calibration,
            Skip = Skip,
            Debug = Debug,
            Override = Override
        };
    }

    // ──────────────────────────────────────────────────────────────────────────────

    public class ReactiveRotateConfig : ReactiveTransformConfig
    {
        public ReactiveRotateConfig() : base(new RotateConfig() { Var = null }) { }

        public ReactiveRotateConfig(RotateConfig cfg) : base(cfg)
        {
            Wrap = cfg.Wrap;
        }

        private bool _wrap;
        public bool Wrap
        {
            get => _wrap;
            set => this.RaiseAndSetIfChanged(ref _wrap, value);
        }

        public override TransformConfig ToModel() => new RotateConfig
        {
            Var = Var,
            From = From,
            To = To,
            Min = Min,
            Max = Max,
            Invert = Invert,
            Multiply = Multiply,
            Calibration = Calibration,
            Skip = Skip,
            Debug = Debug,
            Override = Override,
            Wrap = Wrap
        };
    }

    // ──────────────────────────────────────────────────────────────────────────────

    public class ReactiveTranslateConfig : ReactiveTransformConfig
    {
        public ReactiveTranslateConfig() : base(new TranslateConfig() { Var = null }) { }

        public ReactiveTranslateConfig(TranslateConfig cfg) : base(cfg) { }

        public override TransformConfig ToModel() => new TranslateConfig
        {
            Var = Var,
            From = From,
            To = To,
            Min = Min,
            Max = Max,
            Invert = Invert,
            Multiply = Multiply,
            Calibration = Calibration,
            Skip = Skip,
            Debug = Debug,
            Override = Override
        };
    }

    // ──────────────────────────────────────────────────────────────────────────────

    public class ReactivePathConfig : ReactiveTransformConfig
    {
        public ReactivePathConfig() : base(new PathConfig() { Image = "", Var = null }) { }
        public ReactivePathConfig(PathConfig cfg) : base(cfg)
        {
            Image = cfg.Image;
            Width = cfg.Width;
            Height = cfg.Height;
            Origin = cfg.Origin;
            Position = cfg.Position;
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

        public override TransformConfig ToModel() => new PathConfig
        {
            Var = Var,
            From = From,
            To = To,
            Min = Min,
            Max = Max,
            Invert = Invert,
            Multiply = Multiply,
            Calibration = Calibration,
            Skip = Skip,
            Debug = Debug,
            Override = Override,
            Image = Image,
            Width = Width,
            Height = Height,
            Origin = Origin,
            Position = Position
        };
    }
}

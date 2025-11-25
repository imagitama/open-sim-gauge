using ReactiveUI;

namespace OpenGaugeClient
{
    public class ReactiveTextDef : ReactiveObject
    {
        public ReactiveTextDef() { }

        public ReactiveTextDef(TextDef def)
        {
            Var = def.Var;
            Default = def.Default;
            Template = def.Template;
            FontSize = def.FontSize;
            FontFamily = def.FontFamily;
            Font = def.Font;
            Color = def.Color;
        }

        private SimVarConfig? _var;
        public SimVarConfig? Var
        {
            get => _var;
            set => this.RaiseAndSetIfChanged(ref _var, value);
        }

        private string? _default;
        public string? Default
        {
            get => _default;
            set => this.RaiseAndSetIfChanged(ref _default, value);
        }

        private string? _template;
        public string? Template
        {
            get => _template;
            set => this.RaiseAndSetIfChanged(ref _template, value);
        }

        private double _fontSize = 64;
        public double FontSize
        {
            get => _fontSize;
            set => this.RaiseAndSetIfChanged(ref _fontSize, value);
        }

        private string? _fontFamily;
        public string? FontFamily
        {
            get => _fontFamily;
            set => this.RaiseAndSetIfChanged(ref _fontFamily, value);
        }

        private string? _font;
        public string? Font
        {
            get => _font;
            set => this.RaiseAndSetIfChanged(ref _font, value);
        }

        private ColorDef? _color = new ColorDef(255, 255, 255);
        public ColorDef? Color
        {
            get => _color;
            set => this.RaiseAndSetIfChanged(ref _color, value);
        }

        public TextDef ToModel()
        {
            return new TextDef
            {
                Var = Var,
                Default = Default,
                Template = Template,
                FontSize = FontSize,
                FontFamily = FontFamily,
                Font = Font,
                Color = Color
            };
        }

        public override string ToString()
            => $"ReactiveTextDef(Default={Default ?? "none"}, Template={Template ?? "none"}, FontSize={FontSize})";
    }
}

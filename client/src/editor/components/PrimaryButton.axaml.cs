using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Windows.Input;

namespace OpenGaugeClient.Editor.Components
{
    public enum ButtonSize
    {
        Small,
        Standard
    }

    public class IconOrLabelConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            var icon = values[0] as string;
            var label = values[1] as string;
            return !string.IsNullOrWhiteSpace(icon) ? icon : label;
        }
    }


    public partial class PrimaryButton : UserControl
    {
        public static readonly StyledProperty<string> IconProperty =
            AvaloniaProperty.Register<PrimaryButton, string>(nameof(Icon));
        public static readonly StyledProperty<ButtonSize?> SizeProperty =
            AvaloniaProperty.Register<PrimaryButton, ButtonSize?>(nameof(Size));
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<PrimaryButton, string>(nameof(Label));
        public static readonly StyledProperty<int> PositivityProperty =
            AvaloniaProperty.Register<PrimaryButton, int>(nameof(Positivity));
        public static readonly StyledProperty<bool> SelectedProperty =
            AvaloniaProperty.Register<PrimaryButton, bool>(nameof(Selected));
        public static readonly StyledProperty<bool> DebugProperty =
            AvaloniaProperty.Register<PrimaryButton, bool>(nameof(Debug));
        public static readonly StyledProperty<ICommand?> CommandProperty =
            AvaloniaProperty.Register<PrimaryButton, ICommand?>(nameof(Command));
        public static readonly StyledProperty<object?> CommandParameterProperty =
            AvaloniaProperty.Register<PrimaryButton, object?>(nameof(CommandParameter));
        public string Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
        public ButtonSize? Size
        {
            get => GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
        public int Positivity
        {
            get => GetValue(PositivityProperty);
            set => SetValue(PositivityProperty, value);
        }
        public bool Selected
        {
            get => GetValue(SelectedProperty);
            set => SetValue(SelectedProperty, value);
        }
        public bool Debug
        {
            get => GetValue(DebugProperty);
            set => SetValue(DebugProperty, value);
        }
        public ICommand? Command
        {
            get => GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }
        private readonly CompositeDisposable _cleanup = new();

        public PrimaryButton()
        {
            InitializeComponent();

            var button = this.FindControl<Button>("PART_Button") ?? throw new Exception("No button part");

            this.GetObservable(PositivityProperty).Subscribe(value =>
            {
                button.Tag = value switch
                {
                    -1 => "Bad",
                    1 => "Good",
                    _ => "Neutral"
                };
            }).DisposeWith(_cleanup);

            this.GetObservable(LabelProperty).Subscribe(label =>
            {
                button.MaxWidth = !string.IsNullOrEmpty(label) && new StringInfo(label).LengthInTextElements > 1
                    ? double.PositiveInfinity
                    : 30;
            }).DisposeWith(_cleanup);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _cleanup.Dispose();
        }
    }
}

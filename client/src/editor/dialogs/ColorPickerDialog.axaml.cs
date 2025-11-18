using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ReactiveUI;

namespace OpenGaugeClient.Editor
{
    public partial class ColorPickerDialog : Window
    {
        public Color? SelectedColor { get; private set; }

        public ColorPickerDialog(Color initial)
        {
            InitializeComponent();
            Picker.Color = initial;
            DataContext = new ColorPickerDialogViewModel();

            Console.WriteLine($"[ColorPickerDialog] Construct initial={initial}");
        }

        private void OnOk(object? sender, RoutedEventArgs e)
        {
            SelectedColor = Picker.Color;

            var colorStr = (DataContext as ColorPickerDialogViewModel).OverrideColor;

            if (Color.TryParse(colorStr, out var avaloniaColor))
            {
                SelectedColor = avaloniaColor;
            }

            Console.WriteLine($"[ColorPickerDialog] Clicked ok color={SelectedColor}");
            Close(true);
        }

        private void OnCancel(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine($"[ColorPickerDialog] Clicked cancel");
            Close(false);
        }

        private void OnClear(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine($"[ColorPickerDialog] Clicked clear");

            SelectedColor = null;

            Close(true);
        }
    }

    public class ColorPickerDialogViewModel : ReactiveObject
    {
        private string? _overrideColor;
        public string? OverrideColor
        {
            get => _overrideColor;
            set => this.RaiseAndSetIfChanged(ref _overrideColor, value);
        }
    }
}
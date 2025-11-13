using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace OpenGaugeClient.Editor
{
    public partial class ColorPickerDialog : Window
    {
        public Color SelectedColor { get; private set; }

        public ColorPickerDialog(Color initial)
        {
            InitializeComponent();
            Picker.Color = initial;
        }

        private void OnOk(object? sender, RoutedEventArgs e)
        {
            SelectedColor = Picker.Color;
            Console.WriteLine($"[ColorPickerDialog] Clicked ok color={SelectedColor}");
            Close(true);
        }

        private void OnCancel(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine($"[ColorPickerDialog] Clicked cancel");
            Close(false);
        }
    }
}
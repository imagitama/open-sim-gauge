using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OpenGaugeClient.Editor
{
    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog()
        {
            InitializeComponent();
        }

        private void OnYes(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine($"[ConfirmDialog] Clicked yes");
            Close(true);
        }

        private void OnNo(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine($"[ConfirmDialog] Clicked no");
            Close(false);
        }
    }
}
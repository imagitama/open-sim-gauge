using Avalonia.Controls;
using Avalonia.Media;
using OpenGaugeClient.Shared;

namespace OpenGaugeClient.Editor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void ShowView(Control control)
        {
            Width = WindowHelper.DefaultWidth;
            Height = WindowHelper.DefaultHeight;
            Background = new SolidColorBrush(WindowHelper.DefaultBackground);
            ExtendClientAreaToDecorationsHint = false;
            ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.Default;
            TransparencyLevelHint = [WindowTransparencyLevel.None];
            SystemDecorations = SystemDecorations.Full;
            Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
            Title = "Open Sim Gauge";

            ContentHost.Content = control;
        }
    }
}
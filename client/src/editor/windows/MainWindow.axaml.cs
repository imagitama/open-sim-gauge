using Avalonia.Controls;

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
            ContentHost.Content = control;
        }
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Avalonia.Controls.ApplicationLifetimes;

namespace OpenGaugeClient.Editor.Components
{
    public partial class ColorPickerField : UserControl
    {
        public static readonly StyledProperty<Color?> ValueProperty =
            AvaloniaProperty.Register<ColorPickerField, Color?>(nameof(Value));

        public event Action<Color?>? ColorCommitted;

        public Color? Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private readonly CompositeDisposable _cleanup = new();

        public ColorPickerField()
        {
            InitializeComponent();
            PickButton.Click += OnPick;
            this.GetObservable(ValueProperty).Subscribe(UpdatePreview).DisposeWith(_cleanup);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _cleanup.Dispose();
        }

        private void UpdatePreview(Color? color)
        {
            Preview.Background = color.HasValue
                ? new SolidColorBrush(color.Value)
                : Brushes.Transparent;
        }

        private async void OnPick(object? sender, RoutedEventArgs e)
        {
            Window? owner = null;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is Window mainWindow)
            {
                owner = mainWindow;
            }

            if (owner == null)
                throw new Exception("Cannot pick without owner");

            // TODO: fix defaulting to transparent
            var startColor = Value ?? Color.FromArgb(0, 255, 255, 255);
            var dialog = new ColorPickerDialog(startColor);
            var ok = await dialog.ShowDialog<bool>(owner);

            if (ok)
            {
                Value = dialog.SelectedColor;
                ColorCommitted?.Invoke(dialog.SelectedColor);
            }
        }
    }
}

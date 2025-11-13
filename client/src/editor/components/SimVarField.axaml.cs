using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace OpenGaugeClient.Editor.Components
{
    public partial class SimVarField : UserControl
    {
        public static readonly StyledProperty<VarConfig?> ValueProperty =
            AvaloniaProperty.Register<SimVarField, VarConfig?>(nameof(Value));
        public IReactiveCommand PickCommand { get; }
        public event Action<VarConfig?>? VarConfigCommitted;
        public VarConfig? Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        private readonly CompositeDisposable _cleanup = new();

        public SimVarField()
        {
            InitializeComponent();

            PickCommand = ReactiveCommand.CreateFromTask(Pick);

            this.GetObservable(ValueProperty).Subscribe(UpdatePreview).DisposeWith(_cleanup);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _cleanup.Dispose();
        }

        private void UpdatePreview(VarConfig? varConfig)
        {
            if (varConfig == null)
            {
                Preview.Text = "(none)";
                return;
            }

            var name = varConfig.Name;
            if (name.Length > 10)
                name = $"{name[..10]}...";
            Preview.Text = $"{name}\n{varConfig.Unit}";
        }

        private async Task Pick()
        {
            Window? owner = null;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is Window mainWindow)
            {
                owner = mainWindow;
            }

            if (owner == null)
                throw new Exception("Cannot pick without owner");

            Console.WriteLine($"[SimVarField] Pick value={Value}");

            var dialog = new SimVarDialog(Value);
            var ok = await dialog.ShowDialog<bool>(owner);

            Console.WriteLine($"[SimVarField] Pick ok={ok} selected={dialog.SelectedVarConfig}");

            if (ok)
            {
                Value = dialog.SelectedVarConfig;
                VarConfigCommitted?.Invoke(dialog.SelectedVarConfig);
            }
        }
    }
}

using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using ReactiveUI;

namespace OpenGaugeClient.Editor.Components
{
    public partial class SelectFileField : UserControl
    {
        public static readonly StyledProperty<string?> PathProperty =
            AvaloniaProperty.Register<SelectFileField, string?>(nameof(Path));
        public string? Path
        {
            get => GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }
        public static readonly StyledProperty<string[]?> AllowedExtensionsProperty =
            AvaloniaProperty.Register<SelectFileField, string[]?>(nameof(AllowedExtensions));
        public string[]? AllowedExtensions
        {
            get => GetValue(AllowedExtensionsProperty);
            set => SetValue(AllowedExtensionsProperty, value);
        }
        public static readonly StyledProperty<bool> DirectoryOnlyProperty =
            AvaloniaProperty.Register<SelectFileField, bool>(nameof(DirectoryOnly));
        public bool? DirectoryOnly
        {
            get => GetValue(DirectoryOnlyProperty);
            set => SetValue(DirectoryOnlyProperty, value);
        }
        public IReactiveCommand PickCommand { get; }
        public event Action<string?>? FileCommitted;
        private readonly CompositeDisposable _cleanup = new();

        public SelectFileField()
        {
            InitializeComponent();

            PickCommand = ReactiveCommand.CreateFromTask(Pick);

            this.GetObservable(PathProperty).Subscribe(UpdateFileName).DisposeWith(_cleanup);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _cleanup.Dispose();
        }

        private void UpdateFileName(string? newPath)
        {
            FileName.Text = string.IsNullOrWhiteSpace(newPath)
                ? "(none)"
                : PathHelper.GetShortFileName(newPath);
        }

        private async Task Pick()
        {
            Window? owner = VisualRoot as Window;

            if (owner == null)
                throw new Exception("Cannot pick without owner");

            Console.WriteLine($"[SelectFileField] Dialog owner={owner}");

            var dialog = new SelectFileDialog(AllowedExtensions ?? [], directoriesOnly: DirectoryOnly == true);
            var result = await dialog.ShowDialog<bool>(owner);

            // TODO: do this better
            var relative = dialog.ViewModel.RelativePath;
            var absolute = dialog.ViewModel.AbsolutePath;

            Console.WriteLine($"[SelectFileField] Dialog complete relative={relative} absolute={absolute}");

            if (!string.IsNullOrWhiteSpace(absolute))
            {
                Path = absolute;
                FileCommitted?.Invoke(absolute);
            }
        }
    }
}

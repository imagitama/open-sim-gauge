using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace OpenGaugeClient.Editor
{
    public partial class SelectFileDialog : Window
    {
        public SelectFileDialogViewModel ViewModel { get; }

        public SelectFileDialog(string[]? allowedExtensions = null, bool directoriesOnly = false)
        {
            InitializeComponent();
            DataContext = ViewModel = new SelectFileDialogViewModel(allowedExtensions, directoriesOnly);

            ViewModel.CloseRequested.RegisterHandler(ctx =>
            {
                Console.WriteLine($"[SelectFileDialog] Close requested input={ctx.Input}");
                Close(ctx.Input);
                ctx.SetOutput(true);
            });

            ViewModel.OnBrowse = OnBrowse;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            (DataContext as SelectFileDialogViewModel)?.Dispose();
        }

        private async Task<string?> OnBrowse(string basePath, string[]? allowedExtensions = null, bool? directoriesOnly = false)
        {
            Console.WriteLine($"[SelectFileDialog] Clicked browse owner={Owner} base={basePath} ext={allowedExtensions} dirs={directoriesOnly}");

            if (Owner == null)
                throw new Exception("No owner");

            if (Owner is not Window)
                throw new Exception("Owner is not a window");

            var parentWindow = (Owner as Window)!;

            if (directoriesOnly == true)
            {
                var uri = new Uri(basePath, UriKind.Absolute);
                var storageProvider = parentWindow.StorageProvider;
                var startFolder = await storageProvider.TryGetFolderFromPathAsync(uri);

                Console.WriteLine($"[SelectFileDialog] Open folder picker");

                var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Folder",
                    SuggestedStartLocation = startFolder,
                    AllowMultiple = false
                });

                Console.WriteLine($"[SelectFileDialog] Open folder picker result={result}");

                if (result.Count > 0)
                {
                    var folder = result[0];
                    var path = folder.Path.LocalPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    Console.WriteLine($"[SelectFileDialog] Open folder picker path={path}");
                    return path;
                }
            }
            else
            {
                var uri = new Uri(basePath, UriKind.Absolute);
                var storageProvider = parentWindow.StorageProvider;
                var startFolder = await storageProvider.TryGetFolderFromPathAsync(uri);

                Console.WriteLine($"[SelectFileDialog] Open file picker");

                var patterns = allowedExtensions?
                    .Select(ext => ext.StartsWith("*.") ? ext : $"*.{ext.TrimStart('.')}")
                    .ToArray();

                var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select Folder",
                    SuggestedStartLocation = startFolder,
                    AllowMultiple = false,
                    FileTypeFilter = patterns != null ? [
                        new FilePickerFileType($"Allowed files ({string.Join(", ", patterns)})")
                        {
                            Patterns = patterns
                        }
                    ] : null
                });

                Console.WriteLine($"[SelectFileDialog] Open file picker result={result}");

                if (result.Count > 0)
                {
                    var folder = result[0];
                    var path = folder.Path.LocalPath;
                    Console.WriteLine($"[SelectFileDialogViewModel] Got path '{path}'");
                    return path;
                }
            }

            return null;
        }
    }

    public class SelectFileDialogViewModel : ReactiveObject
    {
        public Interaction<bool, bool?> CloseRequested { get; }
        private bool _directoriesOnly;
        public bool DirectoriesOnly
        {
            get => _directoriesOnly;
            set => this.RaiseAndSetIfChanged(ref _directoriesOnly, value);
        }
        public string Title => _directoriesOnly ? "Select Folder" : "Select File";
        private string[]? _allowedExtensions;
        public string[]? AllowedExtensions
        {
            get => _allowedExtensions;
            set => this.RaiseAndSetIfChanged(ref _allowedExtensions, value);
        }
        public string AllowedExtensionsDisplay =>
            _allowedExtensions == null || _allowedExtensions.Length == 0 ? "All files allowed" : string.Join(", ", _allowedExtensions);
        private string? _enteredPath;
        private string _basePath;

        public string? EnteredPath
        {
            get => _enteredPath;
            set => this.RaiseAndSetIfChanged(ref _enteredPath, value);
        }
        private readonly ObservableAsPropertyHelper<string?> _absolutePath;
        public string? AbsolutePath => _absolutePath.Value;
        private readonly ObservableAsPropertyHelper<string?> _relativePath;
        public string? RelativePath => _relativePath.Value;
        public ReactiveCommand<Unit, Unit> BrowseCommand { get; }
        public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        private readonly CompositeDisposable _cleanup = new();
        public Func<string, string[]?, bool?, Task<string?>>? OnBrowse;

        public SelectFileDialogViewModel(string[]? allowedExtensions, bool directoriesOnly)
        {
            CloseRequested = new Interaction<bool, bool?>();

            _directoriesOnly = directoriesOnly;
            _allowedExtensions = allowedExtensions;
            _basePath = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

            BrowseCommand = ReactiveCommand.CreateFromTask(Browse);
            ConfirmCommand = ReactiveCommand.Create(Confirm);
            CancelCommand = ReactiveCommand.Create(Cancel);

            this.WhenAnyValue(x => x.EnteredPath)
                .Select(p => !string.IsNullOrWhiteSpace(p) ? Path.GetFullPath(p) : null)
                .ToProperty(this, x => x.AbsolutePath, out _absolutePath)
                .DisposeWith(_cleanup);

            this.WhenAnyValue(x => x.EnteredPath)
                .Select(p => !string.IsNullOrWhiteSpace(p)
                    ? Path.GetRelativePath(Environment.CurrentDirectory, p)
                    : null)
                .ToProperty(this, x => x.RelativePath, out _relativePath)
                .DisposeWith(_cleanup);

            Console.WriteLine($"[SelectFileDialogViewModel] ext={string.Join(",", allowedExtensions ?? [])} dir={directoriesOnly}");
        }

        public void Dispose()
        {
            _cleanup.Dispose();
        }

        private async Task Browse()
        {
            Console.WriteLine($"[SelectFileDialogViewModel] Clicked browse");

            if (OnBrowse is not null)
            {
                var result = await OnBrowse(_basePath, _allowedExtensions, _directoriesOnly);

                Console.WriteLine($"[SelectFileDialogViewModel] Browse result={result}");

                EnteredPath = result;
            }
        }

        private void Confirm()
        {
            Console.WriteLine($"[SelectFileDialogViewModel] Clicked confirm path={EnteredPath}");

            if (string.IsNullOrWhiteSpace(EnteredPath))
                return;

            _ = CloseWindow(true);
        }

        private void Cancel()
        {
            Console.WriteLine($"[SelectFileDialogViewModel] Clicked cancel");
            _ = CloseWindow(false);
        }

        private async Task CloseWindow(bool result)
        {
            Console.WriteLine($"[SelectFileDialogViewModel] Close window result={result} value={EnteredPath}");
            await CloseRequested.Handle(result);
        }
    }
}

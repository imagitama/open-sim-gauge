using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace OpenGaugeClient.Editor
{
    public partial class CreateSvgDialog : Window
    {
        public CreateSvgDialogViewModel ViewModel;
        public SvgCreator SvgCreator { get; private set; }

        public CreateSvgDialog()
        {
            InitializeComponent();

            Console.WriteLine($"[CreateSvgDialog] Instantiate");

            ViewModel = new CreateSvgDialogViewModel();
            DataContext = ViewModel;

            ViewModel.OnLoad = OnLoad;
            ViewModel.OnCreate = OnCreate;
            ViewModel.OnCancel = OnCancel;
        }

        public async Task OnLoad(string filePath)
        {
            Console.WriteLine($"[CreateSvgDialogViewModel] Clicked load existing file={filePath}");

            if (string.IsNullOrEmpty(filePath))
                return;

            var svgCreator = await SvgCreatorUtils.LoadSvgCreator(filePath);

            SvgCreator = svgCreator;

            Close(true);
        }

        public async Task OnCreate(string jsonPath)
        {
            try
            {
                Console.WriteLine($"[CreateSvgDialogViewModel] Clicked create new SVG json={jsonPath}");

                if (string.IsNullOrEmpty(jsonPath))
                    throw new Exception("Need a path");

                var svgCreator = new SvgCreator()
                {
                    Width = 500,
                    Height = 500,
                    Layers = []
                };

                await SvgCreatorUtils.SaveSvgCreator(svgCreator, jsonPath);

                SvgCreator = svgCreator;
                SvgCreator.Source = jsonPath;

                Close(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateSvgDialogViewModel] Failed to create: {ex}");
            }
        }

        public void OnCancel()
        {
            Console.WriteLine($"[CreateSvgDialogViewModel] Clicked cancel");
            Close(false);
        }
    }

    public class CreateSvgDialogViewModel : ReactiveObject
    {
        public Interaction<(bool, string), bool?> CloseRequested { get; }
        private string _filePath = "";
        public string FilePath
        {
            get => _filePath;
            set => this.RaiseAndSetIfChanged(ref _filePath, value);
        }
        private string _dirPath = "";
        public string DirPath
        {
            get => _dirPath;
            set
            {
                this.RaiseAndSetIfChanged(ref _dirPath, value);
                this.RaisePropertyChanged(nameof(HasDirPath));
            }
        }
        public bool HasDirPath => !string.IsNullOrWhiteSpace(DirPath);
        private readonly ObservableAsPropertyHelper<string> _jsonPath;
        public string JsonPath => _jsonPath.Value;
        public ReactiveCommand<Unit, Unit> LoadCommand { get; }
        public ReactiveCommand<Unit, Unit> CreateCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public Func<string, Task> OnLoad;
        public Func<string, Task> OnCreate;
        public Action OnCancel;

        public CreateSvgDialogViewModel()
        {
            CloseRequested = new Interaction<(bool, string), bool?>();

            LoadCommand = ReactiveCommand.Create(Load);
            CreateCommand = ReactiveCommand.CreateFromTask(Create);
            CancelCommand = ReactiveCommand.Create(Cancel);

            this.WhenAnyValue(x => x.DirPath)
                .Select(dir => Path.Combine(dir ?? "", "svg.json"))
                .ToProperty(this, x => x.JsonPath, out _jsonPath);
        }

        private void Load()
        {
            Console.WriteLine($"[CreateSvgDialogViewModel] On click load file={FilePath}");
            OnLoad?.Invoke(FilePath);
        }

        private async Task Create()
        {
            Console.WriteLine($"[CreateSvgDialogViewModel] On click create json={JsonPath}");
            OnCreate?.Invoke(JsonPath);
        }

        private void Cancel()
        {
            Console.WriteLine($"[CreateSvgDialogViewModel] On click cancel");
            OnCancel?.Invoke();
        }
    }
}
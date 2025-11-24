using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using System.Reactive.Linq;

namespace OpenGaugeClient.Editor
{
    public partial class CreateGaugeDialog : Window
    {
        public (string, string DirPath) LastValue { get; private set; }

        public CreateGaugeDialog()
        {
            InitializeComponent();
            var _vm = new CreateGaugeDialogViewModel();
            DataContext = _vm;

            _vm.CloseRequested.RegisterHandler(ctx =>
            {
                var (result, lastValue) = ctx.Input;

                LastValue = lastValue;

                Console.WriteLine($"[CreateGaugeDialog] CloseRequested handler result={result} lastValue={LastValue}");

                Close(result);

                ctx.SetOutput(true);
            });
        }
    }

    public class CreateGaugeDialogViewModel : ReactiveObject
    {
        public Interaction<(bool, (string, string DirPath)), bool?> CloseRequested { get; }
        private string _name = "";
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
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
        public ReactiveCommand<Unit, Unit> OkCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public CreateGaugeDialogViewModel()
        {
            CloseRequested = new Interaction<(bool, (string, string)), bool?>();

            OkCommand = ReactiveCommand.Create(OnOk);
            CancelCommand = ReactiveCommand.Create(OnCancel);

            this.WhenAnyValue(x => x.DirPath)
                .Select(dir => Path.Combine(dir ?? "", "gauge.json"))
                .ToProperty(this, x => x.JsonPath, out _jsonPath);
        }

        public void OnOk()
        {
            Console.WriteLine($"[CreateGaugeDialogViewModel] Clicked ok name={Name} path={DirPath}");

            _ = CloseWindow(true);
        }

        public void OnCancel()
        {
            Console.WriteLine($"[CreateGaugeDialogViewModel] Clicked cancel");

            _ = CloseWindow(false);
        }

        private async Task CloseWindow(bool result)
        {
            Console.WriteLine($"[CreateGaugeDialogViewModel] Close window result={result} name={Name} path={DirPath} json={JsonPath}");

            await CloseRequested.Handle(
                (result, (Name, JsonPath))
            );
        }
    }
}
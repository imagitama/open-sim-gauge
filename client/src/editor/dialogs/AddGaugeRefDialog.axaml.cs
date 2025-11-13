using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using System.Reactive.Linq;
using System.Collections.ObjectModel;

namespace OpenGaugeClient.Editor
{
    public partial class AddGaugeRefDialog : Window
    {
        public (string, string?) LastValue { get; private set; }

        public AddGaugeRefDialog()
        {
            InitializeComponent();
            var _vm = new AddGaugeRefDialogViewModel();
            DataContext = _vm;

            _vm.CloseRequested.RegisterHandler(ctx =>
            {
                var (result, lastValue) = ctx.Input;

                LastValue = lastValue;

                Console.WriteLine($"[AddGaugeRefDialog] CloseRequested handler result={result} lastValue={lastValue}");

                Close(result);

                ctx.SetOutput(true);
            });
        }
    }

    public class AddGaugeRefDialogViewModel : ReactiveObject
    {
        public Interaction<(bool, (string, string?)), bool?> CloseRequested { get; }
        private string _name = "";
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }
        private string _path = "";
        public string Path
        {
            get => _path;
            set => this.RaiseAndSetIfChanged(ref _path, value);
        }
        public ReactiveCommand<Unit, Unit> OkCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public ReactiveCommand<string, Unit> SelectGaugeNameCommand { get; }
        public ObservableCollection<string> GaugeNames { get; } = new();

        public AddGaugeRefDialogViewModel()
        {
            CloseRequested = new Interaction<(bool, (string, string?)), bool?>();

            OkCommand = ReactiveCommand.Create(OnOk);
            CancelCommand = ReactiveCommand.Create(OnCancel);
            SelectGaugeNameCommand = ReactiveCommand.CreateFromTask<string>(SelectGaugeName);

            LoadItems();
        }

        private void LoadItems()
        {
            GaugeNames.Clear();

            var rootGaugeNames = ConfigManager.Config.Gauges.Select(g => g.Name);

            foreach (var name in rootGaugeNames)
                GaugeNames.Add(name);
        }

        private async Task SelectGaugeName(string gaugeName)
        {
            Console.WriteLine($"[AddGaugeRefDialogViewModel] Select gauge by name '{gaugeName}'");

            await CloseRequested.Handle(
             (true, (gaugeName, null))
         );
        }

        public void OnOk()
        {
            Console.WriteLine($"[AddGaugeRefDialogViewModel] Confirm name='{Name}' unit='{Path}'");

            _ = CloseWindow(true);
        }

        public void OnCancel()
        {
            Console.WriteLine($"[AddGaugeRefDialogViewModel] Cancel");
            _ = CloseWindow(false);
        }

        private async Task CloseWindow(bool result)
        {
            Console.WriteLine($"[AddGaugeRefDialogViewModel] Close window result={result} name='{Name}' path='{Path}'");

            await CloseRequested.Handle(
                (result, (Name, Path))
            );
        }
    }
}
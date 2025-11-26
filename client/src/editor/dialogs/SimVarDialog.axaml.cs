using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using System.Reactive.Linq;
using System.Diagnostics;

namespace OpenGaugeClient.Editor
{
    public partial class SimVarDialog : Window
    {
        public SimVarConfig? SelectedSimVarConfig { get; private set; }

        public SimVarDialog(SimVarConfig? initial)
        {
            InitializeComponent();
            var _vm = new SimVarDialogViewModel(initial);
            DataContext = _vm;

            _vm.CloseRequested.RegisterHandler(ctx =>
            {
                var (result, simVar) = ctx.Input;

                SelectedSimVarConfig = simVar;

                Console.WriteLine($"[SimVarDialog] CloseRequested handler result={result} simVar={simVar}");

                Close(result);

                ctx.SetOutput(true);
            });
        }

        private void OpenWebsite_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var url = "https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm";
            try
            {
                // TODO: Move to helper
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open URL: {ex.Message}");
            }
        }
    }

    public class SimVarDialogViewModel : ReactiveObject
    {
        public Interaction<(bool, SimVarConfig), bool?> CloseRequested { get; }
        private string _varName = "";
        public string Name
        {
            get => _varName;
            set => this.RaiseAndSetIfChanged(ref _varName, value);
        }
        private string _varUnit = "";
        public string Unit
        {
            get => _varUnit;
            set => this.RaiseAndSetIfChanged(ref _varUnit, value);
        }
        private double? _override;
        public double? Override
        {
            get => _override;
            set => this.RaiseAndSetIfChanged(ref _override, value);
        }
        public ReactiveCommand<Unit, Unit> OkCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public SimVarDialogViewModel(SimVarConfig? initial)
        {
            CloseRequested = new Interaction<(bool, SimVarConfig), bool?>();

            OkCommand = ReactiveCommand.Create(OnOk);
            CancelCommand = ReactiveCommand.Create(OnCancel);

            if (initial != null)
            {
                Name = initial.Name;
                Unit = initial.Unit;
                Override = initial.Override;
            }
        }

        public void OnOk()
        {
            Console.WriteLine($"[SimVarDialogViewModel] On click ok name={Name} unit={Unit} override={Override}");

            _ = CloseWindow(true);
        }

        public void OnCancel()
        {
            Console.WriteLine($"[SimVarDialogViewModel] On click cancel");

            _ = CloseWindow(false);
        }

        private async Task CloseWindow(bool result)
        {
            var varConfig = new SimVarConfig()
            {
                Name = Name,
                Unit = Unit,
                Override = Override
            };

            Console.WriteLine($"[SimVarDialogViewModel] Close window result={result} var={varConfig}");

            await CloseRequested.Handle(
                (result, varConfig)
            );
        }
    }
}
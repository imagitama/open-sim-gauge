using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using System.Reactive.Linq;
using System.Diagnostics;

namespace OpenGaugeClient.Editor
{
    public partial class SimVarDialog : Window
    {
        public VarConfig? SelectedVarConfig { get; private set; }

        public SimVarDialog(VarConfig? initial)
        {
            InitializeComponent();
            var _vm = new SimVarDialogViewModel(initial);
            DataContext = _vm;

            _vm.CloseRequested.RegisterHandler(ctx =>
            {
                var (result, simVar) = ctx.Input;

                SelectedVarConfig = simVar;

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
        public Interaction<(bool, VarConfig), bool?> CloseRequested { get; }
        private string _simVarName = "";
        public string SimVarName
        {
            get => _simVarName;
            set => this.RaiseAndSetIfChanged(ref _simVarName, value);
        }
        private string _simVarUnit = "";
        public string SimVarUnit
        {
            get => _simVarUnit;
            set => this.RaiseAndSetIfChanged(ref _simVarUnit, value);
        }
        public ReactiveCommand<Unit, Unit> OkCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public SimVarDialogViewModel(VarConfig? initial)
        {
            CloseRequested = new Interaction<(bool, VarConfig), bool?>();

            OkCommand = ReactiveCommand.Create(OnOk);
            CancelCommand = ReactiveCommand.Create(OnCancel);

            if (initial != null)
            {
                SimVarName = initial.Name;
                SimVarUnit = initial.Unit;
            }
        }

        public void OnOk()
        {
            Console.WriteLine($"[SimVarDialogViewModel] Clicked ok name='{SimVarName}' unit='{SimVarUnit}'");

            _ = CloseWindow(true);
        }

        public void OnCancel()
        {
            Console.WriteLine($"[SimVarDialogViewModel] Clicked cancel");

            _ = CloseWindow(false);
        }

        private async Task CloseWindow(bool result)
        {
            var varConfig = new VarConfig()
            {
                Name = SimVarName,
                Unit = SimVarUnit
            };

            Console.WriteLine($"[SimVarDialogViewModel] Close window result={result} var={varConfig}");

            await CloseRequested.Handle(
                (result, varConfig)
            );
        }
    }
}
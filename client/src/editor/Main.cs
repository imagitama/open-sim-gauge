using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using ReactiveUI.Avalonia;
using OpenGaugeClient.Editor.Services;
using OpenGaugeClient.Shared;
using ReactiveUI;
using System.Reactive;

namespace OpenGaugeClient.Editor
{
    public class Program
    {
        public static string[] StartupArgs = Array.Empty<string>();

        static void Main(string[] args)
        {
            StartupArgs = args;

            string logPath = Path.Combine(AppContext.BaseDirectory, "editor.log");
            var fileWriter = new StreamWriter(logPath, append: true) { AutoFlush = true };
            Console.SetOut(new TeeTextWriter(Console.Out, fileWriter));
            Console.SetError(new TeeTextWriter(Console.Error, fileWriter));

            Console.WriteLine("Starting up...");
            Console.Out.Flush();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseReactiveUI()
             .LogToTrace(Avalonia.Logging.LogEventLevel.Verbose);
    }

    public partial class App : Application
    {
        public override async void OnFrameworkInitializationCompleted()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                Console.WriteLine($"[Global] Unhandled exception: {e.ExceptionObject}");
            };
            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                Console.WriteLine($"[UI Thread] {e.Exception}");
                e.Handled = true;
            };
            RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
            {
                Console.WriteLine($"[Rx Exception] {ex}");
            });

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

                var configPath = Cli.GetConfigPathFromArgs(Program.StartupArgs);

                var config = await ConfigManager.LoadConfig(configPath);

                var persistedState = await PersistanceManager.LoadState();

                try
                {
                    var uri = new Uri("avares://OpenSimGaugeEditor/GlobalStyles.axaml");
                    Current?.Styles.Add(new StyleInclude(uri) { Source = uri });

#if DEBUG
                    this.AttachDevTools();
#endif

                    NavigationService.Instance.Register("Main", _ => new MainMenuView());
                    NavigationService.Instance.Register("PanelEditor", param => new PanelEditorView((int)param[0]!));
                    NavigationService.Instance.Register("GaugeEditor", param => new GaugeEditorView((int?)param[0], (Gauge?)param[1]));

                    desktop.MainWindow = new MainWindow();

                    NavigationService.Instance.GoToView("Main");

                    desktop.MainWindow.Show();

                    SetupDebugOverlay();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to start editor: {ex}");
                }
            }
        }

        // TODO: Move to helper/service
        private void SetupDebugOverlay()
        {
            List<Window> debugOverlayWindows = new();

            var isClosing = false;

            var CloseAllOverlays = () =>
            {
                if (isClosing)
                    return;

                Console.WriteLine("[Main] Closing all debug overlay windows...");

                isClosing = true;
                foreach (var win in debugOverlayWindows)
                {
                    win.Close();
                }
                debugOverlayWindows.Clear();
                isClosing = false;
            };

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                SettingsService.Instance
                    .WhenAnyValue(x => x.OverlayVisible)
                    .Subscribe(visible =>
                    {
                        Console.WriteLine($"[Main] overlay visible => {visible}");

                        if (visible)
                        {
                            debugOverlayWindows.Clear();

                            // remember: window positions are relative to entire span of desktop (screens are joined together)
                            foreach (var screen in desktop.MainWindow!.Screens.All)
                            {
                                var debugOverlayWindow = new DebugOverlayWindow(screen);
                                debugOverlayWindow.Show();

                                debugOverlayWindows.Add(debugOverlayWindow);
                            }
                        }
                        else
                        {
                            CloseAllOverlays();
                        }
                    });
            }
        }
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;
using System.Reactive;

namespace OpenGaugeClient.Editor.Services
{
    public class NavigationService
    {
        public static NavigationService Instance { get; } = new();

        private readonly Dictionary<string, Func<object?[], UserControl>> _viewFactories = [];
        public ReactiveCommand<object?, Unit> GoToViewCommand { get; }

        public NavigationService()
        {
            GoToViewCommand = ReactiveCommand.Create<object?>(param =>
            {
                switch (param)
                {
                    case string name:
                        GoToView(name);
                        break;

                    case (string name, object payload):
                        GoToView(name, payload);
                        break;

                    default:
                        throw new Exception("Failed to go to window: need a window name");
                }
            });
        }

        public void Register(string name, Func<object?[], UserControl> factory)
        {
            _viewFactories[name] = factory;
        }

        public void GoToView(string name, params object?[] parameters)
        {
            Console.WriteLine($"[NavigationService] Go to view '{name}' params=[{string.Join(",", parameters)}]");

            if (!_viewFactories.TryGetValue(name, out var factory))
                throw new InvalidOperationException($"View '{name}' not registered");

            var newWindow = factory(parameters);

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                (desktop.MainWindow as MainWindow)!.ShowView(newWindow);
            }
        }
    }
}

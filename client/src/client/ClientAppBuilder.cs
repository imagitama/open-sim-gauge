using System.Text.Json;
using Avalonia;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Themes.Fluent;
using ReactiveUI.Avalonia;
using OpenGaugeClient.Shared;

namespace OpenGaugeClient.Client
{
    public static class ClientAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp(string[] args) =>
            AppBuilder.Configure(() => new ClientApp(args))
                .UsePlatformDetect()
                .With(new Win32PlatformOptions
                {
                    // fix window high DPI scaling issues
                    DpiAwareness = Win32DpiAwareness.Unaware
                })
                .UseReactiveUI()
                .LogToTrace()
                .AfterSetup(_ =>
                    {
                        // make textboxes work
                        Application.Current!.Styles.Add(new FluentTheme()
                        {
                            DensityStyle = DensityStyle.Compact
                        });
                    });
    }
}
using OpenGaugeClient.Client;
using OpenGaugeServer;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using ReactiveUI.Avalonia;
using ReactiveUI;
using System.Reactive;
using System;

public static class Program
{
    public static StreamWriter FileLogger;

    public static async Task Main(string[] args)
    {
        string logPath = Path.Combine(AppContext.BaseDirectory, "combined.log");
        FileLogger = new StreamWriter(logPath, append: true) { AutoFlush = true };
        Console.SetOut(new TeeTextWriter(Console.Out, FileLogger));
        Console.SetError(new TeeTextWriter(Console.Error, FileLogger));

        Console.WriteLine("[Combined] Starting combined server and client...");

        Console.WriteLine("[Combined] Starting server...");

        string[] serverArgs = [];

        _ = Task.Run(async () =>
        {
            try
            {
                await ServerApp.Run(serverArgs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Combined] Failed to start server: {ex}");
            }
        });

        Console.WriteLine("[Combined] Starting client...");

        string[] clientArgs = [];

        ClientAppBuilder.BuildAvaloniaApp(clientArgs).StartWithClassicDesktopLifetime(clientArgs);
    }
}

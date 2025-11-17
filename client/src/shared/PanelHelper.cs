using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OpenGaugeClient.Shared;

namespace OpenGaugeClient
{
    public static class PanelHelper
    {
        public static SolidColorBrush DefaultBackgroundColor = new(Color.FromRgb(25, 25, 25));
        public static int DefaultPanelWidth = 1024;
        public static int DefaultPanelHeight = 768;

        public static Window CreatePanelWindowFromPanel(Panel panel)
        {
            var startupLocation = panel.Position is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.Manual;

            var window = new Window
            {
                Title = panel.Name,
                WindowStartupLocation = startupLocation,
                Topmost = true,
                CanResize = false,
                SystemDecorations = SystemDecorations.None,
                ExtendClientAreaToDecorationsHint = false,
                ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome
            };

            UpdateWindowForPanel(window, panel);

            return window;
        }

        // TODO: move to WindowHelper
        public static void CenterWindow(Window window)
        {
            if (window?.Screens == null)
                return;

            var screen = window.Screens.ScreenFromVisual(window) ?? window.Screens.Primary;

            if (screen == null)
                throw new Exception("Screen is null");

            var workArea = screen.WorkingArea;

            var x = workArea.X + (workArea.Width - window.Width) / 2;
            var y = workArea.Y + (workArea.Height - window.Height) / 2;

            window.Position = new PixelPoint((int)x, (int)y);
        }

        // TODO: move to WindowHelper
        public static void ResetWindow(Window window)
        {
            if (window == null)
                return;

            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.WindowState = WindowState.Normal;
            window.CanResize = true;
            window.Topmost = false;
            window.Content = null;
            window.ExtendClientAreaToDecorationsHint = false;
            window.ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.Default;
            window.TransparencyLevelHint = [WindowTransparencyLevel.None];
            window.Background = DefaultBackgroundColor;

            // do near end
            window.Width = DefaultPanelWidth;
            window.Height = DefaultPanelHeight;

            window.Opened += (_, _) => CenterWindow(window);

            // last
            window.InvalidateVisual();
        }

        public static void UpdateWindowForPanel(Window window, Panel panel)
        {
            var background = new SolidColorBrush(
                (panel.Background?.ToColor()) ?? Color.FromRgb(0, 0, 0)
            );
            window.Background = panel.Transparent == true ? Brushes.Transparent : background;

            if (panel.Transparent == true)
            {
                window.TransparencyLevelHint =
                [
                    WindowTransparencyLevel.Transparent
                ];
            }
            else
            {
                window.TransparencyLevelHint = Array.Empty<WindowTransparencyLevel>();
            }

            var screens = window.Screens.All;
            if (panel.Screen != null)
            {
                if (panel.Screen < 0 || panel.Screen >= screens.Count)
                    throw new Exception($"Screen index {panel.Screen} is invalid (found {screens.Count} screens)");
            }

            var targetScreen = panel.Screen != null ? screens[(int)panel.Screen] : screens[0];
            var bounds = targetScreen.Bounds;

            var width = panel.Width ?? bounds.Width;
            var height = panel.Height ?? bounds.Height;

            window.Width = width;
            window.Height = height;

            if (panel.Position != null)
            {
                window.Position = GetWindowPositionForPanel(panel, window);
            }
            else
            {
                WindowHelper.CenterWindowWithoutFrame(window, panel.Screen);
            }

            if (panel.Fullscreen == true)
            {
                if (window.WindowState != WindowState.FullScreen)
                    window.WindowState = WindowState.FullScreen;
            }
            else
            {
                if (window.WindowState == WindowState.FullScreen)
                    window.WindowState = WindowState.Normal;
            }

            window.Title = panel.Name;
            window.Topmost = panel.OnTop == true;

            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[PanelHelper] Update window for panel '{panel.Name}':\n" +
    $"\tDimensions: {panel.Width}x{panel.Height} => {window.Width}x{window.Height}\n" +
    $"\tOrigin: {panel.Origin}\n" +
    $"\tPosition: {panel.Position} => {window.Position}\n" +
    $"\tOnTop: {window.Topmost}"
);
        }

        public static PixelPoint GetWindowPositionForPanel(Panel panel, Window window)
        {
            if (window == null || window.Screens == null)
                throw new Exception("Window or Screens missing");

            var screens = window.Screens.All;
            int screenIndex = panel.Screen ?? 0;
            if (screenIndex < 0 || screenIndex >= screens.Count)
                screenIndex = 0;

            var screen = screens[screenIndex];

            var scaling = screen.Scaling;

            var screenWidth = screen.Bounds.Width / scaling;
            var screenHeight = screen.Bounds.Height / scaling;

            var (panelX, panelY) = panel.Position.Resolve(screenWidth, screenHeight);

            int windowWidth = (int)window.Width;
            int windowHeight = (int)window.Height;

            var (originX, originY) = panel.Origin.Resolve(windowWidth, windowHeight);

            var dipX = panelX - originX + (screen.Bounds.X / scaling);
            var dipY = panelY - originY + (screen.Bounds.Y / scaling);

            int x = (int)Math.Round(dipX * scaling);
            int y = (int)Math.Round(dipY * scaling);

            return new PixelPoint(x, y);
        }

        public static FlexibleVector2 GetPanelPositionFromWindow(Panel panel, Window window)
        {
            if (window == null || window.Screens == null)
                throw new Exception("Window or Screens missing");

            var screens = window.Screens.All;
            int screenIndex = panel.Screen ?? 0;
            if (screenIndex < 0 || screenIndex >= screens.Count)
                screenIndex = 0;

            var screen = screens[screenIndex];

            var scaling = screen.Scaling;
            double windowDipX = window.Position.X / scaling;
            double windowDipY = window.Position.Y / scaling;

            double screenDipX = screen.Bounds.X / scaling;
            double screenDipY = screen.Bounds.Y / scaling;

            var (originX, originY) = panel.Origin.Resolve(window.Width, window.Height);

            double posX = Math.Round(windowDipX - screenDipX + originX, 2);
            double posY = Math.Round(windowDipY - screenDipY + originY, 2);

            return new FlexibleVector2 { X = posX, Y = posY };
        }

        public static bool GetIsPanelVisible(Panel panel, string? vehicleName)
        {
            return panel.Vehicle == null || vehicleName == null || Utils.GetIsVehicle(panel.Vehicle, vehicleName) || ConfigManager.Config.Debug;
        }
    }
}
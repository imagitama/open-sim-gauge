using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace OpenGaugeClient.Shared
{
    public static class WindowHelper
    {
        public const int DefaultWidth = 1024;
        public const int DefaultHeight = 768;
        public static Color DefaultBackground = Color.FromRgb(25, 25, 25);

        public static void CenterWindowWithoutFrame(Window window, int? screenIndex = null)
        {
            // wait until layout is finalized
            Dispatcher.UIThread.Post(() =>
            {
                if (window.Screens.ScreenFromVisual(window) is { } screen)
                {
                    var oldPos = window.Position;

                    var newPos = GetCenteredWindowPositionWithoutFrame(window, screenIndex);

                    window.Position = newPos;

                    Console.WriteLine($"[WindowHelper] Center window={window} screen={screenIndex} oldPos={oldPos} newPos={newPos}");
                }
            }, DispatcherPriority.Background);
        }

        // TODO: re-use with panel helper
        public static PixelPoint GetCenteredWindowPositionWithoutFrame(Window window, int? screenIndex = null)
        {
            var screens = window.Screens.All;

            if (screens.Count == 0)
                throw new Exception("No screens available");

            var screen = screenIndex == null
                ? window.Screens.Primary
                : screens[Math.Clamp(screenIndex.Value, 0, screens.Count - 1)];

            if (screen == null)
                throw new Exception("Primary screen not available");

            var scaling = screen.Scaling;

            var screenWidthDip = screen.Bounds.Width / scaling;
            var screenHeightDip = screen.Bounds.Height / scaling;

            var (panelX, panelY) = new FlexibleVector2
            {
                X = "50%",
                Y = "50%"
            }.Resolve(screenWidthDip, screenHeightDip);

            double windowWidthDip = window.Width;
            double windowHeightDip = window.Height;

            double originX = windowWidthDip / 2.0;
            double originY = windowHeightDip / 2.0;

            double extraHeight = 0;

            double dipX = panelX - originX;
            double dipY = panelY - originY - extraHeight;

            int pixelX = (int)Math.Round(dipX * scaling);
            int pixelY = (int)Math.Round(dipY * scaling);

            return new PixelPoint(pixelX, pixelY);
        }

        public static int GetMenuBarHeight(Window window)
        {
            // Get the screen containing this window
            var screen = window.Screens.ScreenFromVisual(window);
            if (screen is null)
                return 0;

            var bounds = screen.Bounds;
            var workingArea = screen.WorkingArea;

            // Menu bar height is the top offset between total bounds and working area
            var menuBarHeight = workingArea.Y - bounds.Y;
            return menuBarHeight > 0 ? menuBarHeight : 0;
        }
    }
}

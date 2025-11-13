using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using OpenGaugeClient.Editor.Services;
using OpenGaugeClient.Shared;

namespace OpenGaugeClient.Editor
{
    public partial class DebugOverlayWindow : Window
    {
        private Point _cursorPosition;

        public DebugOverlayWindow(Screen screen)
        {
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
            Background = Brushes.Transparent;
            CanResize = false;
            ShowInTaskbar = false;
            Topmost = true;
            SystemDecorations = SystemDecorations.None;
            ExtendClientAreaToDecorationsHint = true;
            var bounds = screen.Bounds;
            Position = new PixelPoint(bounds.X, bounds.Y);
            var scale = screen.Scaling;
            Width = bounds.Width / scale;
            Height = bounds.Height / scale;
            Position = new PixelPoint(
                (int)(bounds.X / scale),
                (int)(bounds.Y / scale)
            );

            PointerMoved += (_, e) =>
            {
                _cursorPosition = e.GetPosition(this);
                InvalidateVisual();
            };

            PointerPressed += (_, _) =>
            {
                Console.WriteLine("[DebugOverlayWindow] Clicked");
                SettingsService.Instance.OverlayVisible = false;
            };
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            double cellSize = 100;
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 1);

            var menuBarHeight = 0;

            if (OperatingSystem.IsMacOS())
            {
                menuBarHeight = WindowHelper.GetMenuBarHeight(this);
            }

            for (double x = 0; x < Bounds.Width; x += cellSize)
                context.DrawLine(pen, new Point(x, 0), new Point(x, Bounds.Height));
            for (double y = -menuBarHeight; y < Bounds.Height; y += cellSize)
                context.DrawLine(pen, new Point(0, y), new Point(Bounds.Width, y));

            var formatted = new FormattedText(
                $"({_cursorPosition.X:0}, {_cursorPosition.Y + menuBarHeight:0})",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                14,
                Brushes.White
            );
            context.DrawText(formatted, _cursorPosition + new Vector(10, -20));
        }
    }
}
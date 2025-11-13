using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace OpenGaugeClient
{
    public sealed class RenderingHelper : IDisposable
    {
        private readonly Image _imageControl;
        private readonly Func<DrawingContext, Task> _renderFrameAsync;
        private readonly int _fps;
        private readonly Window? _window;
        private RenderTargetBitmap? _target;
        private CancellationTokenSource? _cts;
        private bool _isRunning;

        public RenderingHelper(Image imageControl, Func<DrawingContext, Task> renderFrameAsync, int fps, Window? window)
        {
            _window = window;
            _imageControl = imageControl ?? throw new ArgumentNullException(nameof(imageControl));
            _renderFrameAsync = renderFrameAsync ?? throw new ArgumentNullException(nameof(renderFrameAsync));
            _fps = fps <= 0 ? 30 : fps;
        }

        public void Start()
        {
            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[RenderingHelper] Start isRunning={_isRunning}");

            if (_isRunning)
                return;

            _isRunning = true;
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => RenderLoopAsync(_cts.Token));
        }

        private async Task RenderLoopAsync(CancellationToken token)
        {
            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[RenderingHelper] Start render loop");

            var frameDelay = TimeSpan.FromMilliseconds(1000.0 / _fps);

            while (!token.IsCancellationRequested && GetRenderSize().Width <= 0)
                await Task.Delay(50, token);

            while (!token.IsCancellationRequested)
            {
                var size = GetRenderSize();
                if (size.Width <= 0 || size.Height <= 0)
                {
                    await Task.Delay(50, token);
                    continue;
                }

                // recreate if size changed
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_target == null || _target.PixelSize != size)
                    {
                        if (ConfigManager.Config.Debug)
                            Console.WriteLine($"[RenderingHelper] Regenerate target size={size}");

                        var old = _target;
                        _target = new RenderTargetBitmap(size);
                        _imageControl.Source = null;
                        _imageControl.Source = _target;
                        old?.Dispose();
                    }
                });

                try
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        if (_target == null) return;

                        using (var ctx = _target.CreateDrawingContext())
                            await _renderFrameAsync(ctx);

                        _imageControl.Source = null;
                        _imageControl.Source = _target;
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RenderingHelper] Render error: {ex}");
                }

                try
                {
                    await Task.Delay(frameDelay, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private PixelSize GetRenderSize()
        {
            if (_window != null)
                return new PixelSize(Math.Max(1, (int)_window.ClientSize.Width),
                                     Math.Max(1, (int)_window.ClientSize.Height));

            return new PixelSize(Math.Max(1, (int)_imageControl.Bounds.Width),
                                 Math.Max(1, (int)_imageControl.Bounds.Height));
        }

        public void Dispose()
        {
            if (!_isRunning)
                return;

            _isRunning = false;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                _imageControl.Source = null;
            });
            _target?.Dispose();
            _target = null;
        }

        public static void DrawGrid(DrawingContext ctx, int width, int height, int cellSize)
        {
            var pen = new Pen(new SolidColorBrush(Color.FromRgb(60, 60, 60)), 1);

            for (double x = 0; x <= width; x += cellSize)
                ctx.DrawLine(pen, new Point(x, 0), new Point(x, height));

            for (double y = 0; y <= height; y += cellSize)
                ctx.DrawLine(pen, new Point(0, y), new Point(width, y));
        }
    }
}

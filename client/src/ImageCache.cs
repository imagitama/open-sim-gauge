using SkiaSharp;
using Svg.Skia;
using Avalonia.Media.Imaging;

namespace OpenGaugeClient
{
    public class ImageCache(FontProvider fontProvider) : IDisposable
    {
        private readonly FontProvider _fontProvider = fontProvider;
        private readonly Dictionary<string, Bitmap> _cache = [];
        private bool _disposed;

        public Bitmap Load(string imagePath, Layer layer)
        {
            string absolutePath = PathHelper.GetFilePath(imagePath);

            if (_cache.TryGetValue(absolutePath, out var cached))
                return cached;

            string ext = Path.GetExtension(absolutePath).ToLowerInvariant();

            if (ext == ".svg")
            {
                var svg = new SKSvg();

                if (_fontProvider is { } provider && svg?.Settings?.TypefaceProviders != null)
                    svg.Settings.TypefaceProviders.Insert(0, provider);

                using var fileStream = File.OpenRead(absolutePath);

                if (svg is null)
                    throw new InvalidOperationException("svg is not initialized");

                svg.Load(fileStream);
                var pic = svg.Picture!;

                var (viewBoxWidth, viewBoxHeight) = SvgUtils.GetSvgDimensionsFromViewBox(absolutePath);

                var width = layer.Width != null ? layer.Width : viewBoxWidth > 0 ? (int)viewBoxWidth : 0;
                var height = layer.Height != null ? layer.Height : viewBoxHeight > 0 ? (int)viewBoxHeight : 0;

                if (width == 0 || height == 0)
                    throw new Exception("SVG has no dimensions");

                using var skiaBitmap = new SKBitmap((int)width, (int)height);
                using var skiaCanvas = new SKCanvas(skiaBitmap);
                skiaCanvas.Clear(SKColors.Transparent);

                var picBounds = pic.CullRect;
                float scaleX = (float)(width / picBounds.Width);
                float scaleY = (float)(height / picBounds.Height);

                skiaCanvas.Scale(scaleX, scaleY);
                skiaCanvas.DrawPicture(pic);
                skiaCanvas.Flush();

                using var image = SKImage.FromBitmap(skiaBitmap);

                if (image == null)
                    throw new Exception($"Failed to load image '{absolutePath}'");

                using var png = image.Encode(SKEncodedImageFormat.Png, 100);
                using var pngStream = png.AsStream();
                var avaloniaBmp = new Bitmap(pngStream);

                if (layer.Debug)
                    Console.WriteLine($"[ImageCache] Loaded SVG '{imagePath}' {picBounds.Width}x{picBounds.Height} => {width}x{height}");

                _cache[absolutePath] = avaloniaBmp;

                return avaloniaBmp;
            }
            else
            {
                using var s = File.OpenRead(absolutePath);
                var bmp = new Bitmap(s);

                if (layer.Debug)
                    Console.WriteLine($"[ImageCache] Loaded PNG '{absolutePath}'");

                _cache[absolutePath] = bmp;

                return bmp;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var bmp in _cache.Values)
            {
                bmp.Dispose();
            }

            _cache.Clear();
            _disposed = true;

            GC.SuppressFinalize(this);
        }
    }
}
using SkiaSharp;
using Svg.Skia;
using Avalonia.Media.Imaging;

namespace OpenGaugeClient
{
    public class ImageCache(SKFontProvider skFontProvider) : IDisposable
    {
        private readonly SKFontProvider _skFontProvider = skFontProvider;
        private readonly Dictionary<(string, double?, double?), Bitmap> _cache = [];
        private bool _disposed;

        public Bitmap Load(string imagePath, double? imageWidth, double? imageHeight, double renderScaling)
        {
            string absolutePath = PathHelper.GetFilePath(imagePath);

            var key = (absolutePath, imageWidth, imageHeight);

            if (_cache.TryGetValue(key, out var cached))
                return cached;

            string ext = Path.GetExtension(absolutePath).ToLowerInvariant();

            if (ext == ".svg")
            {
                var svg = new SKSvg();

                if (_skFontProvider is { } provider && svg?.Settings?.TypefaceProviders != null)
                    svg.Settings.TypefaceProviders.Insert(0, provider);

                using var fileStream = File.OpenRead(absolutePath);

                if (svg is null)
                    throw new InvalidOperationException("svg is not initialized");

                svg.Load(fileStream);
                var pic = svg.Picture!;

                var (viewBoxWidth, viewBoxHeight) = SvgUtils.GetSvgDimensionsFromFileViewBox(absolutePath);

                int dipWidth = (int)(imageWidth != null ? imageWidth : viewBoxWidth > 0 ? (int)viewBoxWidth : 0);
                int dipHeight = (int)(imageHeight != null ? imageHeight : viewBoxHeight > 0 ? (int)viewBoxHeight : 0);

                if (dipWidth == 0 || dipHeight == 0)
                    throw new Exception("SVG has no dimensions");

                int pxWidth = (int)Math.Ceiling(dipWidth * renderScaling);
                int pxHeight = (int)Math.Ceiling(dipHeight * renderScaling);

                using var skiaBitmap = new SKBitmap(pxWidth, pxHeight);
                using var skiaCanvas = new SKCanvas(skiaBitmap);
                skiaCanvas.Clear(SKColors.Transparent);

                var picBounds = pic.CullRect;
                float scaleX = (float)(pxWidth / picBounds.Width);
                float scaleY = (float)(pxHeight / picBounds.Height);

                skiaCanvas.Scale(scaleX, scaleY);
                skiaCanvas.DrawPicture(pic);
                skiaCanvas.Flush();

                using var image = SKImage.FromBitmap(skiaBitmap);

                if (image == null)
                    throw new Exception($"Failed to load image '{absolutePath}'");

                using var png = image.Encode(SKEncodedImageFormat.Png, 100);
                using var pngStream = png.AsStream();
                var avaloniaBmp = new Bitmap(pngStream);

                if (ConfigManager.Config.Debug)
                    Console.WriteLine($"[ImageCache] Loaded SVG '{imagePath}' {picBounds.Width}x{picBounds.Height} => {dipWidth}x{dipHeight} => {pxWidth}x{pxHeight}");

                _cache[key] = avaloniaBmp;

                return avaloniaBmp;
            }
            else
            {
                using var s = File.OpenRead(absolutePath);
                var bmp = new Bitmap(s);

                if (ConfigManager.Config.Debug)
                    Console.WriteLine($"[ImageCache] Loaded PNG '{absolutePath}'");

                _cache[key] = bmp;

                return bmp;
            }
        }

        public Bitmap LoadFromSvgText(string svgText, double? imageWidth, double? imageHeight, double renderScaling)
        {
            if (string.IsNullOrWhiteSpace(svgText))
                throw new ArgumentException("SVG text is empty");

            string hash = svgText.GetHashCode().ToString();
            var key = ($"__inline_svg_{hash}", imageWidth, imageHeight);

            if (_cache.TryGetValue(key, out var cached))
                return cached;

            var svg = new SKSvg();

            if (_skFontProvider is { } provider && svg?.Settings?.TypefaceProviders != null)
                svg.Settings.TypefaceProviders.Insert(0, provider);

            using (var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgText)))
            {
                svg!.Load(memoryStream);
            }

            var pic = svg.Picture ?? throw new Exception("Failed to parse inline SVG");

            var (viewBoxWidth, viewBoxHeight) = SvgUtils.GetSvgDimensionsFromViewBox(svgText);

            int dipWidth = (int)(imageWidth != null ? imageWidth : viewBoxWidth > 0 ? (int)viewBoxWidth : 0);
            int dipHeight = (int)(imageHeight != null ? imageHeight : viewBoxHeight > 0 ? (int)viewBoxHeight : 0);

            if (dipWidth == 0 || dipHeight == 0)
                throw new Exception("Inline SVG has no dimensions (width/height/viewBox missing)");

            int pxWidth = (int)Math.Ceiling(dipWidth * renderScaling);
            int pxHeight = (int)Math.Ceiling(dipHeight * renderScaling);

            using var skiaBitmap = new SKBitmap(pxWidth, pxHeight);
            using var skiaCanvas = new SKCanvas(skiaBitmap);
            skiaCanvas.Clear(SKColors.Transparent);

            var picBounds = pic.CullRect;
            float scaleX = (float)(pxWidth / picBounds.Width);
            float scaleY = (float)(pxHeight / picBounds.Height);

            skiaCanvas.Scale(scaleX, scaleY);
            skiaCanvas.DrawPicture(pic);
            skiaCanvas.Flush();

            using var image = SKImage.FromBitmap(skiaBitmap);
            using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            using var pngStream = png.AsStream();
            var avaloniaBmp = new Bitmap(pngStream);

            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[ImageCache] Loaded Inline SVG len={svgText.Length} => {dipWidth}x{dipHeight} => {pxWidth}x{pxHeight}");

            _cache[key] = avaloniaBmp;
            return avaloniaBmp;
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
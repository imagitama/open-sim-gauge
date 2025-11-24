using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SkiaSharp;
using Svg;
using System.Globalization;

namespace OpenGaugeClient.Editor
{
    public class SvgCreatorRenderer
    {
        private SvgCreator _svgCreator;
        private FlexibleVector2 _position;
        private int _canvasWidth;
        private int _canvasHeight;
        private double _renderScaling;
        private ImageCache _imageCache;
        private FontProvider _fontProvider;
        private SvgCache _svgCache;
        private bool _debug;
        private int _debugOutputCount = 0;

        public class BuiltLayer
        {
            public required string SvgText;
        }

        private Dictionary<SvgLayer, BuiltLayer?> builtLayers = [];

        public SvgCreatorRenderer(SvgCreator svgCreator, FlexibleVector2 position, int canvasWidth, int canvasHeight, double renderScaling, ImageCache imageCache, FontProvider fontProvider, SvgCache svgCache, bool debug = false)
        {
            _svgCreator = svgCreator;
            _position = position;
            _canvasWidth = canvasWidth;
            _canvasHeight = canvasHeight;
            _renderScaling = renderScaling;
            _imageCache = imageCache;
            _fontProvider = fontProvider;
            _svgCache = svgCache;
            _debug = debug;

            _ = BuildSvgs();
        }

        private async Task BuildSvgs()
        {
            try
            {
                foreach (var layer in _svgCreator.Layers)
                {
                    var ops = layer.Operations;

                    if (ops.Count == 0)
                    {
                        builtLayers[layer] = null;
                        continue;
                    }

                    Console.WriteLine($"[SvgCreatorRenderer] Build {ops.Count} operations at {_svgCreator.Width}x{_svgCreator.Height} shadow={layer.Shadow?.ToConfig()} orig={layer.Shadow}");

                    var svgText = await SvgBuilder.Build([.. ops], _svgCreator.Width, _svgCreator.Height, layer.Shadow?.ToConfig());

                    // Console.WriteLine($"[SvgCreatorRenderer] Built:\n{svgText}");

                    builtLayers[layer] =
                        new BuiltLayer()
                        {
                            SvgText = svgText.ToString()!
                        };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SvgCreatorRenderer] Failed to build SVGs: {ex}");
            }
        }

        private sealed class SvgOperationRenderData
        {
            public required SvgOperation SvgOperation { get; set; }
            public double SvgOperationWidth { get; set; }
            public double SvgOperationHeight { get; set; }
            public double SvgOperationPosX { get; set; }
            public double SvgOperationPosY { get; set; }
            public Matrix FinalTransform { get; set; }
            public string DebugText { get; set; } = "";
        }

        public void DrawSvgLayers(DrawingContext ctx, List<double?> layerForceRotations)
        {
            var scale = 1;
            var halfW = _canvasWidth / 2.0;
            var halfH = _canvasHeight / 2.0;

            var centerTransform = Matrix.CreateTranslation(halfW, halfH);
            var scaleTransform = Matrix.CreateScale(scale, scale);

            var offsetX = _svgCreator.Width / 2;
            var offsetY = _svgCreator.Height / 2;
            var offsetTransform = Matrix.CreateTranslation(-offsetX, -offsetY);

            var svgCreatorTransform =
                offsetTransform *
                scaleTransform *
                centerTransform;

            var svgOperationRenderDatas = new List<SvgOperationRenderData>();

            _debugOutputCount = 0;

            var svgWidth = _svgCreator.Width;
            var svgHeight = _svgCreator.Height;

            using (ctx.PushTransform(svgCreatorTransform))
            {
                var layersToRender = _svgCreator.Layers.ToArray().Reverse().ToArray();

                for (var i = 0; i < layersToRender.Length; i++)
                {
                    var layer = layersToRender[i];
                    var builtLayer = builtLayers[layer];

                    if (builtLayer == null)
                        continue;

                    var svgText = builtLayer.SvgText;

                    var svgBitmap = _imageCache.LoadFromSvgText(svgText, svgWidth, svgHeight, _renderScaling);

                    double? forceRotate = layerForceRotations[layerForceRotations.Count - 1 - i];

                    IDisposable? layerTransform = null;

                    if (forceRotate != null)
                    {
                        double angleRadians = (double)(Math.PI * forceRotate / 180.0);
                        var rotateCenter = Matrix.CreateTranslation(svgWidth / 2, svgHeight / 2);
                        var rotate = Matrix.CreateRotation(angleRadians);
                        var uncenter = Matrix.CreateTranslation(-svgWidth / 2, -svgHeight / 2);
                        var rotationMatrix = uncenter * rotate * rotateCenter;
                        layerTransform = ctx.PushTransform(rotationMatrix);
                    }

                    using (layerTransform)
                    {
                        var outRect = new Rect(0, 0, svgWidth, svgHeight);
                        ctx.DrawImage(svgBitmap, outRect);
                    }

                    foreach (var svgOperationRenderData in svgOperationRenderDatas)
                    {
                        using (ctx.PushTransform(svgOperationRenderData.FinalTransform))
                        {
                            const int crossSize = 10;
                            ctx.DrawLine(new Pen(Brushes.LightBlue, 4), new Point(-crossSize, 0), new Point(crossSize, 0));
                            ctx.DrawLine(new Pen(Brushes.LightBlue, 4), new Point(0, -crossSize), new Point(0, crossSize));

                            var strings = new List<string>();

                            // if (svgOperationRenderData.RotationAngle != 0)
                            //     strings.Add($"rot={Math.Truncate(svgOperationRenderData.RotationAngle)}°");

                            // if (svgOperationRenderData.OffsetX != 0)
                            //     strings.Add($"x={Math.Truncate(svgOperationRenderData.OffsetX)}px");

                            // if (svgOperationRenderData.OffsetY != 0)
                            //     strings.Add($"y={Math.Truncate(svgOperationRenderData.OffsetY)}px");

                            // if (svgOperationRenderData.PathPosition != null)
                            //     strings.Add($"path={Math.Truncate(svgOperationRenderData.PathPosition.Value.X)},{Math.Truncate(svgOperationRenderData.PathPosition.Value.Y)}");

                            DrawDebugText(ctx, string.Join("\n", strings), Brushes.LightBlue, new Point(2, 0), 1);

                            ctx.DrawRectangle(null, new Pen(Brushes.LightBlue, 1), new Rect(0, 0, svgOperationRenderData.SvgOperationWidth, svgOperationRenderData.SvgOperationHeight));
                        }
                    }
                }
            }
        }

        private void DrawSvgCreatorDebug(DrawingContext ctx, double x, double y, double scale)
        {
            var svgCreatorRect = new Rect(0, 0, _svgCreator.Width, _svgCreator.Height);

            var pen = new Pen(Brushes.Pink, 1)
            {
                DashStyle = new DashStyle([6, 4], 0)
            };
            ctx.DrawRectangle(pen, svgCreatorRect);

            var canvasWidth = _svgCreator.Width;
            var canvasHeight = _svgCreator.Height;

            var formattedText = new FormattedText(
                // $"'{_svgCreator.Name}'\n" +
                // $"{_svgCreatorRef.Position.X},{_svgCreatorRef.Position.Y} => {x},{y}\n" +
                $"{_svgCreator.Width}x{_svgCreator.Height} => {_svgCreator.Width * scale}x{_svgCreator.Height * scale}",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                16,
                Brushes.White
            );

            var textX = canvasWidth - formattedText.Width - 5;
            var textY = canvasHeight + 5;

            ctx.DrawText(formattedText, new Point(textX, textY));

            // if (_svgCreator.Grid != null && _svgCreator.Grid > 0)
            //     RenderingHelper.DrawGrid(ctx, _svgCreator.Width, _svgCreator.Height, (double)_svgCreator.Grid);
        }

        private void DrawDebugText(DrawingContext ctx, string text, IBrush brush, Point? pos = null, double scaleText = 1)
        {
            var p = pos ?? new Point(0, 0);

            var formattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                12 * scaleText,
                brush
            );

            ctx.DrawText(formattedText, new Point(p.X + 2, p.Y + 2 + (_debugOutputCount * formattedText.Height)));

            _debugOutputCount++;
        }
    }
}
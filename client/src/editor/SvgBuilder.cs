using System.Xml.Linq;
using Splat.ModeDetection;

namespace OpenGaugeClient.Editor
{
    public class SvgBuilder
    {
        public static readonly XNamespace Ns = "http://www.w3.org/2000/svg";

        public static async Task BuildAndOutput(SvgOperation[] operations, string outputSvgPath, double svgWidth, double svgHeight, ShadowConfig? shadow = null)
        {
            var svgText = await Build(operations, svgWidth, svgHeight, shadow);
            await WriteSvgToFile(svgText, outputSvgPath, "output");
        }

        public static async Task<XElement> Build(SvgOperation[] operations, double svgWidth, double svgHeight, ShadowConfig? shadow = null)
        {
            var nodes = new List<XElement>();

            foreach (var op in operations)
            {
                if (op == null) continue;

                var (posX, posY) = op.Position.Resolve(svgWidth, svgHeight);

                var opWidth = op.Width != null ? op.Width.Resolve(svgWidth) : svgWidth;
                var opHeight = op.Height != null ? op.Height.Resolve(svgHeight) : svgHeight;

                var (originX, originY) = op.Origin.Resolve(opWidth, opHeight);

                XElement node;

                switch (op)
                {
                    case CircleSvgOperation circle:
                        node = CreateCircleNode(
                            circle.Radius,
                            circle.Fill ?? "transparent",
                            circle.StrokeWidth,
                            circle.StrokeFill
                        );
                        break;

                    case ArcSvgOperation arc:
                        node = CreateArcNode(
                            arc.Radius,
                            arc.DegreesStart,
                            arc.DegreesEnd,
                            arc.InnerThickness,
                            arc.Fill ?? "transparent"
                        );
                        break;

                    case GaugeTicksSvgOperation gaugeTicks:
                        node = CreateGaugeTicksNode(
                            gaugeTicks.Radius,
                            gaugeTicks.DegreesStart,
                            gaugeTicks.DegreesEnd,
                            gaugeTicks.DegreesGap,
                            gaugeTicks.TickLength,
                            gaugeTicks.TickWidth,
                            gaugeTicks.TickFill
                        );
                        break;

                    case GaugeTickLabelsSvgOperation gaugeTickLabels:
                        node = CreateGaugeTickLabelsNode(
                            gaugeTickLabels.Radius,
                            gaugeTickLabels.DegreesStart,
                            gaugeTickLabels.DegreesEnd,
                            gaugeTickLabels.DegreesGap,
                            gaugeTickLabels.Labels,
                            gaugeTickLabels.LabelFill,
                            gaugeTickLabels.LabelSize,
                            gaugeTickLabels.LabelFont
                        );
                        break;

                    case TextSvgOperation text:
                        node = CreateTextNode(
                            text.Text,
                            text.Size,
                            text.Fill,
                            text.Font
                        );
                        break;

                    case SquareSvgOperation square:
                        node = CreateSquareNode(
                            opWidth,
                            opHeight,
                            square.Fill,
                            square.Round,
                            square.StrokeWidth
                        );
                        break;

                    case TriangleSvgOperation triangle:
                        node = CreateTriangleNode(
                            opWidth,
                            opHeight,
                            triangle.Fill,
                            triangle.StrokeWidth
                        );
                        break;

                    default:
                        throw new Exception($"Unknown operation type: {op.Type}");
                }

                var wrapped = ApplyRotation(node, op.Type, op.Rotate ?? 0, posX, posY, originX, originY, opWidth, opHeight);

                if (op.Name == "square")
                    Console.WriteLine($"DRAW pos={posX},{posY} origin={originX},{originY} size={opWidth}x{opHeight} bounds={svgWidth},{svgHeight}");

                nodes.Add(wrapped);
            }

            return ConvertNodesIntoSvg(nodes, svgWidth, svgHeight, shadow);
        }

        private static XElement ApplyRotation(
            XElement node,
            SvgOperationType type,
            double angle,
            double posX,
            double posY,
            double originX,
            double originY,
            double opWidth,
            double opHeight)
        {
            var localOriginX = originX - opWidth / 2.0;
            var localOriginY = originY - opHeight / 2.0;

            var translateX = posX - localOriginX;
            var translateY = posY - localOriginY;

            var transform =
                $"translate({translateX},{translateY}) " +
                $"translate({localOriginX},{localOriginY}) " +
                $"rotate({angle}) " +
                $"translate({-localOriginX},{-localOriginY})";

            var g = new XElement(Ns + "g",
                new XAttribute("transform", transform)
            );

            g.Add(node);
            return g;
        }

        private static double DegToRad(double deg) => (deg - 90) * Math.PI / 180.0;

        private static (double x, double y) PolarToCartesian(double cx, double cy, double radius, double angleDeg)
        {
            var a = DegToRad(angleDeg);
            return (cx + radius * Math.Cos(a),
                    cy + radius * Math.Sin(a));
        }

        private static XElement CreateCircleNode(
            double radius,
            string fill,
            double? strokeWidth,
            string? strokeFill)
        {
            return new XElement(Ns + "circle",
                new XAttribute("cx", 0),
                new XAttribute("cy", 0),
                new XAttribute("r", radius),
                new XAttribute("fill", fill),
                strokeWidth != null ? new XAttribute("stroke-width", strokeWidth) : null,
                strokeFill != null ? new XAttribute("stroke", strokeFill) : null
            );
        }

        private static XElement CreateArcNode(
            double radius,
            double degreesStart,
            double degreesEnd,
            double innerThickness,
            string fill)
        {
            double cx = 0;
            double cy = 0;

            var (sx, sy) = PolarToCartesian(cx, cy, radius, degreesStart);
            var (ex, ey) = PolarToCartesian(cx, cy, radius, degreesEnd);

            string largeArc = Math.Abs(degreesEnd - degreesStart) % 360 > 180 ? "1" : "0";
            double innerR = radius - innerThickness;

            var (exi, eyi) = PolarToCartesian(cx, cy, innerR, degreesEnd);
            var (sxi, syi) = PolarToCartesian(cx, cy, innerR, degreesStart);

            string d =
                $"M {sx},{sy} " +
                $"A {radius},{radius} 0 {largeArc} 1 {ex},{ey} " +
                $"L {exi},{eyi} " +
                $"A {innerR},{innerR} 0 {largeArc} 0 {sxi},{syi} Z";

            return new XElement(Ns + "path",
                new XAttribute("d", d),
                new XAttribute("fill", fill)
            );
        }

        private static XElement CreateGaugeTicksNode(
            double radius,
            double startDeg,
            double endDeg,
            double gapDeg,
            double tickLength,
            double tickWidth,
            string tickFill)
        {
            var group = new XElement(Ns + "g",
                new XAttribute("stroke", tickFill),
                new XAttribute("fill", "none")
            );

            double cx = 0;
            double cy = 0;

            int direction = endDeg >= startDeg ? 1 : -1;
            double angle = startDeg;

            while ((direction == 1 && angle <= endDeg) ||
                   (direction == -1 && angle >= endDeg))
            {
                var (x1, y1) = PolarToCartesian(cx, cy, radius - tickLength, angle);
                var (x2, y2) = PolarToCartesian(cx, cy, radius, angle);

                group.Add(new XElement(Ns + "line",
                    new XAttribute("x1", x1),
                    new XAttribute("y1", y1),
                    new XAttribute("x2", x2),
                    new XAttribute("y2", y2),
                    new XAttribute("stroke-width", tickWidth)
                ));

                angle += direction * gapDeg;
            }

            return group;
        }

        private static XElement CreateGaugeTickLabelsNode(
            double radius, double startDeg, double endDeg, double gapDeg,
            IList<string> labels, string fill, double size, string font)
        {
            var group = new XElement(Ns + "g",
                new XAttribute("font-family", font),
                new XAttribute("fill", fill),
                new XAttribute("text-anchor", "middle"),
                new XAttribute("dominant-baseline", "middle")
            );

            double cx = 0;
            double cy = 0;

            var angles = new List<double>();
            double totalLabels = labels.Count;
            double totalAngle = Math.Abs(endDeg - startDeg);

            if (gapDeg > 0)
            {
                for (int i = 0; i < totalLabels; i++)
                    angles.Add(startDeg + i * gapDeg);
            }
            else
            {
                double actual = totalLabels > 1 ? totalAngle / (totalLabels - 1) : 0;
                for (int i = 0; i < totalLabels; i++)
                    angles.Add(startDeg + i * actual);
            }

            for (int i = 0; i < labels.Count; i++)
            {
                var angle = angles[i];
                var (x, y) = PolarToCartesian(cx, cy, radius, angle);

                group.Add(new XElement(Ns + "text",
                    new XAttribute("x", x),
                    new XAttribute("y", y),
                    new XAttribute("font-size", size),
                    new XAttribute("text-anchor", "middle"),
                    new XAttribute("dominant-baseline", "middle"),
                    new XAttribute("dy", "0.35em")
                )
                { Value = labels[i] });
            }

            return group;
        }

        private static XElement CreateTextNode(
            string text, double size, string fill, string font)
        {
            return new XElement(Ns + "text",
                new XAttribute("x", 0),
                new XAttribute("y", 0),
                new XAttribute("fill", fill),
                new XAttribute("font-family", font),
                new XAttribute("font-size", size),
                new XAttribute("text-anchor", "middle"),
                new XAttribute("dominant-baseline", "middle"),
                new XAttribute("dy", "0.35em"),
                text
            );
        }

        private static XElement CreateSquareNode(
            double w,
            double h,
            string fill,
            double? round,
            double? strokeWidth)
        {
            var node = new XElement(Ns + "rect",
                new XAttribute("x", -w / 2),
                new XAttribute("y", -h / 2),
                new XAttribute("width", w),
                new XAttribute("height", h),
                new XAttribute("fill", fill)
            );

            if (round != null) node.Add(new XAttribute("rx", round));
            if (strokeWidth != null) node.Add(new XAttribute("stroke-width", strokeWidth));

            return node;
        }

        private static XElement CreateTriangleNode(
            double w,
            double h,
            string fill,
            double? strokeWidth)
        {
            double halfW = w / 2;
            double hh = h;

            string points =
                $"0,{-hh / 2} {-halfW},{hh / 2} {halfW},{hh / 2}";

            var node = new XElement(Ns + "polygon",
                new XAttribute("points", points),
                new XAttribute("fill", fill)
            );

            if (strokeWidth != null)
                node.Add(new XAttribute("stroke-width", strokeWidth));

            return node;
        }

        private static XElement ConvertNodesIntoSvg(List<XElement> nodes, double width, double height, ShadowConfig? shadow = null)
        {
            var svg = new XElement(Ns + "svg",
                new XAttribute("width", width),
                new XAttribute("height", height),
                new XAttribute("viewBox", $"0 0 {width} {height}"),
                new XAttribute("style", "background:none")
            );

            if (shadow != null)
            {
                WrapInShadow(svg, nodes, shadow);
            }
            else
            {
                foreach (var n in nodes)
                    svg.Add(n);
            }

            return svg;
        }

        private static void WrapInShadow(XElement svg, List<XElement> nodes, ShadowConfig shadow)
        {
            int size = 4;
            int dx = 3;
            int dy = 3;

            if (shadow is IDictionary<string, object> dict)
            {
                if (dict.TryGetValue("size", out var sizeObj) && sizeObj is IConvertible)
                    size = Convert.ToInt32(sizeObj);

                if (dict.TryGetValue("x", out var xObj) && xObj is IConvertible)
                    dx = Convert.ToInt32(xObj);

                if (dict.TryGetValue("y", out var yObj) && yObj is IConvertible)
                    dy = Convert.ToInt32(yObj);
            }

            var defs = new XElement(Ns + "defs");
            svg.Add(defs);

            var filter = new XElement(Ns + "filter",
                new XAttribute("id", "shadow"),
                new XAttribute("x", "-20%"),
                new XAttribute("y", "-20%"),
                new XAttribute("width", "140%"),
                new XAttribute("height", "140%")
            );
            defs.Add(filter);

            filter.Add(new XElement(Ns + "feGaussianBlur",
                new XAttribute("in", "SourceAlpha"),
                new XAttribute("stdDeviation", size),
                new XAttribute("result", "blur")
            ));

            filter.Add(new XElement(Ns + "feOffset",
                new XAttribute("in", "blur"),
                new XAttribute("dx", dx),
                new XAttribute("dy", dy),
                new XAttribute("result", "offset")
            ));

            filter.Add(new XElement(Ns + "feFlood",
                new XAttribute("flood-color", "rgba(0,0,0,0.5)"),
                new XAttribute("result", "color")
            ));

            filter.Add(new XElement(Ns + "feComposite",
                new XAttribute("in", "color"),
                new XAttribute("in2", "offset"),
                new XAttribute("operator", "in"),
                new XAttribute("result", "shadow")
            ));

            var merge = new XElement(Ns + "feMerge");
            merge.Add(new XElement(Ns + "feMergeNode", new XAttribute("in", "shadow")));
            merge.Add(new XElement(Ns + "feMergeNode", new XAttribute("in", "SourceGraphic")));
            filter.Add(merge);

            var group = new XElement(Ns + "g",
                new XAttribute("filter", "url(#shadow)")
            );

            group.Add(nodes);
            svg.Add(group);
        }

        private static async Task WriteSvgToFile(XElement svg, string path, string name)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                svg
            );

            await using var fileStream = File.Create(path);
            await doc.SaveAsync(fileStream, SaveOptions.None, CancellationToken.None);
        }
    }
}

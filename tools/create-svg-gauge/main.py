#!/usr/bin/env python3
import json
import math
import os
import sys
from xml.dom import minidom
from xml.etree.ElementTree import Element, SubElement, ElementTree, tostring

def debug(msg):
    print(f"[DEBUG] {msg}")


def deg_to_rad(deg):
    return math.radians(deg - 90) # 0deg = north


def polar_to_cartesian(cx, cy, radius, angle_degrees):
    angle_rad = deg_to_rad(angle_degrees)
    x = cx + radius * math.cos(angle_rad)
    y = cy + radius * math.sin(angle_rad)
    return x, y


def coord_to_str(v):
    if isinstance(v, (int, float)):
        return str(v)
    elif isinstance(v, str):
        return v.strip()
    else:
        raise TypeError(f"Invalid coordinate type for text node: {v!r}")


def CreateCircleNode(x, y, radius, fill=None, strokeWidth=None, strokeFill=None):
    cx = coord_to_str(x)
    cy = coord_to_str(y)

    debug(f"CreateCircleNode: cx={cx} cy={cy} radius={radius} fill={fill} strokeWidth={strokeWidth} strokeFill={strokeFill}")

    attrs = {
        "cx": cx,
        "cy": cy,
        "r": str(radius),
        "fill": fill if fill is not None else "transparent",
        "stroke-width": str(strokeWidth) if strokeWidth is not None else None,
        "stroke": strokeFill if strokeFill is not None else None
    }

    attrs = {k: v for k, v in attrs.items() if v is not None}

    node = Element("circle", attrs)
    return node


def CreateArcNode(position, radius, degreesStart, degreesEnd, innerThickness, fill):
    cx, cy = position

    debug(f"CreateArcNode: pos=({cx:.1f},{cy:.1f}) radius={radius} start={degreesStart} end={degreesEnd} thickness={innerThickness} fill={fill}")

    start_x, start_y = polar_to_cartesian(cx, cy, radius, degreesStart)
    end_x, end_y = polar_to_cartesian(cx, cy, radius, degreesEnd)

    large_arc_flag = "1" if abs(degreesEnd - degreesStart) % 360 > 180 else "0"
    inner_r = radius - innerThickness

    debug(f" → Outer arc start=({start_x:.1f},{start_y:.1f}) end=({end_x:.1f},{end_y:.1f}) largeArc={large_arc_flag}")
    debug(f" → Inner radius={inner_r}")

    path_data = [
        f"M {start_x},{start_y}",
        f"A {radius},{radius} 0 {large_arc_flag} 1 {end_x},{end_y}",
    ]

    end_inner_x, end_inner_y = polar_to_cartesian(cx, cy, inner_r, degreesEnd)
    start_inner_x, start_inner_y = polar_to_cartesian(cx, cy, inner_r, degreesStart)
    path_data += [
        f"L {end_inner_x},{end_inner_y}",
        f"A {inner_r},{inner_r} 0 {large_arc_flag} 0 {start_inner_x},{start_inner_y}",
        "Z"
    ]

    return Element("path", {"d": " ".join(path_data), "fill": fill})


def CreateGaugeTicksNode(position, radius, degreesStart, degreesEnd, degreesGap,
                         tickLength, tickWidth, tickFill):
    cx, cy = position

    group = Element("g", {"stroke": tickFill, "fill": "none"})

    direction = 1 if degreesEnd > degreesStart else -1
    angle = degreesStart
    tick_index = 0

    debug(
        f"CreateGaugeTicksNode: pos=({cx:.1f},{cy:.1f}) radius={radius} "
        f"start={degreesStart} end={degreesEnd} gap={degreesGap} "
        f"tickLen={tickLength} tickWidth={tickWidth} color={tickFill} "
    )

    while (direction == 1 and angle <= degreesEnd) or (direction == -1 and angle >= degreesEnd):
        x1, y1 = polar_to_cartesian(cx, cy, radius - tickLength, angle)
        x2, y2 = polar_to_cartesian(cx, cy, radius, angle)

        debug(
            f"  tick {tick_index:02d}: angle={angle:.2f} len={tickLength:.1f} "
            f"width={tickWidth:.1f} ({x1:.1f},{y1:.1f})→({x2:.1f},{y2:.1f})"
        )

        line_node = Element("line", {
            "x1": str(x1),
            "y1": str(y1),
            "x2": str(x2),
            "y2": str(y2),
            "stroke-width": str(tickWidth)
        })

        group.append(line_node)

        angle += direction * degreesGap
        tick_index += 1

    debug(f" Total ticks created: {tick_index}")
    return group



def CreateGaugeTickLabelsNode(
    position, radius, degreesStart, degreesEnd, degreesGap, labels,
    labelFill="rgb(255,255,255)", labelSize=24, labelFont="Arial"
):
    cx, cy = position

    try:
        labelSize = float(labelSize)
    except Exception:
        raise TypeError(f"labelSize must be a number got {labelSize!r}")

    debug(
        f"CreateGaugeTickLabelsNode: pos=({cx:.1f},{cy:.1f}) radius={radius} "
        f"start={degreesStart} end={degreesEnd} gap={degreesGap} labels={labels} "
        f"labelFill={labelFill} labelSize={labelSize} labelFont={labelFont}"
    )

    group = Element("g", {
        "font-family": labelFont,
        "fill": labelFill,
        "text-anchor": "middle",
        "dominant-baseline": "middle"
    })

    total_labels = len(labels)
    total_angle = abs(degreesEnd - degreesStart)

    if degreesGap and float(degreesGap) > 0:
        angles = [degreesStart + i * float(degreesGap) for i in range(total_labels)]
    else:
        actual_gap = total_angle / (total_labels - 1) if total_labels > 1 else 0
        angles = [degreesStart + i * actual_gap for i in range(total_labels)]

    for i, label in enumerate(labels):
        angle = angles[i] if i < len(angles) else degreesEnd
        x, y = polar_to_cartesian(cx, cy, radius, angle)
        debug(f"  label {i:02d}: '{label}' angle={angle:.2f} pos=({x:.1f},{y:.1f})")

        SubElement(group, "text", {
            "x": str(x),
            "y": str(y),
            "font-size": str(labelSize),
            "text-anchor": "middle",
            "dominant-baseline": "middle",
            "dy": "0.35em" # compensate for text rendering issues
        }).text = str(label)

    return group


def CreateTriangleNode(x, y, width, height, rotation=0, fill="transparent", strokeWidth=None, svgWidth=None, svgHeight=None):
    def parse_coord(value, total=None):
        if isinstance(value, (int, float)):
            return float(value)
        if isinstance(value, str):
            v = value.strip()
            if v.endswith("%") and total is not None:
                return float(v[:-1]) / 100 * total
            try:
                return float(v)
            except ValueError:
                raise TypeError(f"Invalid coordinate: {value!r}")
        raise TypeError(f"Invalid coordinate type: {value!r}")

    if svgWidth is None or svgHeight is None:
        raise ValueError("CreateTriangleNode requires svgWidth and svgHeight for percentage positioning")

    cx = parse_coord(x, svgWidth)
    cy = parse_coord(y, svgHeight)

    half_w = float(width) / 2
    h = float(height)
    points = [
        (0, -h / 2),       # top
        (-half_w, h / 2),  # bottom left
        (half_w, h / 2)    # bottom right
    ]
    points_str = " ".join(f"{px},{py}" for px, py in points)

    debug(f"CreateTriangleNode: x={x} y={y} (abs=({cx:.1f},{cy:.1f})) width={width} height={height} rotation={rotation} fill={fill}")

    transform_parts = [f"translate({cx},{cy})"]
    if rotation != 0:
        transform_parts.append(f"rotate({rotation})")
    transform_str = " ".join(transform_parts)

    node = Element("polygon", {
        "points": points_str,
        "fill": fill,
        "transform": transform_str
    })

    if strokeWidth:
        node.set("stroke-width", str(strokeWidth))

    return node


def CreateSquareNode(x, y, width, height, fill="transparent", roundAmount=None, strokeWidth=None):
    cx = coord_to_str(x)
    cy = coord_to_str(y)

    debug(f"CreateSquareNode: center=({cx},{cy}) width={width} height={height} round={roundAmount} fill={fill} strokeWidth={strokeWidth}")

    node = Element("rect", {
        "x": cx,
        "y": cy,
        "width": str(width),
        "height": str(height),
        "fill": fill,
        "transform": f"translate(-{float(width)/2}, -{float(height)/2})"
    })

    if roundAmount:
        node.set("rx", str(roundAmount))
    if strokeWidth:
        node.set("stroke-width", str(strokeWidth))

    return node


def CreateTextNode(x, y, text, size=24, fill="transparent", font="Arial"):
    x_str = coord_to_str(x)
    y_str = coord_to_str(y)

    debug(f"CreateTextNode: text='{text}' x={x_str} y={y_str} size={size} fill={fill} font={font}")

    node = Element("text", {
        "x": x_str,
        "y": y_str,
        "fill": fill,
        "font-family": font,
        "font-size": str(size),
        "text-anchor": "middle",
        "dominant-baseline": "middle",
        "dy": "0.35em" # compensate for text rendering issues
    })
    node.text = str(text)

    return node


def LoadJson(pathToJson):
    debug(f"Loading JSON: {pathToJson}")
    with open(pathToJson, "r") as f:
        return json.load(f)


def ConvertNodesIntoSvg(nodes, width, height, shadow=None):
    debug(f"Converting {len(nodes)} nodes into SVG size={width}x{height}")

    svg = Element("svg", {
        "xmlns": "http://www.w3.org/2000/svg",
        "width": str(width),
        "height": str(height),
        "viewBox": f"0 0 {width} {height}",
        "style": "background:none"
    })

    if shadow:
        if shadow is True:
            shadow = {}

        size = shadow.get("size", 4)
        dx = shadow.get("x", 3)
        dy = shadow.get("y", 3)

        debug(f"Adding shadow filter: size={size} dx={dx} dy={dy}")

        defs = SubElement(svg, "defs")
        filter_elem = SubElement(defs, "filter", {
            "id": "shadow",
            "x": "-20%",
            "y": "-20%",
            "width": "140%",
            "height": "140%"
        })

        SubElement(filter_elem, "feGaussianBlur", {
            "in": "SourceAlpha",
            "stdDeviation": str(size),
            "result": "blur"
        })
        SubElement(filter_elem, "feOffset", {
            "in": "blur",
            "dx": str(dx),
            "dy": str(dy),
            "result": "offset"
        })
        SubElement(filter_elem, "feFlood", {
            "flood-color": "rgba(0,0,0,0.5)",
            "result": "color"
        })
        SubElement(filter_elem, "feComposite", {
            "in": "color",
            "in2": "offset",
            "operator": "in",
            "result": "shadow"
        })

        merge = SubElement(filter_elem, "feMerge")
        SubElement(merge, "feMergeNode", {"in": "shadow"})
        SubElement(merge, "feMergeNode", {"in": "SourceGraphic"})

        # Wrap all nodes in <g> using this filter
        group = SubElement(svg, "g", {"filter": "url(#shadow)"})
        for i, node in enumerate(nodes):
            debug(f"  Node {i:02d}: {tostring(node, encoding='unicode').strip()}")
            group.append(node)
    else:
        for i, node in enumerate(nodes):
            debug(f"  Node {i:02d}: {tostring(node, encoding='unicode').strip()}")
            svg.append(node)

    debug("Shadow added")

    return svg


def WriteSvgFile(svg_element, name, output_dir):
    debug(f"Writing...")
    os.makedirs(output_dir, exist_ok=True)
    path = os.path.join(output_dir, f"{name}.svg")
    rough_str = ElementTree(svg_element).write(path, encoding="utf-8", xml_declaration=True)
    raw_xml = tostring(svg_element, encoding="unicode")
    parsed = minidom.parseString(raw_xml)
    pretty_xml = parsed.toprettyxml(indent="  ")

    with open(path, "w", encoding="utf-8") as f:
        f.write(pretty_xml)

    debug(f"Wrote SVG file: {path}")


def CreateLayer(layerInfo, output_dir):
    name = layerInfo.get("name", "unnamed")
    width = layerInfo.get("width", 600)
    height = layerInfo.get("height", 600)
    position = (width / 2, height / 2)

    debug(f"Layer '{name}' {width}x{height} center=({position[0]:.1f},{position[1]:.1f})")

    nodes = []
    for op in layerInfo.get("operations", []):
        t = op["type"]
        debug(f"Operation: {t}")

        if t == "circle":
            nodes.append(CreateCircleNode(
                op.get("x", position[0]),
                op.get("y", position[1]),
                op["radius"],
                op.get("fill", "transparent"),
                op.get("strokeWidth"),
                op.get("strokeFill")
            ))

        elif t == "arc":
            nodes.append(CreateArcNode(position, 
                op["radius"],
                op["degreesStart"],
                op["degreesEnd"],
                op["innerThickness"],
                op["fill"])
            )

        elif t == "gaugeTicks":
            nodes.append(CreateGaugeTicksNode(position, 
                                              op["radius"],
                                              op["degreesStart"],
                                              op["degreesEnd"], 
                                              op["degreesGap"],
                                              op.get("tickLength", 20),
                                              op.get("tickWidth", 2),
                                              op.get("tickFill")))

        elif t == "gaugeTickLabels":
            nodes.append(CreateGaugeTickLabelsNode(position, 
                                                   op["radius"], 
                                                   op["degreesStart"],
                                                   op["degreesEnd"], 
                                                   op.get("degreesGap", 10),
                                                   op["labels"],
                                                   op.get("labelFill", "rgb(255,255,255)"),
                                                   op["labelSize"],
                                                   op["labelFont"]))
        elif t == "text":
            nodes.append(CreateTextNode(
                op["x"],
                op["y"],
                op["text"],
                op.get("size", 24),
                op.get("fill", "rgb(255,255,255)"),
                op.get("font", "Arial")
            ))
        elif t == "square":
            nodes.append(CreateSquareNode(
                op["x"],
                op["y"],
                op["width"],
                op["height"],
                op.get("fill", "transparent"),
                op.get("round"),
                op.get("strokeWidth")
            ))
        elif t == "triangle":
            nodes.append(CreateTriangleNode(
                op["x"],
                op["y"],
                op["width"],
                op["height"],
                op.get("rotation", 0),
                op.get("fill", "transparent"),
                op.get("strokeWidth"),
                svgWidth=width,
                svgHeight=height
            ))
        else:
            debug(f"Unknown operation type: {t}")

    shadow = layerInfo.get("shadow")

    svg = ConvertNodesIntoSvg(nodes, width, height, shadow)
    WriteSvgFile(svg, name, output_dir)


def main():
    if len(sys.argv) < 2:
        print("Usage: create-svg-gauge/main.py path/to/input.json path/to/output (default: cwd)")
        sys.exit(1)

    input_path = sys.argv[1]
    output_dir = sys.argv[2] if len(sys.argv) > 2 else os.getcwd()

    data = LoadJson(input_path)
    layers = data.get("layers", [])
    debug(f"Loaded {len(layers)} layers from JSON")

    for layer in layers:
        CreateLayer(layer, output_dir)


if __name__ == "__main__":
    main()

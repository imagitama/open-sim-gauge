# Client & Editor

## Configuration

<!-- START_SECTION:config -->

### ServerConfig

Configure the server IP and port.

| Property | Type | Default | Description |
|-----------|------|----------|--------------|
| `ipAddress` | `string` | `"127.0.0.1"` |  |
| `port` | `int` | `1234` |  |

### Gauge

An object that describes a gauge and how to render it.

| Property | Type | Default | Description |
|-----------|------|----------|--------------|
| `name` | `string?` |  | The name of the gauge. Used for referencing it from a panel and for debugging. |
| `path` | `string?` |  | Replace this gauge with another gauge in another file. Does not merge anything. |
| `width` | `int` |  | The width of the gauge (in pixels). |
| `height` | `int` |  | The height of the gauge (in pixels). |
| `origin` | `[double\|string, double\|string]` | `["50%", "50%"]` | The origin of the gauge which is used for all transforms such as positioning, scaling and rotation. |
| `layers` | `List<Layer>` | `new()` | The layers to render to make the gauge. |
| `clip` | `ClipConfig?` |  | How to clip the layers of the gauge. Useful for gauges like an attitude indicator that translates outside of the gauge bounds. |
| `grid` | `double?` |  | Renders a grid with the provided cell size. |
| `debug` | `bool?` |  | Extra logging. Beware of console spam! |

### Config

The config for your client.

| Property | Type | Default | Description |
|-----------|------|----------|--------------|
| `fps` | `int` | `60` | The intended FPS when rendering. |
| `server` | `ServerConfig` |  | Configure the server IP and port. |
| `panels` | `List<Panel>` |  | The panels to render. On desktop a panel is a window. |
| `gauges` | `List<Gauge>` | `[]` | The gauges that are available to your panels. Optional because your panels can reference gauge JSON files by path. |
| `debug` | `bool` | `false` | Log extra info to the console. |
| `requireConnection` | `bool` | `true` | If to only render panels if connected.<br>Note: There should always be a console open on launch. |

### RotateConfig

An object that describes how a layer should rotate. Inherits from `TransformConfig`.

| Property | Type | Default | Description |
|-----------|------|----------|--------------|
| `wrap` | `bool` | `false` | If to allow the rotation to "wrap" around 360 degrees such as with an altimeter. |

### GaugeRef

An object that describes a reference to a gauge to render inside a panel.

| Property | Type | Default | Description |
|-----------|------|----------|--------------|
| `name` | `string?` |  | The name of the gauge to use. Optional if you specify a path. |
| `path` | `string?` |  | The path to a JSON file that contains the gauge to use.<br>The file should contain a single property "gauge" which is the Gauge object. |
| `position` | `[double\|string, double\|string]` | `[0, 0]` | The position of the gauge inside the panel.<br>X or Y can be a pixel value or a string which is a percent of the panel.<br>Use a negative value to flip the position (so -100 is 100px from the right edge). |
| `scale` | `double` | `1.0` | How much to scale the gauge (respecting the width you set). |
| `width` | `double?` |  | Force the width (and height) of the gauge in pixels before scaling. |
| `skip` | `bool` | `false` | If to skip rendering this gauge. |

### PathConfig

An object that describes how a layer should translate along a path. Inherits from `TransformConfig`.

| Property | Type | Default | Description |
|-----------|------|----------|--------------|
| `image` | `string` |  | The path to an SVG to use. It must contain a single path element. |
| `width` | `double?` | `SVG viewbox width or layer width` | The width of the SVG (in pixels). |
| `height` | `double?` | `SVG viewbox height or layer height` | The width of the SVG (in pixels). |
| `origin` | `[double\|string, double\|string]` | `["50%", "50%"]` | The origin of the SVG for positioning. |
| `position` | `[double\|string, double\|string]` | `["50%", "50%"]` | The position of the image inside the gauge.<br>X or Y can be a pixel value or a string which is a percent of the gauge. |

### TextDef

An object that describes what kind of text to render in the layer.

| Property | Type | Default | Description |
|-----------|------|----------|--------------|
| `var` | `[string, string]` |  | How to subscribe to a SimVar (and its unit) as the source of the text. eg. ["AIRSPEED INDICATED", "knots"]<br>Note all vars are requested as floats so units like "position" -127..127 are mapped to -1..1. |
| `default` | `string?` |  | The default text to render when there is no SimVar value. |
| `template` | `string?` |  | How to format the text. [Cheatsheet](https://gist.github.com/luizcentennial/c6353c2ae21815420e616a6db3897b4c) |
| `fontSize` | `double` | `64` | The size of the text. |
| `fontFamily` | `string?` |  | The family of the text. Supports any system font plus any inside the `fonts/` directory (currently only "Gordon").<br>If you specify a font path this lets you choose a family inside it.<br><default>OS default ("Segoe UI" on Windows)<default> |
| `font` | `string?` |  | Path to a font file to use. Relative to the config JSON file. |
| `color` | `ColorDef?` | `rgb(255, 255, 255)` | The color of the text as a CSS-like value.<br>eg. "rgb(255, 255, 255)" or "#FFF" or "white" |
| `horizontal` | `TextHorizontalAlignment` | `Center` | How to align the text horizontally.<br>"Left" would be the text starts at the layer X position, going right.<br>"Right" would be the text starts at the layer X position, going left. |
| `vertical` | `TextVerticalAlignment` | `Center` | How to align the text vertically.<br>"Top" would be the text starts at the layer Y position, going down.<br>"Bottom" would be the text starts at the layer Y position, going up. |

### Panel

An object that describes a panel.

| Property | Type | Default | Description |
|-----------|------|----------|--------------|
| `name` | `string` |  | The unique name of this panel. |
| `vehicle` | `string \| string[]` | `[]` | The name or names of vehicles this panel should render for.<br>A vehicle's name is determined by the data source. eg. a C172 in MSFS2020/24 is "Cessna Skyhawk G1000 Asobo".<br>Include specific vehicles by providing a glob pattern like: ["\*Cessna\*", "\*PA44\*"]<br>Exclude specific vehicles by providing a "exclude" glob pattern like: ["!\*Cessna\*"]<br>More info on glob patterns: https://code.visualstudio.com/docs/editor/glob-patterns<br>Tester: https://www.digitalocean.com/community/tools/glob?comments=true&glob=%2ACessna%2A&matches=false&tests=Cessna%20172&tests=Cessna&tests=PA44<br>Omit or use empty array to use for all vehicles. |
| `gauges` | `List<GaugeRef>` |  | Which gauges to render in this panel. |
| `screen` | `int?` | `0` | The index of the screen you want to render this panel on (starting at 0 which is usually your main one). |
| `width` | `double?` |  | The width of the panel in pixels.<br>Optional if you use fullscreen. |
| `height` | `double?` |  | The width of the panel in pixels.<br>Optional if you use fullscreen. |
| `fullscreen` | `bool` | `false` | If to have the panel fill the screen. |
| `position` | `[double\|string, double\|string]` | `["50%", "50%"]` | The position of the panel inside the screen (where 0,0 is the top-left of the specific screen).<br>X or Y can be a pixel value or a string which is a percent of the screen.<br>Use a negative value to flip the position (so -100 is 100px from the right edge). |
| `origin` | `[double\|string, double\|string]` | `["50%", "50%"]` | The origin of the panel which is used for all transforms such as positioning, scaling and rotation. |
| `background` | `string` | `rgb(0, 0, 0)` | Background color of the panel as a CSS-like value. Cannot use transparency.<br>eg. "rgb(255, 255, 255)" or "#FFF" or "white" |
| `transparent` | `bool?` |  | If to render with a transparent background. |
| `onTop` | `bool?` |  | If to render this panel above all other desktop windows and apps. |
| `grid` | `double?` |  | Renders a grid with the provided cell size. |
| `clip` | `bool?` | `true` | If to clip all gauges. |
| `skip` | `bool?` | `false` | If to skip rendering this panel. |
| `force` | `bool?` | `false` | If to always render this panel. |
| `debug` | `bool?` | `false` | Extra console logging for this panel. |

### TranslateConfig

An object that describes how a layer should translate. Inherits from `TransformConfig`.

| Property | Type | Default | Description |
|-----------|------|----------|--------------|

### CalibrationPoint

An object that describes how to "map" a SimVar value into a specific degrees. Useful for non-linear gauges like a C172 airspeed indicator. When rendering the actual degrees is interpolated between calibration points.

| Property | Type | Default | Description |
|-----------|------|----------|--------------|
| `value` | `double` |  |  |
| `degrees` | `double` |  |  |

### TransformConfig

How to transform a layer using a SimVar.

| Property | Type | Default | Description |
|-----------|------|----------|--------------|
| `var` | `[string, string]` |  | The SimVar and its unit to subscribe to. eg. ["AIRSPEED INDICATED", "knots"]<br>Note all vars are requested as floats so units like "position" -127..127 are mapped to -1..1. |
| `from` | `double?` |  | The minimum to translate/rotate. If the value is 50% the from->to then it will render at 50% from->to. |
| `to` | `double?` |  | The maximum to translate/rotate. If the value is 50% the from->to then it will render at 50% from->to. |
| `min` | `double?` |  | The minimum possible value for the SimVar. eg. for airspeed it would be 0 for 0 knots |
| `max` | `double?` |  | The maximum possible value for the SimVar. |
| `invert` | `bool` | `false` | If to invert the resulting rotation/translation. |
| `multiply` | `double?` |  | How much to multiply the SimVar amount by. Useful to convert "feet per second" into "feet per minute". |
| `calibration` | `List<CalibrationPoint>?` |  | How to "calibrate" raw SimVar values to specific angles because there is not a linear relationship.<br>Some gauges are not linear so require calibration (such as the C172 ASI). |
| `skip` | `bool?` |  | If to skip applying this transform. |
| `debug` | `bool?` |  | Extra logging. Beware of console spam! |
| `override` | `double?` |  | Force a SimVar value for debugging purposes. |

### ClipConfig

An object that describes how to clip the layers of a gauge.

| Property | Type | Default | Description |
|-----------|------|----------|--------------|
| `image` | `string` |  | The path to the SVG to use to clip. It must contain a single path element (such as a circle). |
| `width` | `double?` | `SVG viewbox width or gauge width` | The width of the SVG (in pixels). |
| `height` | `double?` | `SVG viewbox height or gauge height` | The width of the SVG (in pixels). |
| `origin` | `[double\|string, double\|string]` | `["50%", "50%"]` | The origin of the SVG for positioning. |
| `position` | `[double\|string, double\|string]` | `["50%", "50%"]` | The position of the clip inside the gauge.<br>X or Y can be a pixel value or a string which is a percent of the gauge. |
| `debug` | `bool` | `false` | Extra debugging for clipping. |

### Layer

An object that describes a layer of a gauge.

| Property | Type | Default | Description |
|-----------|------|----------|--------------|
| `name` | `string?` |  | The name of the layer. Only used for debugging. |
| `text` | `TextDef?` |  | Some text to render as this layer. If provided then `image` will be ignored. |
| `image` | `string?` |  | A path to an image to render as this layer. PNG and SVG supported. |
| `width` | `FlexibleDimension?` | `Gauge width` | The width of the layer (in pixels). |
| `height` | `FlexibleDimension?` | `Gauge height` | The height of the layer (in pixels). |
| `origin` | `[double\|string, double\|string]` | `["50%", "50%"]` | The origin of the layer (in pixels) for all transformations to be based on. |
| `position` | `[double\|string, double\|string]` | `["50%", "50%"]` | The position of the layer inside the gauge.<br>X or Y can be a pixel value or a string which is a percent of the gauge. |
| `transform` | `TransformDef?` |  | How to transform this layer using a SimVar. |
| `rotate` | `double` | `0` | How many degrees to initially rotate the layer. |
| `translateX` | `double` | `0` | How much to initially translate the layer on the X axis. |
| `translateY` | `double` | `0` | How much to initially translate the layer on the Y axis. |
| `fill` | `ColorDef?` |  | A color to fill with (behind any image you specify). |
| `debug` | `bool` | `false` | Render useful debugging visuals such as bounding box.<br>Note: If you subscribe to a SimVar in this layer and debugging is enabled it is sent to the server for extra logging. |
| `skip` | `bool` | `false` | If to skip rendering this layer. |

### TransformDef

An object that describes how to transform a layer using vars.

| Property | Type | Default | Description |
|-----------|------|----------|--------------|
| `rotate` | `RotateConfig?` |  |  |
| `translateX` | `TranslateConfig?` |  |  |
| `translateY` | `TranslateConfig?` |  |  |
| `path` | `PathConfig?` |  |  |

<!-- END_SECTION:config -->

### Creating SVGs

There is a Python script to generate a SVG per layer of a gauge using
"operations". Usually an operation equals a SVG node. Each operation is
described in the script's README.md.

```cli
python3 tools/create-svg-gauge.py gauges/rpm_piper_seminole/svg.json gauges/rpm_piper_seminole
```

### Fonts

When using a text layer you can specify any system font or one of these special
ones bundled with the client:

- [Gordon](http://www.identifont.com/show?62D)

## Developing

```cli
dotnet run ./src/client
```

or

```cli
dotnet run ./src/editor
```

## Publishing

```cli
bash ./build.sh
```

Upload ZIP files in `dist`

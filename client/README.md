# Client & Editor

## Config

### Root-level

| Property  | Type           | Default | Description                                                                                                        |
| --------- | -------------- | ------- | ------------------------------------------------------------------------------------------------------------------ |
| `fps`     | `int`          | `60`    | The intended FPS when rendering.                                                                                   |
| `server`  | `ServerConfig` |         | Configure the server IP and port.                                                                                  |
| `panels`  | `List<Panel>`  |         | The panels to render. On desktop a panel is a window.                                                              |
| `gauges`  | `List<Gauge>`  | `[]`    | The gauges that are available to your panels. Optional because your panels can reference gauge JSON files by path. |
| `debug`   | `bool`         | `false` | Log extra info to the console.                                                                                     |
| `editing` | `bool`         | `false` | If in edit mode.                                                                                                   |

### ServerConfig

| Property    | Type     | Default       | Description |
| ----------- | -------- | ------------- | ----------- |
| `ipAddress` | `string` | `"127.0.0.1"` |             |
| `port`      | `int`    | `1234`        |             |

### Panel

| Property      | Type                               | Default          | Description                                                                                                                                                                                                                                                       |
| ------------- | ---------------------------------- | ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `name`        | `string`                           |                  | The unique name of this panel.                                                                                                                                                                                                                                    |
| `vehicle`     | `string?`                          |                  | The name of a vehicle (ie. aircraft) to only render this panel for. Wildcards supported (eg. `"*Skyhawk*"`).<br>Uses aircraft title eg. "Cessna Skyhawk G1000 Asobo".                                                                                             |
| `gauges`      | `List<GaugeRef>`                   |                  | Which gauges to render in this panel.                                                                                                                                                                                                                             |
| `screen`      | `int?`                             | `0`              | The index of the screen you want to render this panel on (starting at 0 which is usually your main one).                                                                                                                                                          |
| `width`       | `double?`                          |                  | The width of the panel in pixels or a percent of the screen.<br>Optional if you use fullscreen.                                                                                                                                                                   |
| `height`      | `double?`                          |                  | The width of the panel in pixels or a percent of the screen.<br>Optional if you use fullscreen.                                                                                                                                                                   |
| `fullscreen`  | `bool`                             | `false`          | If to have the panel fill the screen.                                                                                                                                                                                                                             |
| `position`    | `[double\|string, double\|string]` | `["50%", "50%"]` | The position of the panel inside the screen (where 0,0 is the top-left of the specific screen).<br>X or Y can be a pixel value or a string which is a percent of the screen.<br>Use a negative value to flip the position (so -100 is 100px from the right edge). |
| `origin`      | `[double\|string, double\|string]` | `["50%", "50%"]` | The origin of the panel which is used for all transforms such as positioning, scaling and rotation.                                                                                                                                                               |
| `background`  | `string`                           | `rgb(0, 0, 0)`   | Background color of the panel as a CSS-like value. Cannot use transparency.<br>eg. "rgb(255, 255, 255)" or "#FFF" or "white"                                                                                                                                      |
| `transparent` | `bool?`                            |                  | If to render with a transparent background.                                                                                                                                                                                                                       |
| `onTop`       | `bool?`                            |                  | If to render this panel above all other desktop windows and apps.                                                                                                                                                                                                 |
| `skip`        | `bool?`                            | `false`          | If to skip rendering this panel.                                                                                                                                                                                                                                  |
| `debug`       | `bool?`                            | `false`          | Extra console logging for this panel.                                                                                                                                                                                                                             |

### GaugeRef

An object that describes a reference to a gauge to render inside a panel.

| Property | Type | Default | Description |
| -------- | ---- | ------- | ----------- |

### Gauge

An object that describes a gauge and how to render it.

| Property | Type                               | Default          | Description                                                                                                                    |
| -------- | ---------------------------------- | ---------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| `name`   | `string`                           |                  | The name of the gauge. Used for referencing it from a panel and for debugging.                                                 |
| `path`   | `string?`                          |                  |                                                                                                                                |
| `width`  | `int`                              |                  | The width of the gauge (in pixels).                                                                                            |
| `height` | `int`                              |                  | The height of the gauge (in pixels).                                                                                           |
| `origin` | `[double\|string, double\|string]` | `["50%", "50%"]` | The origin of the gauge which is used for all transforms such as positioning, scaling and rotation.                            |
| `layers` | `List<Layer>`                      | `new()`          | The layers to render to make the gauge.                                                                                        |
| `clip`   | `ClipConfig?`                      |                  | How to clip the layers of the gauge. Useful for gauges like an attitude indicator that translates outside of the gauge bounds. |

### ClipConfig

An object that describes how to clip the layers of a gauge.

| Property   | Type                               | Default                      | Description                                                                                                            |
| ---------- | ---------------------------------- | ---------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| `image`    | `string`                           |                              | The path to the SVG to use to clip. It must contain a single path element (such as a circle).                          |
| `width`    | `double?`                          | `SVG viewbox width or 100%`  | The width of the SVG (in pixels).                                                                                      |
| `height`   | `double?`                          | `SVG viewbox height or 100%` | The width of the SVG (in pixels).                                                                                      |
| `origin`   | `[double\|string, double\|string]` | `["50%", "50%"]`             | The origin of the SVG for positioning.                                                                                 |
| `position` | `[double\|string, double\|string]` | `["50%", "50%"]`             | The position of the clip inside the gauge.<br>X or Y can be a pixel value or a string which is a percent of the gauge. |
| `debug`    | `bool`                             | `false`                      | Extra debugging for clipping.                                                                                          |

### Layer

An object that describes a layer of a gauge.

| Property     | Type                               | Default          | Description                                                                                                                                                                    |
| ------------ | ---------------------------------- | ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `name`       | `string?`                          |                  | The name of the layer. Only used for debugging.                                                                                                                                |
| `text`       | `TextDef?`                         |                  | Some text to render as this layer. If provided then `image` will be ignored.                                                                                                   |
| `image`      | `string?`                          |                  | A path to an image to render as this layer. PNG and SVG supported. If provided then `text` will be ignored.                                                                    |
| `width`      | `double?`                          |                  | The width of the layer (in pixels).                                                                                                                                            |
| `height`     | `double?`                          |                  | The height of the layer (in pixels).                                                                                                                                           |
| `origin`     | `[double\|string, double\|string]` | `["50%", "50%"]` | The origin of the layer (in pixels) for all transformations to be based on.                                                                                                    |
| `position`   | `[double\|string, double\|string]` | `["50%", "50%"]` | The position of the layer inside the gauge.<br>X or Y can be a pixel value or a string which is a percent of the gauge.                                                        |
| `transform`  | `TransformDef?`                    |                  | How to transform this layer using a SimVar.                                                                                                                                    |
| `rotate`     | `double`                           | `0`              | How many degrees to initially rotate the layer.                                                                                                                                |
| `translateX` | `double`                           | `0`              | How much to initially translate the layer on the X axis.                                                                                                                       |
| `translateY` | `double`                           | `0`              | How much to initially translate the layer on the Y axis.                                                                                                                       |
| `debug`      | `bool`                             | `false`          | Render useful debugging visuals such as bounding box.<br>Note: If you subscribe to a SimVar in this layer and debugging is enabled it is sent to the server for extra logging. |
| `skip`       | `bool?`                            | `false`          | If to skip rendering this layer.                                                                                                                                               |

### TextDef

An object that describes what kind of text to render in the layer.

| Property     | Type               | Default | Description                                                                                                                                                                                                 |
| ------------ | ------------------ | ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `var`        | `[string, string]` |         | How to subscribe to a SimVar (and its unit) as the source of the text. eg. ["AIRSPEED INDICATED", "knots"]<br>Note all vars are requested as floats so units like "position" -127..127 are mapped to -1..1. |
| `default`    | `string?`          |         | The default text to render when there is no SimVar value.                                                                                                                                                   |
| `template`   | `string?`          |         | How to format the text. [Cheatsheet](https://gist.github.com/luizcentennial/c6353c2ae21815420e616a6db3897b4c)                                                                                               |
| `fontSize`   | `double`           | `64`    | The size of the text.                                                                                                                                                                                       |
| `fontFamily` | `string?`          |         | The family of the text. Supports any system font plus any inside the `fonts/` directory (currently only "Gordon").<br>If you specify a font path this lets you choose a family inside it.                   |
| `font`       | `string?`          |         | Path to a font file to use. Relative to the config JSON file.                                                                                                                                               |
| `color`      | `ColorDef?`        |         | The color of the text as a CSS-like value.<br>eg. "rgb(255, 255, 255)" or "#FFF" or "white"                                                                                                                 |

### TransformDef

An object that describes how to transform a layer using SimVars.

| Property     | Type               | Default | Description |
| ------------ | ------------------ | ------- | ----------- |
| `rotate`     | `RotateConfig?`    |         |             |
| `translateX` | `TranslateConfig?` |         |             |
| `translateY` | `TranslateConfig?` |         |             |
| `path`       | `PathConfig?`      |         |             |

### CalibrationPoint

| Property  | Type     | Default | Description |
| --------- | -------- | ------- | ----------- |
| `value`   | `double` |         |             |
| `degrees` | `double` |         |             |

### TransformConfig

How to transform a layer using a SimVar.

| Property      | Type                      | Default | Description                                                                                                                                                                      |
| ------------- | ------------------------- | ------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `var`         | `[string, string]`        |         | The SimVar and its unit to subscribe to. eg. ["AIRSPEED INDICATED", "knots"]<br>Note all vars are requested as floats so units like "position" -127..127 are mapped to -1..1.    |
| `from`        | `double?`                 |         | The minimum to translate/rotate. If the value is 50% the from->to then it will render at 50% from->to.                                                                           |
| `to`          | `double?`                 |         | The maximum to translate/rotate. If the value is 50% the from->to then it will render at 50% from->to.                                                                           |
| `min`         | `double?`                 |         | The minimum possible value for the SimVar. eg. for airspeed it would be 0 for 0 knots                                                                                            |
| `max`         | `double?`                 |         | The maximum possible value for the SimVar.                                                                                                                                       |
| `invert`      | `bool`                    | `false` | If to invert the resulting rotation/translation.                                                                                                                                 |
| `multiply`    | `double?`                 |         | How much to multiply the SimVar amount by. Useful to convert "feet per second" into "feet per minute".                                                                           |
| `calibration` | `List<CalibrationPoint>?` |         | How to "calibrate" raw SimVar values to specific angles because there is not a linear relationship.<br>Some gauges are not linear so require calibration (such as the C172 ASI). |
| `skip`        | `bool?`                   |         | If to skip applying this transform.                                                                                                                                              |
| `debug`       | `bool?`                   |         | Extra logging. Beware of console spam!                                                                                                                                           |
| `override`    | `double?`                 |         | Force a SimVar value for debugging purposes.                                                                                                                                     |

### RotateConfig

An object that describes how a layer should rotate. Inherits from
`TransformConfig`.

| Property | Type   | Default | Description                                                                      |
| -------- | ------ | ------- | -------------------------------------------------------------------------------- |
| `wrap`   | `bool` | `false` | If to allow the rotation to "wrap" around 360 degrees such as with an altimeter. |

### TranslateConfig

An object that describes how a layer should translate. Inherits from
`TransformConfig`.

| Property | Type | Default | Description |
| -------- | ---- | ------- | ----------- |

### PathConfig

An object that describes how a layer should translate along a path. Inherits
from `TransformConfig`.

| Property   | Type                               | Default                      | Description                                                                                                             |
| ---------- | ---------------------------------- | ---------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `image`    | `string`                           |                              | The path to an SVG to use. It must contain a single path element.                                                       |
| `width`    | `double?`                          | `SVG viewbox width or 100%`  | The width of the SVG (in pixels).                                                                                       |
| `height`   | `double?`                          | `SVG viewbox height or 100%` | The width of the SVG (in pixels).                                                                                       |
| `origin`   | `[double\|string, double\|string]` | `["50%", "50%"]`             | The origin of the SVG for positioning.                                                                                  |
| `position` | `[double\|string, double\|string]` | `["50%", "50%"]`             | The position of the image inside the gauge.<br>X or Y can be a pixel value or a string which is a percent of the gauge. |

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

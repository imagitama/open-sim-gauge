# create-svg-gauge

A Python script that takes an input JSON document that describes a gauge and its
layers and generates a SVG per layer.

\* = required

## Input file

| **Key**   | **Type**     | **Default** | **Description**                               |
| --------- | ------------ | ----------- | --------------------------------------------- |
| `layers`* | `LayerObj[]` |             | A list of layers which equate to an SVG file. |

## `LayerObj`

| **Key**       | **Type**         | **Default** | **Description**                                                       |
| ------------- | ---------------- | ----------- | --------------------------------------------------------------------- |
| `name`*       | `string`         |             | Name used as the SVG filename.                                        |
| `operations`* | `OperationObj[]` |             | List of drawing operations that make up the layer. Executed in order. |
| `width`       | `int`            | `600`       | Viewbox width.                                                        |
| `height`      | `int`            | `600`       | Viewbox height.                                                       |
| `shadow`      | `ShadowObj`      |             | A shadow to apply to all layers. Useful for gauge needles.            |

## `ShadowObj`

| **Key** | **Type** | **Default**         | **Description**                |
| ------- | -------- | ------------------- | ------------------------------ |
| `size`  | `int`    | `4`                 | The blur radius of the shadow. |
| `x`     | `int`    | `3`                 | Horizontal offset.             |
| `y`     | `int`    | `3`                 | Vertical offset.               |
| `color` | `string` | `"rgba(0,0,0,0.5)"` | Shadow color.                  |

## `OperationObj`

An operation to perform. Usually equates to a new node in the SVG.

### `CircleOperationObj`

| **Key**  | **Type**   | **Default**  | **Description**                  |
| -------- | ---------- | ------------ | -------------------------------- |
| `type`   | `"circle"` |              | Operation type identifier.       |
| `x`      | `float`    | Gauge center | X position of the circle center. |
| `y`      | `float`    | Gauge center | Y position of the circle center. |
| `radius` | `float`    |              | Circle radius in pixels.         |
| `fill`   | `string`   |              | Fill color.                      |

### `ArcOperationObj`

| **Key**          | **Type** | **Default** | **Description**                   |
| ---------------- | -------- | ----------- | --------------------------------- |
| `type`           | `"arc"`  |             | Operation type identifier.        |
| `radius`         | `float`  |             | Outer radius of the arc.          |
| `degreesStart`   | `float`  |             | Start angle in degrees (0° = up). |
| `degreesEnd`     | `float`  |             | End angle in degrees.             |
| `innerThickness` | `float`  |             | Arc thickness.                    |
| `fill`           | `string` |             | Arc fill color.                   |

### `GaugeTicksOperationObj`

| **Key**        | **Type**       | **Default** | **Description**                 |
| -------------- | -------------- | ----------- | ------------------------------- |
| `type`         | `"gaugeTicks"` |             | Operation type identifier.      |
| `radius`       | `float`        |             | Outer radius of the tick marks. |
| `degreesStart` | `float`        |             | Start angle in degrees.         |
| `degreesEnd`   | `float`        |             | End angle in degrees.           |
| `degreesGap`   | `float`        |             | Angle gap between tick marks.   |
| `tickLength`   | `float`        | `20`        | Length of each tick.            |
| `tickWidth`    | `float`        | `2`         | Width of each tick line.        |
| `tickFill`     | `string`       |             | Tick color.                     |

### `GaugeTickLabelsOperationObj`

| **Key**        | **Type**            | **Default**          | **Description**                            |
| -------------- | ------------------- | -------------------- | ------------------------------------------ |
| `type`         | `"gaugeTickLabels"` |                      | Operation type identifier.                 |
| `radius`       | `float`             |                      | Distance from center to label baseline.    |
| `degreesStart` | `float`             |                      | Start angle in degrees.                    |
| `degreesEnd`   | `float`             |                      | End angle in degrees.                      |
| `degreesGap`   | `float`             |                      | Optional fixed angular gap between labels. |
| `labels`       | `string[]`          |                      | List of label texts.                       |
| `labelFill`    | `string`            | `"rgb(255,255,255)"` | Label color.                               |
| `labelSize`    | `float`             | `24`                 | Font size for labels.                      |
| `labelFont`    | `string`            | `"Arial"`            | Font family for labels.                    |

### `TextOperationObj`

| **Key** | **Type** | **Default**          | **Description**            |
| ------- | -------- | -------------------- | -------------------------- |
| `type`  | `"text"` |                      | Operation type identifier. |
| `x`     | `float`  |                      | X position of the text.    |
| `y`     | `float`  |                      | Y position of the text.    |
| `text`  | `string` |                      | The text content.          |
| `size`  | `float`  | `24`                 | Font size.                 |
| `fill`  | `string` | `"rgb(255,255,255)"` | Text color.                |
| `font`  | `string` | `"Arial"`            | Font family.               |

### `SquareOperationObj`

| **Key**       | **Type**   | **Default** | **Description**            |
| ------------- | ---------- | ----------- | -------------------------- |
| `type`        | `"square"` |             | Operation type identifier. |
| `x`           | `float`    |             | Center X position.         |
| `y`           | `float`    |             | Center Y position.         |
| `width`       | `float`    |             | Width in pixels.           |
| `height`      | `float`    |             | Height in pixels.          |
| `fill`        | `string`   |             | Fill color.                |
| `round`       | `float`    |             | Corner radius.             |
| `strokeWidth` | `float`    |             | Stroke width.              |
| `strokeFill`  | `string`   |             | Stroke color.              |

### `TriangleOperationObj`

| **Key**       | **Type**            | **Default** | **Description**                      |
| ------------- | ------------------- | ----------- | ------------------------------------ |
| `type`        | `"triangle"`        |             | Operation type identifier.           |
| `x`           | `float` or `string` |             | X position (absolute or percentage). |
| `y`           | `float` or `string` |             | Y position (absolute or percentage). |
| `width`       | `float`             |             | Triangle base width.                 |
| `height`      | `float`             |             | Triangle height.                     |
| `rotation`    | `float`             | `0`         | Rotation in degrees.                 |
| `fill`        | `string`            |             | Fill color.                          |
| `strokeWidth` | `float`             |             | Stroke width.                        |
| `strokeFill`  | `string`            |             | Stroke color.                        |

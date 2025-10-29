# OpenSimGauge

An open-source tool for rendering customizable gauge panels for MSFS 2020/2024
(and any other sim out there) - no coding needed, just config files and
PNGs/SVGs.

Works on Windows, macOS (ARM) and Linux.

![](./screenshot.png)

Default 6 pack SVGs sourced from
[here](https://github.com/cecn/Skyhawk-Flight-Instruments/tree/master).

Inspired by
[Scott's instrument panel](https://github.com/scott-vincent/instrument-panel).

## Usage

1. Launch your flight sim
2. On the same PC run `server.exe`
3. On the same PC or another device (macOS and Linux supported) run your client
   (eg. `client.exe` on Windows)
4. Start a fight and the gauges should start animating

To create a custom panel and/or gauge you must define everything inside of
`config.json`.

## Config (client)

\* = required

In your client edit `config.json` to configure it:

| Key     | Type         | Default | Description                                         |
| ------- | ------------ | ------- | --------------------------------------------------- |
| panels* | Panel[]      |         | The panels to display.                              |
| gauges* | Gauge[]      |         | The gauges available for your panels.               |
| debug   | bool         | false   | If to log more info and draw useful debugging info. |
| server  | ServerConfig |         | Override the default server connection info.        |

### `ServerConfig`

| Key       | Type   | Default   | Description |
| --------- | ------ | --------- | ----------- |
| ipAddress | string | 127.0.0.1 |             |
| port      | int    | 1234      |             |

## Creating a panel

In your client edit `config.json` and under key `panels` define a panel:

| Key       | Type                      | Default | Description                                                                            |
| --------- | ------------------------- | ------- | -------------------------------------------------------------------------------------- |
| name      | string                    |         | The name of the panel. Used for debugging.                                             |
| screen    | int                       | 0       | The index of the screen/monitor to display the panel on. 0 should be your primary one. |
| position* | [int\|string,int\|string] |         | The position of the panel (in pixels). Relative to the selected screen.                |
| width     | int or string             | 100%    | The width of the panel (in pixels).                                                    |
| height    | int or string             | 100%    | The height of the panel (in pixels).                                                   |
| gauges*   | `Gauge`[]                 |         | The gauges to display in the panel.                                                    |

### `Gauge`

| Key       | Type                      | Default | Description                                                                                                                                           |
| --------- | ------------------------- | ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| name      | string                    |         | The name of the gauge you should have defined in the gauges list.                                                                                     |
| path      | string                    |         | The path to a JSON file that contains a gauge.                                                                                                        |
| position* | [int\|string,int\|string] |         | The position of the gauge in your panel. Relative to the top left of the panel. Relative to the gauge's origin point (if defined). Percent supported. |
| scale     | float                     | 1       | The scale of the gauge as a percent.                                                                                                                  |

## Creating a gauge

In your client edit `config.json` and under key `gauges` define a gauge.
Optionally you can create a JSON file anywhere and link to it by specifying a
`path` in your layer.

| Key     | Type      | Default | Description                                                                           |
| ------- | --------- | ------- | ------------------------------------------------------------------------------------- |
| name*   | string    |         | The unique name of the gauge.                                                         |
| width*  | int       |         | The width of the gauge (in pixels). Used for positioning of your layers and clipping. |
| height* | int       |         | The height of the gauge (in pixels).                                                  |
| origin  | [int,int] | 50%,50% | The center of the gauge. Used for position, scale, etc.                               |
| layers* | `Layer`[] |         | The layers of your gauge.                                                             |
| clip    | `ClipObj` |         | How to clip your layers using an SVG path.                                            |

### `Layer`

| Key        | Type           | Default   | Description                                                                                                           |
| ---------- | -------------- | --------- | --------------------------------------------------------------------------------------------------------------------- |
| name       | string         |           | The name of your layer. Optional for debugging.                                                                       |
| image      | string         |           | The path to an image to render. PNGs recommended. SVGs supported. Relative to your config file.                       |
| width      | int            |           | The width of the image. If using an SVG it will default to viewbox width.                                             |
| height     | int            |           | The height of the image. If using an SVG it will default to viewbox height.                                           |
| origin     | [int,int]      | 50%,50%   | The position of the "center" of your image. Used as pivot point for rotation and translation.                         |
| position   | [int,int]      |           | Where to position your image/text/whatever inside your gauge. For a needle you probably want the center of the gauge. |
| rotate     | float          | 0         | How many degrees to initially rotate it. Used to fix needles starting in the wrong direction.                         |
| translateX | int            | 0         | How much to translate your layer initially on the X axis.                                                             |
| translateY | int            | 0         | How much to translate your layer initially on the Y axis.                                                             |
| transform  | `TransformObj` | Transform | How to transform the layer using SimVars.                                                                             |
| skip       | bool           | false     | If to skip rendering this layer.                                                                                      |

### `TransformObj`

This object describes different ways you can transform the layer using SimVars.
All are optional and stack on each other.

| Key        | Type           | Default | Description                                   |
| ---------- | -------------- | ------- | --------------------------------------------- |
| rotate     | `RotateObj`    |         | How to rotate your layer.                     |
| translateX | `TranslateObj` |         | How to translate (move) your layer.           |
| translateY | `TranslateObj` |         | How to translate (move) your layer.           |
| path       | `PathObj`      |         | How to translate your layer along a SVG path. |

#### `RotateObj`

This object describes how to rotate your layer using SimVars:

| Key    | Type                            | Default | Description                                                                                                                                                                                                              |
| ------ | ------------------------------- | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| var*   | [string*, string*, `VarConfig`] |         | The name of the SimVar to depend on and what type it is. eg. `["PLANE HEADING DEGREES TRUE", "degrees"]`. List defined [here](https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm). |
| from   | int                             |         | The minimum angle to rotate from. Relative to 0 degrees north. Can be negative.                                                                                                                                          |
| to     | int                             |         | The maximum angle to rotate to. Relative to 0 degrees north. Can be negative.                                                                                                                                            |
| min    | float                           |         | The minimum possible value from the SimVar. Used to determine the rotation.                                                                                                                                              |
| max    | float                           |         | The maximum possible value from the SimVar. Used to determine the rotation.                                                                                                                                              |
| wrap   | bool                            | false   | If the degrees can wrap around like an altimeter.                                                                                                                                                                        |
| invert | bool                            | false   | If to invert the degrees.                                                                                                                                                                                                |

#### `TranslateObj`

This object describes how to translate (move) your layer using SimVars:

| Key  | Type                            | Default | Description                                                                                                                                                                                                                                                                                                         |
| ---- | ------------------------------- | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| var* | [string*, string*, `VarConfig`] |         | The name of the SimVar to depend on and what type it is. eg. `["INDICATED AIRSPEED", "knots"]`. List defined [here](https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm).<br />Note: All values are requested as float so some units like `position` are normalized to -1...1. |
| from | int                             |         | The minimum position to translate from (in pixels).                                                                                                                                                                                                                                                                 |
| to   | int                             |         | The maximum position to translate to (in pixels).                                                                                                                                                                                                                                                                   |

#### `PathObj`

This object describes how to translate (move) your layer along a path defined in
a SVG, using SimVars. Useful for gauges like a turn coordinator.

| Key    | Type                            | Default        | Description                                                                                                                                                                                                                                                                                                               |
| ------ | ------------------------------- | -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| var*   | [string*, string*, `VarConfig`] |                | The name of the SimVar to depend on and what type it is. eg. `["TURN COORDINATOR BALL", "position"]`. List defined [here](https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm).<br />Note: All values are requested as float so some units like `position` are normalized to -1...1. |
| image* | string                          |                | The path to the SVG used to determine the path. It must contain a view box and a single `<path>` object. Relative to your config file.                                                                                                                                                                                    |
| width  | int                             | viewbox width  | The width of the SVG that contains the path (in pixels).                                                                                                                                                                                                                                                                  |
| height | int                             | viewbox height | The height of the SVG that contains the path (in pixels).                                                                                                                                                                                                                                                                 |
| min    | number                          |                | The minimum possible value from the SimVar. Used to determine the path position. eg. `-1` for the turn coordinator.                                                                                                                                                                                                       |
| max    | number                          |                | The maximum possible value from the SimVar. Used to determine the path position. eg. `1` for the turn coordinator.                                                                                                                                                                                                        |

#### `VarConfig`

Some extra configuration such as calibrating values when gauges are not
perfectly linear.

| Key         | Type               | Default | Description                                                            |
| ----------- | ------------------ | ------- | ---------------------------------------------------------------------- |
| calibration | `CalibrationObj`[] |         | A list of degree positions. Useful for non-linear airspeed indicators. |

##### `CalibrationObj`

| Key     | Type   | Default | Description                                                                         |
| ------- | ------ | ------- | ----------------------------------------------------------------------------------- |
| value   | number |         | The SimVar value to use as this position. eg. 40 for an airspeed of 40 knots.       |
| degrees | number |         | The corresponding degrees to use. eg. for 40 you may use something like 33 degrees. |

## `ClipObj`

This object describes how to clip all layers using an SVG that contains a path
as the clip boundary. Useful for layers in an attitude indicator which need to
translate outside the boundary of the gauge.

| Key      | Type      | Default        | Description                                                                                  |
| -------- | --------- | -------------- | -------------------------------------------------------------------------------------------- |
| image*   | string    |                | The path to a SVG to use to clip. Relative to your config file.                              |
| width    | int       | viewbox width  | The width of the image to render the SVG at.                                                 |
| height   | int       | viewbox height | The height of the image to render the SVG at.                                                |
| origin   | [int,int] | [0,0]          | The position of the "center" of your clip. Used as pivot point for rotation and translation. |
| position | [int,int] |                | Where to position your clip inside your gauge. Usually the center.                           |

There is a `circle-clip.svg` which contains a perfect circle:

```json
{
  "clip": {
    "image": "circle-clip.svg",
    "width": 400,
    "height": 400,
    "origin": [200, 200],
    "position": [250, 250]
  }
}
```

## Using SVGs

You can render a SVG in any layer by specifying the SVG path, width and height:

```json
{
  "image": "mysvg.svg",
  "width": 600,
  "height": 600
}
```

I wrote a script to generate the layers of a gauge as SVGs using a config file
(see example JSON file):

```cli
python3 tools/create-svg-gauge.py gauges/rpm_piper_seminole/svg.json gauges/rpm_piper_seminole
```

### Fonts

When using a text layer you can specify any system font or one of these special
ones bundled with the client:

| Font Family                                  | Description           |
| -------------------------------------------- | --------------------- |
| [Gordon](http://www.identifont.com/show?62D) | 1940s-1980s aircraft. |

## Config (server)

In your server edit `config.json` to configure it (* = required):

| Key    | Type                           | Default        | Description                                                                                                                                                                           |
| ------ | ------------------------------ | -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| source | `"SimConnect"` or `"Emulator"` | `"SimConnect"` | The name of the data source for your clients. The emulator spits out random values for a 6 pack.                                                                                      |
| server | `ServerConfig`                 |                | Override the default server connection.                                                                                                                                               |
| rate   | int (ms)                       | 50             | How fast to ask the data source for new data, which is then sent over the network.<br />100 = 10Hz which is okay, 50 = 20Hz for most gauges, 33 = 30Hz is very good but can be laggy. |
| debug  | bool                           | false          | If to log more info.                                                                                                                                                                  |

### `ServerConfig`

| Key       | Type   | Default   | Description |
| --------- | ------ | --------- | ----------- |
| ipAddress | string | 127.0.0.1 |             |
| port      | int    | 1234      |             |

## Development

```cli
cd ./client/src
dotnet run

cd ./server/src
dotnet run
```

### macOS

```cli
brew install --cask dotnet-sdk
```

### Linux

todo

## Building

Use the dotnet CLI to build for Windows, macOS and Linux.

On Windows you can just run `client/build.ps1` to build each platform.

## FAQ

### Can I create my own client?

Yes - it just needs to connect over TCP to the server, broadcast the correct
initialization message as JSON and the server will start sending it the SimVars.

### Can I use this with my train or some other sim?

Yes - you just need to creae a new data source in the server and change your
`config.json` to use it:

```c#
    public class MyAwesomeTrainSim : IDataSource
    {
        // a function called whenever a gauge layer wants to subscribe to a variable
        public void SubscribeToVar(string varName, string unit, Action<object> callback);

        bool IsConnected { get; set; } // if connected or not

        void Connect() {
          // connect to the game's API
        }
        void Disconnect() {
          // disconnect (called when all clients have disconnected)
        }
        void Listen() {
          // start a loop to request data from the game (optional)
        }
    }
}
```

## Ideas

- better interpolation of values for smoother updates
- send `config.json` to clients to remote update
- Android/iOS app
- add JSON validation for better UX
- visual editor for panels/gauge
- supply your own fonts in gauges
- dim panel if not connected to sim

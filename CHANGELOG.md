# 0.0.10

- renamed client and server `config.json` to `client.json` and `server.json`
  respectively
- create "combined" client and server binary for easier launching
- interpolate vars for smoother rendering
- render warning if variable is empty
- fixed font file selector wrong file type
- changed panels to never render if vehicle set and current vehicle does not
  match
- changed client `config.RequireConnection` to default `true` (relying on
  console appearing instead)
- write server logs to file

# 0.0.9

- fixed client crash
- fixed high DPI issues and panel sizes (may cause lower resolution rendering)

# 0.0.8

- unsubscribe from all vars/events on client disconnect
- stop throwing exception on null vehicle name
- changed `IDataSource` methods to async
- added `server.config.ReconnectDelay = ms`
- panels are now always shown
  - added `client.config.RequireConnection = bool` to disable this
- moved CPU to its own config
- moved data source name to attribute `[DataSourceName("SimConnect")]`
- fixed main menu black text on Linux
- fixed text rendering without a template
- fixed creating data sources twice
- fixed gauges not defaulting to center of panel
- fixed `set` and `unset` server CLI commands

# 0.0.7

- added "connect" button to editor for live editing
- improved performance when using text
- fixed text rendering under image
- fixed layer width/height not accepting percentage
- added `layer.fill = color`
- fixed negative position
- changed all color values to support anything CSS-like (eg. "red")
- added manual input and clear button to color picker dialog
- fixed text rendering using font files (and supplied font file)
- added `gauge.debug = bool`
- fixed gauge editor not refreshing properly

# 0.0.6

- fixed unique path transforms not working
- added `panel.grid = number` and `gauge.grid = number` to render a grid
- fixed `layer.transform.path.origin` throwing error
- validate JSON and show nice error message
- override config with CLI args
- render debugging info in 2nd pass to avoid overlapping

# 0.0.5

- moved data sources to plugin architecture
- even smoother drawing on high PPI displays
- changed panel editor to 2 separate windows
- render something if panel gauge not found
- added `panel.ontop = bool` to have control over render on top
- if client in debug mode always render all panels (still respects skipping)

# 0.0.4

- added a GUI for editing panels (`editor.exe`)
- improved rendering performance
- various fixes
- panels always render on top

# 0.0.3

- aircraft-specific panels with wildcards (`panel.vehicle = "*Cessna Skyhawk*"`)
- smoother drawing on high PPI displays
- render message if disconnected
- added transparency to panels (`panel.transparency = true`)

Tested in MSFS2020 Win10 and macOS ARM.

# 0.0.2

- handle reconnection better
- support percentage for all positions/origins
- determine origin automatically as [50%, 50%]
- changed pixel values from `int` to `double` to support sub-pixel rendering
- moved SimVar calibration to `TransformConfig`
- improved default server rate to `16.7` (60Hz)
- changed default FPS to 120
- fixed text positioning
- fixed emulator ball position

Tested in MSFS2020 Win10 and macOS ARM.

# 0.0.1

- initial version

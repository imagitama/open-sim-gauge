# 0.0.6

- fixed unique path transforms not working
- added `panel.grid = number` and `gauge.grid = number` to render a grid
- fixed `layer.transform.path.origin` throwing error
- validate JSON and show nice error message

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

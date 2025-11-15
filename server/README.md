# Server

## Configuring

Edit `config.json`:

### Config

Configuration of the server application that runs on the host machine.

| Property | Type                         | Default        | Description                                                                                                    |
| -------- | ---------------------------- | -------------- | -------------------------------------------------------------------------------------------------------------- |
| `source` | `'SimConnect' \| 'emulator'` |                | Which data source to use.                                                                                      |
| `server` | `ServerConfig`               | `ServerConfig` | Override the default IP address and port of the server.                                                        |
| `rate`   | `double`                     | `16.7`         | Override the default poll rate the data source should use (which is also network send rate).<br>16.7ms = 60Hz. |
| `debug`  | `bool`                       | `false`        | Log extra output to help diagnose issues.                                                                      |

### ServerConfig

Override the default IP address and port of the server.

| Property    | Type     | Default     | Description |
| ----------- | -------- | ----------- | ----------- |
| `ipAddress` | `string` | `"0.0.0.0"` |             |
| `port`      | `int`    | `1234`      |             |

## Developing

```cli
dotnet run ./src/server
```

## Publishing

```cli
bash ./build.sh
```

Upload ZIP files in `dist`

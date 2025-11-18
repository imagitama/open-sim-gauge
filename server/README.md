# Server

## Config

### Root-level

Configuration of the server application that runs on the host machine.

| Property | Type           | Default        | Description                                                                                                    |
| -------- | -------------- | -------------- | -------------------------------------------------------------------------------------------------------------- |
| `source` | `string`       |                | Which data source to use. The default config uses SimConnect.                                                  |
| `server` | `ServerConfig` | `ServerConfig` | Override the default IP address and port of the server.                                                        |
| `rate`   | `double`       | `16.7`         | Override the default poll rate the data source should use (which is also network send rate).<br>16.7ms = 60Hz. |
| `debug`  | `bool`         | `false`        | Log extra output to help diagnose issues.                                                                      |

### ServerConfig

Override the default IP address and port of the server.

| Property    | Type     | Default     | Description |
| ----------- | -------- | ----------- | ----------- |
| `ipAddress` | `string` | `"0.0.0.0"` |             |
| `port`      | `int`    | `1234`      |             |

## Creating data sources

A data source is a generic way to connect to _something_ and subscribe to
variables and vehicles.

You can build your own, create a cross-platform DLL and dump it into directory
`data-sources` and the server will automatically load it for you.

The existing SimConnect data source is designed this way.

### Developing a data source

1. Create a new dotnet core app and depend on the abstractions project:

   ```xml
   <ItemGroup>
       <ProjectReference Include="..\..\abstractions\OpenSimGaugeAbstractions.csproj" />
   </ItemGroup>
   ```

2. Create a class that implements `IDataSource`
3. Build it and output the DLL into directory `data-sources` alongside your
   `server.exe`:

   server.exe\
   data-sources/MyCoolDataSource.dll

4. Switch your config `source` to use whatever name you specified in your data
   source

## Developing

You must build the data sources before they work in development:

```cli
bash ./build.sh osx-arm64
```

or

```cli
dotnet build ./src/data-sources/Emulator
```

Then run the server:

```cli
dotnet run --project ./src/server
```

## Publishing

```cli
bash ./build.sh
```

Upload ZIP files in `dist`

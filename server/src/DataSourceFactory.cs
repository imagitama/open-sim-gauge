namespace OpenGaugeServer
{
    public static class DataSourceFactory
    {
        public static IDataSource Create(string sourceName)
        {
    #if !DEBUG || WINDOWS
            if (sourceName.Equals(SourceName.SimConnect, StringComparison.OrdinalIgnoreCase))
                return new SimConnectDataSource();
    #endif
            if (sourceName.Equals(SourceName.Emulator, StringComparison.OrdinalIgnoreCase))
                return new EmulatorDataSource();
            if (sourceName.Equals(SourceName.Cpu, StringComparison.OrdinalIgnoreCase))
                return new CpuDataSource();

            throw new NotSupportedException($"Unknown data source: {sourceName}");
        }
    }
}
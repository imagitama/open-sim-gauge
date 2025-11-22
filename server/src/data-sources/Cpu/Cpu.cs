using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenGaugeAbstractions;

[DataSourceName("Cpu")]
public class CpuDataSource : DataSourceBase
{
    private Config _config;
    public override string? CurrentVehicleName { get; set; } = "CPU";
    private CancellationTokenSource? _cts;
    private readonly object _lock = new();
    private bool _isSubscribed = false;
    private readonly Dictionary<string, Action<object>> _eventCallbacks = [];

    public CpuDataSource(Config config)
    {
        _config = config;
    }

    public override async Task Connect()
    {
        if (IsConnected) return;
        IsConnected = true;

        _cts = new CancellationTokenSource();
    }

    public override async Task Disconnect()
    {
        if (!IsConnected) return;
        IsConnected = false;
        _cts?.Cancel();
    }

    public override async Task Listen()
    {
    }

    public override async Task SubscribeToVar(string varName, string? unit, Action<object> callback)
    {
        if (_isSubscribed)
            return;

        _ = Task.Run(() => PollCpuUsageAsync(callback, _config.Rate, _cts.Token));

        _isSubscribed = true;
    }

    public override async Task UnsubscribeFromVar(string varName, string? unit, Action<object> callback)
    {
        _cts?.Cancel();
        _isSubscribed = false;
    }

    public override async Task SubscribeToEvent(string eventName, Action<object> callback)
    {
        var key = eventName;
        _eventCallbacks[key] = callback;
    }

    public override async Task UnsubscribeFromEvent(string eventName, Action<object> callback)
    {
        var key = eventName;
        _eventCallbacks.Remove(key);
    }

    public override async Task SubscribeToVehicle(Action<string> callback)
    {
    }

    private async Task PollCpuUsageAsync(Action<object> callback, double rate, CancellationToken token)
    {
        Console.WriteLine("[CPU] Polling CPU usage");

        while (!token.IsCancellationRequested)
        {
            float cpuUsage = GetSystemCpuUsage();
            lock (_lock)
            {
                callback.Invoke(cpuUsage);
            }

            await Task.Delay((int)rate, token);
        }

        Console.WriteLine("[CPU] Stopped polling CPU usage");
    }

    private static float GetSystemCpuUsage()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetWindowsCpuUsage();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return GetMacCpuUsage();
        else
            return GetLinuxCpuUsage();
    }

    private static float GetWindowsCpuUsage()
    {
        using var proc = Process.GetCurrentProcess();
        var startCpu = proc.TotalProcessorTime;
        var startTime = DateTime.UtcNow;
        Thread.Sleep(200);
        var endCpu = proc.TotalProcessorTime;
        var endTime = DateTime.UtcNow;
        var cpuUsedMs = (endCpu - startCpu).TotalMilliseconds;
        var totalMsPassed = (endTime - startTime).TotalMilliseconds * Environment.ProcessorCount;
        return (float)(cpuUsedMs / totalMsPassed * 100);
    }

    private static float GetLinuxCpuUsage()
    {
        try
        {
            var fields = File.ReadAllText("/proc/stat").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            long user = long.Parse(fields[1]);
            long nice = long.Parse(fields[2]);
            long system = long.Parse(fields[3]);
            long idle = long.Parse(fields[4]);
            long total = user + nice + system + idle;
            Thread.Sleep(200);
            fields = File.ReadAllText("/proc/stat").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            long user2 = long.Parse(fields[1]);
            long nice2 = long.Parse(fields[2]);
            long system2 = long.Parse(fields[3]);
            long idle2 = long.Parse(fields[4]);
            long total2 = user2 + nice2 + system2 + idle2;
            long totalDelta = total2 - total;
            long idleDelta = idle2 - idle;
            return (float)(100.0 * (totalDelta - idleDelta) / totalDelta);
        }
        catch
        {
            return 0;
        }
    }

    private static float GetMacCpuUsage()
    {
        try
        {
            using var ps = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sh",
                    Arguments = "-c \"ps -A -o %cpu | awk '{s+=$1} END {print s}'\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            ps.Start();
            string output = ps.StandardOutput.ReadToEnd().Trim();
            ps.WaitForExit(1000);
            if (float.TryParse(output, out var total))
                return Math.Clamp(total / Environment.ProcessorCount, 0, 100);
        }
        catch { }
        return 0;
    }
}

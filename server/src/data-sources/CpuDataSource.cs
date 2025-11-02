using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace OpenGaugeServer
{
    public class CpuDataSource : IDataSource
    {
        public bool IsConnected { get; set; }

        private Action<object>? _cpuCallback;
        private CancellationTokenSource? _cts;
        private readonly object _lock = new();

        public void Connect()
        {
            if (IsConnected) return;
            IsConnected = true;

            _cts = new CancellationTokenSource();
        }

        public void Disconnect()
        {
            if (!IsConnected) return;
            IsConnected = false;
            _cts?.Cancel();
        }

        public void Listen(Config config)
        {
            if (_cts == null)
                return;
            
            Task.Run(() => PollCpuUsageAsync(config.Rate, _cts.Token));
        }

        public void SubscribeToVar(string varName, string unit, Action<object> callback)
        {
            if (varName.Equals("CPU", StringComparison.OrdinalIgnoreCase))
            {
                _cpuCallback = callback;
            
                Console.WriteLine($"[CPU] Subscribed to var '{varName}' ({unit})");
            }
            else
            {
                Console.WriteLine($"[CPU] Unknown var '{varName}' ({unit})");
            }
        }

        public void SubscribeToEvent(string eventName, Action callback) { }

        public void WatchVar(string varName) { }

        private async Task PollCpuUsageAsync(double rate, CancellationToken token)
        {
            Console.WriteLine("[CPU] Polling CPU usage");

            while (!token.IsCancellationRequested)
            {
                float cpuUsage = GetSystemCpuUsage();
                lock (_lock)
                {
                    _cpuCallback?.Invoke(cpuUsage);
                }

                await Task.Delay((int)rate, token);
            }
        }

        private float GetSystemCpuUsage()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetWindowsCpuUsage();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetMacCpuUsage();
            else
                return GetLinuxCpuUsage();
        }

        private float GetWindowsCpuUsage()
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

        private float GetLinuxCpuUsage()
        {
            try
            {
                var fields = System.IO.File.ReadAllText("/proc/stat").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                long user = long.Parse(fields[1]);
                long nice = long.Parse(fields[2]);
                long system = long.Parse(fields[3]);
                long idle = long.Parse(fields[4]);
                long total = user + nice + system + idle;
                Thread.Sleep(200);
                fields = System.IO.File.ReadAllText("/proc/stat").Split(' ', StringSplitOptions.RemoveEmptyEntries);
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

        private float GetMacCpuUsage()
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
}

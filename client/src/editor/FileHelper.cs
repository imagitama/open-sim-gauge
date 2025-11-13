using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenGaugeClient.Editor
{
    public static class FileHelper
    {
        public static void OpenInDefaultApp(string filePath)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", filePath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", filePath);
                }
                else
                {
                    throw new PlatformNotSupportedException("Unsupported OS");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open file: {ex.Message}");
            }
        }

        public static void RevealFile(string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (string.IsNullOrEmpty(directory))
                    throw new ArgumentException("Invalid file path.", nameof(filePath));

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\""));
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    if (File.Exists(filePath))
                        Process.Start("open", $"-R \"{filePath}\"");
                    else
                        Process.Start("open", $"\"{directory}\"");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", directory);
                }
                else
                {
                    throw new PlatformNotSupportedException("Unsupported OS");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open folder: {ex.Message}");
            }
        }
    }
}
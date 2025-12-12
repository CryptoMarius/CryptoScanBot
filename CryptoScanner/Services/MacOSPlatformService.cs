using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CryptoScanner.Services;

public class MacOSPlatformService : IPlatformService
{
    public string GetDataDirectory()
    {
        // macOS: ~/Library/Application Support/CryptoScanBot
        var appSupport = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appSupport, "CryptoScanner");
    }

    public Task<bool> OpenExternalApp(string appName)
    {
        try
        {
            // macOS uses 'open' command with -a flag for applications
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"-a \"{appName}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open app: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task<bool> OpenFile(string filePath)
    {
        try
        {
            // macOS uses 'open' command for files
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"\"{filePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open file: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public string PlatformName => "macOS";
}
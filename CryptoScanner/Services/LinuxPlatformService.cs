using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CryptoScanner.Services;

public class LinuxPlatformService : IPlatformService
{
    public string GetDataDirectory()
    {
        // Linux: ~/.local/share/CryptoScanBot
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".local", "share", "CryptoScanner");
    }

    public Task<bool> OpenExternalApp(string appName)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = appName,
                UseShellExecute = true
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
            // xdg-open is the standard way on Linux
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
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

    public string PlatformName => "Linux";
}
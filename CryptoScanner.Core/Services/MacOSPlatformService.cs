using CryptoScanner.Core.Core;

using System.Diagnostics;

namespace CryptoScanner.Services;

public class MacOSPlatformService : IPlatformService
{
    public string GetDataDirectory()
    {
        // And allow user defined data folder
        ApplicationParams.InitApplicationOptions();
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ApplicationParams.Options?.AppDataFolder ?? CryptoScanner.Core.Const.Constants.AppName);
        return folder;
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
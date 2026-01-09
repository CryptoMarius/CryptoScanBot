using CryptoScanner.Core.Core;

using System.Diagnostics;

namespace CryptoScanner.Core.Services;

public class LinuxPlatformService : IPlatformService
{
    public string GetDataDirectory()
    {
        // And allow user defined data folder
        ApplicationParams.InitApplicationOptions();
        var folder = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share"),
            ApplicationParams.Options?.AppDataFolder ?? Const.Constants.AppName);
        return folder;
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
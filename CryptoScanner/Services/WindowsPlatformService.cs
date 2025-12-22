using CryptoScanner.Core.Core;

using System.Diagnostics;

namespace CryptoScanner.Services;

public class WindowsPlatformService : IPlatformService
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
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open file: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public string PlatformName => "Windows";
}
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CryptoScanner.Services;

public class WindowsPlatformService : IPlatformService
{
    public string GetDataDirectory() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CryptoScanBot");

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
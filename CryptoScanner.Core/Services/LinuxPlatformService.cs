using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Services;

public class LinuxPlatformService : IPlatformService
{
    public string GetDataDirectory()
    {
        // Normally we store data in the user data folder under the name of the application
        var baseFolder = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share"));

        // But we can overrule that via the -f parameter and that can be a partial or a full path
        ApplicationParams.InitApplicationOptions();
        var folder = ApplicationParams.Options?.AppDataFolder;
        if (string.IsNullOrEmpty(folder))
        {
            // This is the standard path
            return Path.Combine(baseFolder, Const.Constants.AppName);
        }
        else if (!Path.IsPathFullyQualified(folder))
        {
            // This is the standard path + folder parameter
            return Path.Combine(baseFolder, folder);
        }
        else
        {
            // This is a full path given by the parameter
            return folder;
        }
    }

    //public Task<bool> OpenExternalApp(string appName)
    //{
    //    try
    //    {
    //        Process.Start(new ProcessStartInfo
    //        {
    //            FileName = appName,
    //            UseShellExecute = true
    //        });
    //        return Task.FromResult(true);
    //    }
    //    catch (Exception ex)
    //    {
    //        System.Diagnostics.Debug.WriteLine($"Failed to open app: {ex.Message}");
    //        return Task.FromResult(false);
    //    }
    //}

    //public Task<bool> OpenFile(string filePath)
    //{
    //    try
    //    {
    //        // xdg-open is the standard way on Linux
    //        Process.Start(new ProcessStartInfo
    //        {
    //            FileName = "xdg-open",
    //            Arguments = $"\"{filePath}\"",
    //            UseShellExecute = false,
    //            CreateNoWindow = true
    //        });
    //        return Task.FromResult(true);
    //    }
    //    catch (Exception ex)
    //    {
    //        System.Diagnostics.Debug.WriteLine($"Failed to open file: {ex.Message}");
    //        return Task.FromResult(false);
    //    }
    //}

    public string PlatformName => "Linux";
}
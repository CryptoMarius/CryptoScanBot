using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Services;

public class WindowsPlatformService : IPlatformService
{
    public string GetDataDirectory()
    {
        // Normally we store data in the user data folder under the name of the application
        var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

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

}
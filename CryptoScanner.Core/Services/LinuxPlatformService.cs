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

    /// <summary>
    /// There is no single message box on Linux, so the two dialog helpers that are almost always
    /// present are tried in turn (zenity on GNOME, kdialog on KDE). When neither is installed the
    /// message still reaches the console.
    /// </summary>
    public void ShowMessage(string title, string message)
    {
        if (TryShowWith("zenity", ["--warning", $"--title={title}", $"--text={message}"]))
            return;
        if (TryShowWith("kdialog", ["--title", title, "--sorry", message]))
            return;

        Console.WriteLine($"{title}: {message}");
    }

    private static bool TryShowWith(string command, string[] arguments)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo(command) { UseShellExecute = false };
            foreach (string argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
                return false;

            process.WaitForExit();
            return true;
        }
        catch
        {
            // Not installed, or no display to show it on - the caller tries the next one
            return false;
        }
    }
}
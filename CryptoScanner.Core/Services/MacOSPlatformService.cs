using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Services;

public class MacOSPlatformService : IPlatformService
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

    /// <summary>
    /// macOS has no message-box call we can reach from .NET, so this goes through AppleScript.
    /// Falls back to the console when osascript is not available.
    /// </summary>
    public void ShowMessage(string title, string message)
    {
        try
        {
            // AppleScript string literals only need the quote and the backslash escaped
            string text = message.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string caption = title.Replace("\\", "\\\\").Replace("\"", "\\\"");

            var startInfo = new System.Diagnostics.ProcessStartInfo("osascript") { UseShellExecute = false };
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add($"display dialog \"{text}\" with title \"{caption}\" buttons {{\"OK\"}} default button 1 with icon caution");

            using var process = System.Diagnostics.Process.Start(startInfo);
            process?.WaitForExit();
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "ShowMessage");
            Console.WriteLine($"{title}: {message}");
        }
    }
}
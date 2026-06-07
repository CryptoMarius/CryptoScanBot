using CryptoScanner.Core.Const;

namespace CryptoScanner.Emulator;

/// <summary>
/// Tiny single-line file that remembers the last data folder the user picked in the
/// SetupWindow, so the next launch pre-fills the same path instead of forcing them to
/// click through the picker again. Lives at the OS user-app base (not inside the chosen
/// emulator folder — it has to survive folder switches).
/// </summary>
public static class LastFolderMemory
{
    private const string FileName = "emulator-last-folder.txt";


    private static string FilePath
    {
        get
        {
            string baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string parent = Path.Combine(baseFolder, Constants.AppName);
            return Path.Combine(parent, FileName);
        }
    }


    public static string? Load()
    {
        try
        {
            string path = FilePath;
            if (!File.Exists(path))
                return null;
            string content = File.ReadAllText(path).Trim();
            return string.IsNullOrEmpty(content) ? null : content;
        }
        catch
        {
            return null;
        }
    }


    public static void Save(string folder)
    {
        try
        {
            string path = FilePath;
            string? parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);
            File.WriteAllText(path, folder);
        }
        catch
        {
            // Non-fatal: forgetting the last folder is annoying, not a bug worth surfacing.
        }
    }
}

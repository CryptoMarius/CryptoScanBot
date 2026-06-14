using CryptoScanner.Core.Const;

namespace CryptoScanner.Emulator;

/// <summary>
/// Tiny single-line files that remember the last choices the user made in the SetupWindow — the data
/// folder and the exchange — so the next launch pre-fills both instead of forcing them through the
/// picker/combo again. Live at the OS user-app base (not inside the chosen emulator folder — they have
/// to survive folder switches).
/// </summary>
public static class LastFolderMemory
{
    private const string FolderFileName = "emulator-last-folder.txt";
    private const string ExchangeFileName = "emulator-last-exchange.txt";


    private static string FilePathFor(string fileName)
    {
        string baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string parent = Path.Combine(baseFolder, Constants.AppName);
        return Path.Combine(parent, fileName);
    }


    private static string? Read(string fileName)
    {
        try
        {
            string path = FilePathFor(fileName);
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


    private static void Write(string fileName, string value)
    {
        try
        {
            string path = FilePathFor(fileName);
            string? parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);
            File.WriteAllText(path, value);
        }
        catch
        {
            // Non-fatal: forgetting the last choice is annoying, not a bug worth surfacing.
        }
    }


    public static string? Load() => Read(FolderFileName);

    public static void Save(string folder) => Write(FolderFileName, folder);


    public static string? LoadExchange() => Read(ExchangeFileName);

    public static void SaveExchange(string exchange) => Write(ExchangeFileName, exchange);
}

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;

namespace CryptoScanner.Emulator;

/// <summary>
/// Tiny single-line files that remember the last choices the user made in the SetupWindow — the data
/// folder and the exchange — so the next launch pre-fills both instead of forcing them through the
/// picker/combo again. Live at the OS user-app base (not inside the chosen emulator folder — they have
/// to survive folder switches).
/// <para>
/// With <c>-f</c> on the command line they live in that folder instead, the same folder the live
/// scanner takes from <c>-f</c>. Two emulators started side by side (Binance and HyperLiquid, each
/// with its own <c>-f</c>) then each remember their own choices instead of overwriting each other's.
/// </para>
/// </summary>
public static class LastFolderMemory
{
    private const string FolderFileName = "emulator-last-folder.txt";
    private const string CandleFolderFileName = "emulator-last-candle-folder.txt";
    private const string ExchangeFileName = "emulator-last-exchange.txt";


    /// <summary>
    /// The OS-specific platform service. The setup dialog runs before the services are registered,
    /// so it is created here instead of taken from GlobalData.
    /// </summary>
    internal static IPlatformService CreatePlatformService()
    {
        return OperatingSystem.IsWindows()
            ? new WindowsPlatformService()
            : OperatingSystem.IsMacOS()
                ? new MacOSPlatformService()
                : OperatingSystem.IsLinux()
                    ? new LinuxPlatformService()
                    : throw new PlatformNotSupportedException($"Platform not supported: {Environment.OSVersion.Platform}");
    }


    /// <summary>
    /// The folder the files are kept in: the <c>-f</c> folder when one was given, otherwise the
    /// application folder of the user ({AppData}/CryptoScanBot) - both through
    /// <see cref="IPlatformService.GetDataDirectory"/>, the same rules as the live scanner.
    /// </summary>
    internal static string MemoryFolder()
    {
        return CreatePlatformService().GetDataDirectory();
    }


    private static string FilePathFor(string fileName)
    {
        return Path.Combine(MemoryFolder(), fileName);
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


    /// <summary>The folder given with <c>-f</c>, or null when the emulator was started without it.</summary>
    internal static string? FolderParameter()
    {
        ApplicationParams.InitApplicationOptions();
        string? folder = ApplicationParams.Options?.AppDataFolder;
        return string.IsNullOrWhiteSpace(folder) ? null : folder;
    }


    /// <summary>The exchange given with <c>-e</c>, or null when the emulator was started without it.</summary>
    internal static string? ExchangeParameter()
    {
        ApplicationParams.InitApplicationOptions();
        string? exchange = ApplicationParams.Options?.ExchangeName;
        return string.IsNullOrWhiteSpace(exchange) ? null : GlobalData.FixLegacyExchangeName(exchange.Trim());
    }


    public static string? Load() => Read(FolderFileName);

    public static void Save(string folder) => Write(FolderFileName, folder);


    public static string? LoadCandleFolder() => Read(CandleFolderFileName);

    public static void SaveCandleFolder(string folder) => Write(CandleFolderFileName, folder);


    public static string? LoadExchange() => Read(ExchangeFileName);

    public static void SaveExchange(string exchange) => Write(ExchangeFileName, exchange);
}

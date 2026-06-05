using Avalonia;

using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;

using System.Reflection;

namespace CryptoScanner.EmulatorApp;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Identify ourselves so signal/position records and side-effect gates can branch on it.
        GlobalData.IsEmulatorMode = true;
        GlobalData.Clock = new EmulatorClock { UtcNow = DateTime.UtcNow };

        var assembly = Assembly.GetExecutingAssembly().GetName();
        GlobalData.AppVersion = assembly.Version!.ToString();
        GlobalData.AppPath = Path.GetDirectoryName(AppContext.BaseDirectory)!;

        // The emulator defaults to a dedicated subfolder so its DB, settings.json and cached
        // candles do not interfere with a parallel live scanner. Users can override with the
        // standard --folder argument (absolute path or relative subfolder under %APPDATA%).
        GlobalData.AppDataFolder = ResolveAppDataFolder();
        if (!Directory.Exists(GlobalData.AppDataFolder))
            Directory.CreateDirectory(GlobalData.AppDataFolder);

        Console.WriteLine($"Emulator Version:        {GlobalData.AppVersion}");
        Console.WriteLine($"Emulator AppPath:        {GlobalData.AppPath}");
        Console.WriteLine($"Emulator AppDataFolder:  {GlobalData.AppDataFolder}");

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }


    /// <summary>
    /// Resolves the emulator's data folder. Honours <c>--folder</c> when given (absolute or
    /// relative under %APPDATA%), otherwise defaults to <c>%APPDATA%\CryptoScanBot\Emulator</c>.
    /// Keeps the emulator's state physically separate from the live scanner.
    /// </summary>
    private static string ResolveAppDataFolder()
    {
        ApplicationParams.InitApplicationOptions();
        string? folder = ApplicationParams.Options?.AppDataFolder;
        string baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (string.IsNullOrEmpty(folder))
            return Path.Combine(baseFolder, Constants.AppName, "Emulator");

        if (Path.IsPathFullyQualified(folder))
            return folder;

        return Path.Combine(baseFolder, folder);
    }


    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont();
}

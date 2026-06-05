using Avalonia;

using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;

using System.Reflection;

namespace CryptoScanner.Emulator;

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
        // standard --folder argument (absolute path or relative subfolder).
        GlobalData.AppDataFolder = ResolveAppDataFolder();
        if (!Directory.Exists(GlobalData.AppDataFolder))
            Directory.CreateDirectory(GlobalData.AppDataFolder);

        // First-run bootstrap: if the emulator folder has no settings yet, seed it from the
        // live scanner's settings.json (lives in the parent folder by convention). The user
        // can immediately tweak via the Configure dialog; nothing is shared after this copy.
        BootstrapFromLiveScanner();

        // Load the scanner settings from THIS folder. Identical to what the live scanner does
        // at startup — same SettingsBasic shape, same JSON serializer.
        GlobalData.LoadScannerConfiguration();

        Console.WriteLine($"Emulator Version:        {GlobalData.AppVersion}");
        Console.WriteLine($"Emulator AppPath:        {GlobalData.AppPath}");
        Console.WriteLine($"Emulator AppDataFolder:  {GlobalData.AppDataFolder}");

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }


    /// <summary>
    /// One-time copy of the live scanner's CryptoScanBot-settings.json into the emulator
    /// folder when the emulator has no settings file yet. The live folder is the parent of
    /// the emulator folder by convention (..\CryptoScanBot\Emulator → ..\CryptoScanBot).
    /// If the live folder cannot be located or has no settings, this is a no-op — the
    /// emulator will then start with default settings and the user can fill them via
    /// Configure.
    /// </summary>
    private static void BootstrapFromLiveScanner()
    {
        string filename = $"{Constants.AppName}-settings.json";
        string emulatorSettings = Path.Combine(GlobalData.AppDataFolder, filename);
        if (File.Exists(emulatorSettings))
            return;

        string? liveFolder = Path.GetDirectoryName(GlobalData.AppDataFolder);
        if (string.IsNullOrEmpty(liveFolder))
            return;

        string liveSettings = Path.Combine(liveFolder, filename);
        if (!File.Exists(liveSettings))
            return;

        try
        {
            File.Copy(liveSettings, emulatorSettings);
            Console.WriteLine($"Bootstrap: copied scanner settings from {liveSettings}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bootstrap: copy failed: {ex.Message}");
        }
    }


    /// <summary>
    /// Reuses the OS-specific <see cref="IPlatformService"/> from the live scanner. The
    /// emulator only needs to ensure a sensible default subfolder when the user did not pass
    /// <c>--folder</c>; we set ApplicationParams.Options.AppDataFolder to
    /// "<see cref="Constants.AppName"/>/Emulator" first, then let the PlatformService translate
    /// it into the proper base directory for Windows (%APPDATA%), macOS (~/Library/Application
    /// Support equivalent), or Linux (~/.local/share). Explicit --folder overrides keep working.
    /// </summary>
    private static string ResolveAppDataFolder()
    {
        ApplicationParams.InitApplicationOptions();
        ApplicationParams.Options ??= new ApplicationParams();

        if (string.IsNullOrEmpty(ApplicationParams.Options.AppDataFolder))
            ApplicationParams.Options.AppDataFolder = Path.Combine(Constants.AppName, "Emulator");

        IPlatformService platformService = OperatingSystem.IsWindows()
            ? new WindowsPlatformService()
            : OperatingSystem.IsMacOS()
                ? new MacOSPlatformService()
                : OperatingSystem.IsLinux()
                    ? new LinuxPlatformService()
                    : throw new PlatformNotSupportedException($"Platform not supported: {Environment.OSVersion.Platform}");

        return platformService.GetDataDirectory();
    }


    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont();
}

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

        Console.WriteLine($"Emulator Version:        {GlobalData.AppVersion}");
        Console.WriteLine($"Emulator AppPath:        {GlobalData.AppPath}");
        Console.WriteLine($"Emulator AppDataFolder:  {GlobalData.AppDataFolder}");

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
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

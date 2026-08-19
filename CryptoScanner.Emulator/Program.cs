using Avalonia;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;

using System.Reflection;

namespace CryptoScanner.Emulator;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Make the exchange implementations known to the core. The core loads this assembly by
        // name on first use anyway, but calling it here keeps the project reference visible to
        // the compiler and puts the registration in one predictable place.
        ExchangeProvider.Register();

        // Identify ourselves so signal/position records and side-effect gates can branch on it.
        GlobalData.IsEmulatorMode = true;
        GlobalData.Clock = new EmulatorClock { UtcNow = DateTime.UtcNow };

        var assembly = Assembly.GetExecutingAssembly().GetName();
        GlobalData.AppVersion = assembly.Version!.ToString();
        GlobalData.AppPath = Path.GetDirectoryName(AppContext.BaseDirectory)!;

        // The data folder is no longer fixed at process start — the SetupWindow asks the user
        // for it (with a sensible default). That keeps the UI as the single source of truth
        // for "which emulator run am I about to drive" instead of relying on shortcut args.

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }


    // No .WithInterFont() here. It only registers the Inter font collection so an explicit
    // FontFamily="Inter" can resolve; it does not change the default family, and nothing in the
    // emulator asked for Inter. Measured: with and without the call, a TextBlock still resolves to
    // the platform default (Segoe UI on Windows) — the same as the scanner and the Photino shell.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect();
}

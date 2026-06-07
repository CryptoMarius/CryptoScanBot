using Avalonia;

using CryptoScanner.Core.Core;

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

        // The data folder is no longer fixed at process start — the SetupWindow asks the user
        // for it (with a sensible default). That keeps the UI as the single source of truth
        // for "which emulator run am I about to drive" instead of relying on shortcut args.

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }


    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont();
}

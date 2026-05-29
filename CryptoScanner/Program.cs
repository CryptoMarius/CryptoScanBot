using Avalonia;
using Avalonia.WebView.Desktop;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace CryptoScanner;

//.claude\settings.json
//{
//  "permissions": {
//    "allow": [
//      "Bash(dotnet *)",
//      "Bash(find *)",
//      "Bash(grep *)",
//      "Bash(powershell *)",
//      "WebFetch(domain:github.com)",
//      "WebFetch(domain:raw.githubusercontent.com)",
//      "WebSearch"
//    ]
//  }
//}
//or:
//{
//    "permissions": {
//        "defaultMode": "bypassPermissions"
//    }
//}

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // We need a version from the main assembly
        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appVersion = assembly.Version!.ToString();
        while (appVersion.EndsWith(".0.0"))
            appVersion = appVersion[0..^2];

        GlobalData.AppVersion = appVersion;
        //System.Diagnostics.Debug.WriteLine($"GlobalData.AppVersion =  {GlobalData.AppVersion}");

        // We need a folder for accessing the Sounds
        // Correct voor single-file = false
        //GlobalData.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        // Correct voor single-file = true
        GlobalData.AppPath = Path.GetDirectoryName(AppContext.BaseDirectory)!;
        //System.Diagnostics.Debug.WriteLine($"GlobalData.AppPath =  {GlobalData.AppPath}");

        // We need a data folder to store our data (temporary dependency injection to hide details)
        var services = new ServiceCollection();
        MyServices.ConfigurePlatformServices(services);
        var platformService = services.BuildServiceProvider().GetService<IPlatformService>()
            ?? throw new InvalidOperationException("IPlatformService not registered");
        GlobalData.AppDataFolder = platformService.GetDataDirectory();
        //System.Diagnostics.Debug.WriteLine($"GlobalData.AppDataFolder =  {GlobalData.AppDataFolder}");

        // WebView2 reads WEBVIEW2_USER_DATA_FOLDER at process level before any initialization.
        // Setting it here (before BuildAvaloniaApp) ensures the cache lands in the app data folder.
        // Note: WebView2 appends "\EBWebView" to this path automatically.
        Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", GlobalData.AppDataFolder);

        // DEBUG OUTPUT
        Console.WriteLine($"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        //Console.WriteLine($"ApplicationData: {Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}");
        //Console.WriteLine($"LocalApplicationData: {Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}");
        //Console.WriteLine($"UserProfile: {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}");
        //Console.WriteLine($"Personal: {Environment.GetFolderPath(Environment.SpecialFolder.Personal)}");
        Console.WriteLine($"Scanner Version: {GlobalData.AppVersion}");
        Console.WriteLine($"Scanner AppPath: {GlobalData.AppPath}");
        Console.WriteLine($"Scanner AppDataFolder: {GlobalData.AppDataFolder}");

        // Initialize the logging system (as soon as possible)
        ScannerLog.InitializeLogging();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }


    public static AppBuilder BuildAvaloniaApp()
    {
        // Route Avalonia's internal diagnostics (binding errors, XAML loader failures,
        // layout warnings …) into ScannerLog instead of System.Diagnostics.Trace, so
        // they end up in the same NLog files as the rest of the application.
        Avalonia.Logging.Logger.Sink = new AvaloniaScannerLogSink();

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseDesktopWebView();
    }

}

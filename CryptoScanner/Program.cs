using Avalonia;
using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Services;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

using Xilium.CefGlue;
using Xilium.CefGlue.Common;

namespace CryptoScanner;

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
        System.Diagnostics.Debug.WriteLine($"GlobalData.AppVersion =  {GlobalData.AppVersion}");

        // We need a folder for accessing the Sounds
        GlobalData.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        System.Diagnostics.Debug.WriteLine($"GlobalData.AppPath =  {GlobalData.AppPath}");

        // We need a data folder to store our data (temporary dependency injection to hide details)
        var services = new ServiceCollection();
        MyServices.ConfigurePlatformServices(services);
        var platformService = services.BuildServiceProvider().GetService<IPlatformService>()
            ?? throw new InvalidOperationException("IPlatformService not registered");
        GlobalData.AppDataFolder = platformService.GetDataDirectory();
        System.Diagnostics.Debug.WriteLine($"GlobalData.AppDataFolder =  {GlobalData.AppDataFolder}");

        // In design mode we just give it an place so it can preview the axaml, otherwise return
        if (Design.IsDesignMode)
        {
            ApplicationParams.InitApplicationOptions();
            //GlobalData.AppDataFolder = ApplicationParams.Options!.AppDataFolder!;
            System.Diagnostics.Debug.WriteLine($"Running in IsDesignMode");
        }
        // Initialize the logging system (as soon as possible)
        ScannerLog.InitializeLogging();

        // Set the unhandled exception mode to force all Windows Forms errors to go through our handler.
        // https://docs.avaloniaui.net/docs/concepts/unhandledexceptions
        // Avalonia UI does not offer any mechanism to handle exceptions globally and mark this as handled. 
        //Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }


    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect().
            //.WithInterFont()
            AfterSetup(_ => { InitCefBrowser();  })
            .LogToTrace();


    /// <summary>
    /// Initialize the CEF browser environment
    /// </summary>
    private static void InitCefBrowser()
    {
        // Setup Dependency Injection (just the platform for now to hide those details)
        //var services = new ServiceCollection();
        //MyServices.ConfigurePlatformServices(services);
        //var platformService = services.BuildServiceProvider().GetService<IPlatformService>()
        //    ?? throw new InvalidOperationException("IPlatformService not registered");
        var dataFolder = GlobalData.AppDataFolder;

        var settings = new CefSettings()
        {
            LogSeverity = CefLogSeverity.Debug,
            CachePath = Path.Combine(dataFolder, "Browser"),
            RootCachePath = Path.Combine(dataFolder, "Browser"),
            LogFile = Path.Combine(dataFolder, "Browser", "cef.log"),
            NoSandbox = true, PersistSessionCookies = true, PersistUserPreferences = true,
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            WindowlessRenderingEnabled = false, // its recommended to leave this off, since its less performant and can cause more issues
        };

        // Add command line switches 
        var args = new[]
        {
            // Optional: Disable GPU for compatibility (linux?)
            //commandLine.Add("disable-gpu");
            //commandLine.AppendSwitch("disable-gpu-compositing");
            new KeyValuePair<string, string>("disable-blink-features", "AutomationControlled"),
            new KeyValuePair<string, string>("enable-javascript", ""),
            new KeyValuePair<string, string>("disable-web-security", ""), // Development only!
        };
        CefRuntimeLoader.Initialize(settings, args);

        // When using CefRuntime.Initialize the browser won't work, no errors, nothing.
        // So we use CefRuntimeLoader.Initialize instead and that works okay for now.
        //var mainArgs = new CefMainArgs([]);
        //CefRuntime.Initialize(mainArgs, settings, new CustomCefApp(), IntPtr.Zero);
    }

}

using Avalonia;

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
        SetMyAppVariables();

        // Vroeger dan alle andere..
        ScannerLog.InitializeLogging();

        // Add the event handler for handling non-UI thread exceptions to the event. 
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);

        // Add the event handler for handling UI thread exceptions to the event.
        //Application.ThreadException += new ThreadExceptionEventHandler(OnThreadException);

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

    private static void InitCefBrowser()
    {
        // Setup Dependency Injection (just the platform for now to hide those details)
        var services = new ServiceCollection();
        MyServices.ConfigurePlatformServices(services);
        var platformService = services.BuildServiceProvider().GetService<IPlatformService>()
            ?? throw new InvalidOperationException("IPlatformService not registered");
        var dataFolder = platformService.GetDataDirectory();

        var settings = new CefSettings()
        {
            LogSeverity = CefLogSeverity.Debug,
            CachePath = Path.Combine(dataFolder, "Browser"),
            RootCachePath = Path.Combine(dataFolder, "Browser"),
            LogFile = Path.Combine(dataFolder, "Browser", "cef.log"),
            NoSandbox = true,
            PersistSessionCookies = true,
            PersistUserPreferences = true,
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            // its recommended to leave this off (false), since its less performant and can cause more issues
            WindowlessRenderingEnabled = false,
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

    private static void SetMyAppVariables()
    {
        GlobalData.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;

        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appVersion = assembly.Version!.ToString();
        while (appVersion.EndsWith(".0.0"))
            appVersion = appVersion[0..^2];
        GlobalData.AppVersion = appVersion;


        // Setup Dependency Injection (just the platform for now to hide those details)
        var services = new ServiceCollection();
        MyServices.ConfigurePlatformServices(services);
        var platformService = services.BuildServiceProvider().GetService<IPlatformService>()
            ?? throw new InvalidOperationException("IPlatformService not registered");

        // TODO: Avoid this global variable (DI work), it works for now
        GlobalData.AppDataFolder = platformService.GetDataDirectory();
    }


    static void UnhandledException(object? sender, UnhandledExceptionEventArgs eventArgs)
    {
        //MessageBox.Show("UnhandledException!!!!");
        Exception e = (Exception)eventArgs.ExceptionObject;
        if (eventArgs.IsTerminating)
            ScannerLog.Logger.Error(e, "UnhandledException (terminating)");
        else
            ScannerLog.Logger.Error(e, "UnhandledException (not terminating)");
    }

    static void OnThreadException(object? sender, ThreadExceptionEventArgs eventArgs)
    {
        ScannerLog.Logger.Info("");
        ScannerLog.Logger.Info("Error " + eventArgs.Exception.Message);
        ScannerLog.Logger.Error("");
        ScannerLog.Logger.Error(eventArgs.Exception, "Global Thread Exception");
    }


}

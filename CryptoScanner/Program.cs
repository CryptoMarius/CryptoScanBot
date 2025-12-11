using Avalonia;

using CryptoScanner;
using CryptoScanner.Core.Core;

using System;
using System.Reflection;
using System.Threading;

namespace CryptoScanner;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Vroeger dan alle andere..
        InitializeApplicationVariables();
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
            .UsePlatformDetect()
            //.WithInterFont()
            .LogToTrace();


    public static void InitializeApplicationVariables()
    {
        GlobalData.AppName = "CryptoScanBot";
        GlobalData.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;

        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appVersion = assembly.Version!.ToString();
        while (appVersion.EndsWith(".0.0"))
            appVersion = appVersion[0..^2];

        GlobalData.AppVersion = appVersion;
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

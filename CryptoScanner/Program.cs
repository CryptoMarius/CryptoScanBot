using CryptoScanner.Core.Core;

using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CryptoScanner;

static class Program
{


    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Vroeger dan alle andere..
        InitializeApplicationVariables();
        ScannerLog.InitializeLogging();

        // Add the event handler for handling UI thread exceptions to the event.
        Application.ThreadException += new ThreadExceptionEventHandler(OnThreadException);

        // Set the unhandled exception mode to force all Windows Forms errors to go through our handler.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);


        // Add the event handler for handling non-UI thread exceptions to the event. 
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);

        Application.EnableVisualStyles();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.SetCompatibleTextRenderingDefault(false);

        // Via service gives thread error with the WebView browser
        //var services = new ServiceCollection();
        //services.AddTransient<Core.Exchange.ExchangeBase>();
        //services.AddTransient<Core.Exchange.SymbolBase>();
        //services.AddTransient<Core.Exchange.CandleBase>();
        //services.AddTransient<Core.Exchange.LimitRatesBase>();

        //services.AddTransient<Core.Exchange.Binance.Futures.Candle>();
        //services.AddTransient<Core.Exchange.Binance.Spot.Candle>();
        //services.AddTransient<CryptoScanner.Core.Exchange.BybitApi.Futures.LimitRate>();


        //services.AddSingleton<Core.Exchange.IExchangeOptions, Core.Exchange.ExchangeOptions>();
        //services.AddTransient<FrmMain>();
        //services.AddTransient<FrmSettings>();

        //var serviceProvider = services.BuildServiceProvider();
        //Application.Run(serviceProvider.GetRequiredService<FrmMain>());
        Application.Run(new FrmMain());
    }


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

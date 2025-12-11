using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.DashBoard.Services;
using CryptoScanner.DashBoard.ViewModels;
using CryptoScanner.MainForm.ViewModels;
using CryptoScanner.MainForm.Views;
using CryptoScanner.Services;
using CryptoScanner.Signal.ViewModels;
using CryptoScanner.Symbol.ViewModels;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace CryptoScanner;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    
    private static void InitializeGlobalData()
    {
        // Initialiseer app variabelen
        GlobalData.AppName = "CryptoScanBot";
        GlobalData.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;

        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appVersion = assembly.Version!.ToString();
        while (appVersion.EndsWith(".0.0"))
            appVersion = appVersion[0..^2];

        GlobalData.AppVersion = appVersion;

        // Laad alle data
        GlobalData.LoadSettings();
        ScannerLog.InitializeLogging();
        CryptoDatabase.SetDatabaseDefaults();
        GlobalData.LoadExchanges();
        GlobalData.LoadIntervals();
        ApplicationParams.InitApplicationOptions();
        GlobalData.InitializeExchange();

        // KRITIEK: Laad symbols en signals VOOR DI
        GlobalData.LoadSymbols();
        GlobalData.LoadSignals();

        System.Diagnostics.Debug.WriteLine($"GlobalData initialized - Symbols: {GlobalData.ActiveExchange?.SymbolListName.Count ?? 0}, Signals: {GlobalData.SignalQueue.Count}");
    }

    
    private static void ConfigureServices(IServiceCollection services)
    {
        // Register Services as Singleton (één instantie voor hele app)
        services.AddSingleton<ITradingViewService, TradingViewService>();

        // Platform service - alleen desktop platforms
        if (OperatingSystem.IsWindows())
            services.AddSingleton<IPlatformService, WindowsPlatformService>();
        else if (OperatingSystem.IsMacOS())
            services.AddSingleton<IPlatformService, MacOSPlatformService>();
        else if (OperatingSystem.IsLinux())
            services.AddSingleton<IPlatformService, LinuxPlatformService>();
        else
            throw new PlatformNotSupportedException($"Platform not supported: {Environment.OSVersion.Platform}");


        // Register ViewModels as Transient (nieuwe instantie bij elke aanvraag)
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashBoardViewModel>();
        services.AddTransient<SymbolGridViewModel>();
        services.AddTransient<SignalGridViewModel>();

        // Register Views
        services.AddTransient<MainView>();
    }

    
    public override void OnFrameworkInitializationCompleted()
    {
        // BELANGRIJK: Initialiseer GlobalData VOOR DI setup
        InitializeGlobalData();

        // Setup DI Container
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Get MainView from DI container
            desktop.MainWindow = Services.GetRequiredService<MainView>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}


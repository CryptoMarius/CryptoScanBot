using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.Sounds;
using CryptoScanner.Services;
using CryptoScanner.Views;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace CryptoScanner;

public partial class App : Application
{
    // Listen to shutsown messages (multi platform)
    private static PowerMonitorService _powerMonitor = new();

    // Forward url to our visible browser tabsheet
    public static event EventHandler<string>? EventOpenInInternalBrowser;
    public static void OpenInInternalBrowser(object sender, string url) => EventOpenInInternalBrowser?.Invoke(sender, url);
    
    // Forward url to our not visible browser tabsheet (to avoid an extra dialog)
    internal static HiddenBrowserService HiddenBrowser { get; private set; } = null!;
    internal static void OpenInHiddenBrowser(string url) => HiddenBrowser?.Navigate(url);


    public override void Initialize()
    {
        System.Diagnostics.Debug.WriteLine($"App.Initialize");

        InitializeComponent();

        GlobalData.ApplicationHasStarted += new AddTextEvent(ApplicationHasStarted);

        //// Events inregelen
        //ScannerSession.TimerAddSignal.Elapsed += TimerAddLogLinesTick;
        //ScannerSession.TimerSoundHeartBeat.Elapsed += TimerSoundHeartBeat_Tick;
        //ScannerSession.TimerShowInformation.Elapsed += dashBoardInformation1.TimerShowInformation_Tick;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        System.Diagnostics.Debug.WriteLine($"App.OnFrameworkInitializationCompleted");

        // Setup DI Container
        var services = new ServiceCollection();
        MyServices.ConfigureServices(services);
        GlobalData.Services = services.BuildServiceProvider();

        InitializeGlobalData(); // Needs the DI services
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Only close when requested
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Get MainView from DI container
            desktop.MainWindow = GlobalData.Services.GetRequiredService<MainWindow>();

            // Save states on application exit
            desktop.ShutdownRequested += This_ShutdownRequested;
        }

        base.OnFrameworkInitializationCompleted();
    }


    private static void InitializeGlobalData()
    {
        System.Diagnostics.Debug.WriteLine($"App.InitializeGlobalData");

        // Initialiseer app variabelen
        GlobalData.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;

        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appVersion = assembly.Version!.ToString();
        while (appVersion.EndsWith(".0.0"))
            appVersion = appVersion[0..^2];
        GlobalData.AppVersion = appVersion;

        if (!Design.IsDesignMode)
        {
            _powerMonitor.PowerModeChanged += OnPowerModeChanged;
            GlobalData.PlaySound += new PlayMediaEvent(PlaySound);

            HiddenBrowser = GlobalData.Services.GetRequiredService<HiddenBrowserService>();
            HiddenBrowser.Initialize();

            var scannerSession = GlobalData.GetService<IScannerSession>()
                ?? throw new InvalidOperationException("ScannerSession not registered");
            scannerSession.AfterStarup();
            scannerSession.ApplySettings();
            scannerSession.Start(0);
            //LinkTools.InitializeTradingView();
        }

        System.Diagnostics.Debug.WriteLine($"GlobalData initialized - Symbols: {GlobalData.ActiveExchange?.SymbolListName.Count ?? 0}, Signals: {GlobalData.SignalQueue.Count}");
    }

    private async void This_ShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"OnApplicationExit(start)");

        System.Diagnostics.Debug.WriteLine($"OnApplicationExit(powerMonitor?.Dispose)");
        _powerMonitor?.Dispose();
        _powerMonitor = null!;

        System.Diagnostics.Debug.WriteLine($"OnApplicationExit(ThreadSoundPlayer.StopSoundThread)");
        ThreadSoundPlayer.StopSoundThread();

        System.Diagnostics.Debug.WriteLine($"OnApplicationExit(powerMonitor?.Dispose)");
        var scannersession = GlobalData.GetService<IScannerSession>()
            ?? throw new InvalidOperationException("ScannerSession not registered");
        await scannersession.StopAsync(); // Blocks..

        System.Diagnostics.Debug.WriteLine($"OnApplicationExit(DataStore.SaveCandlesAsync)");
        await DataStore.SaveCandlesAsync();

        // Ensure all states are written to disk before exit

        // TODO: Rethink this boolean storage
        System.Diagnostics.Debug.WriteLine($"OnApplicationExit(applicationStateService.FlushToDisk)");
        ApplicationStateService applicationStateService = GlobalData.GetService<ApplicationStateService>()
            ?? throw new InvalidOperationException("ApplicationStateService not registered");
        applicationStateService.AnalyzerActive = GlobalData.Settings.Options.AnalyzerActive;
        applicationStateService.SoundsActive = GlobalData.Settings.Options.SoundsActive;
        applicationStateService.TraderActive = GlobalData.Settings.Options.TraderActive;
        applicationStateService.FlushToDisk();

        // Dispose hidden browser
        System.Diagnostics.Debug.WriteLine($"OnApplicationExit(hiddenBrowser?.Dispose)");
        var hiddenBrowser = GlobalData.GetService<HiddenBrowserService>();
        hiddenBrowser?.Dispose();
        //hiddenBrowser? = null;

        System.Diagnostics.Debug.WriteLine($"OnApplicationExit(exit)");
    }

    //private async void OnApplicationExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    //{

    //}

    private static async void OnPowerModeChanged(object? sender, PowerModeEventArgs e)
    {
        switch (e.Mode)
        {
            case PowerMode.Suspend:
                GlobalData.AddTextToLogTab("System going to sleep - disconnecting...");
                var scannersession1 = GlobalData.GetService<IScannerSession>()
                    ?? throw new InvalidOperationException("ScannerSession not registered");
                await scannersession1.StopAsync(); // Blocks..
                await DataStore.SaveCandlesAsync();
                break;

            case PowerMode.Resume:
                GlobalData.AddTextToLogTab("System resumed - reconnecting...");
                await Task.Delay(2000); // wait for netwerk
                var scannersession2 = GlobalData.GetService<IScannerSession>()
                    ?? throw new InvalidOperationException("ScannerSession not registered");
                scannersession2.Start(5000);
                break;
        }
    }

    private void ApplicationHasStarted(string text)
    {
        // Show the symbols
        Dispatcher.UIThread.Post(() => { GlobalData.SymbolsHaveChanged(""); });

        // Show barometer and that it is running
        //Invoke((System.Windows.Forms.MethodInvoker)(() => dashBoardInformation1.ShowBarometerStuff(null, null)));

        // Show the positions
        Dispatcher.UIThread.Post(() => { GlobalData.PositionsHaveChanged(""); });
    }

    private static void PlaySound(string text, bool test)
    {
        ThreadSoundPlayer.AddToQueue(text, test);
    }

    public static IBrush GetBrushResource(string resourceKey)
    {
        if (Application.Current?.TryGetResource(resourceKey, Application.Current.ActualThemeVariant, out var resource) == true 
            && resource is IBrush brush)
        {
            return brush;
        }

        // Fallback
        System.Diagnostics.Debug.WriteLine($"GetBrushResource({resourceKey}) returned default");
        return Brushes.Black;
    }


    public static IBrush PriceUp => App.GetBrushResource("PriceUpBrush");
    public static IBrush PriceDown => App.GetBrushResource("PriceDownBrush");
    public static IBrush PriceNeutral => App.GetBrushResource("PriceNeutralBrush");
}
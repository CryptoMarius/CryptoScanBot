using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.DashBoard.Services;
using CryptoScanner.DashBoard.ViewModels;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Services;
using CryptoScanner.Signal.ViewModels;
using CryptoScanner.Symbol.ViewModels;
using CryptoScanner.Log.ViewModels;
using CryptoScanner.Views;
using CryptoScanner.ViewModels;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;
using Avalonia.Media;
using CryptoScanner.Core.Model;
using Avalonia.Threading;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trader;


namespace CryptoScanner;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Singleton instance of GridStateService available throughout the application
    /// </summary>
    public static GridStateService GridStateService { get; private set; } = null!;

    /// <summary>
    /// Queued text for the Log tab
    /// LogViewModel pulls the text via a timer.
    /// </summary>
    public static readonly Queue<string> LogQueue = new();


    public override void Initialize()
    {
        System.Diagnostics.Debug.WriteLine($"App.Initialize");
        LogQueue.EnsureCapacity(2500);
        GlobalData.LogToLogTabEvent += new AddTextEvent(AddTextToLogTab);

        AvaloniaXamlLoader.Load(this);

        //GlobalData.AnalyzeSignalCreated = AnalyzeSignalCreated;
        GlobalData.ApplicationHasStarted += new AddTextEvent(ApplicationHasStarted);

        //// Events inregelen
        //ScannerSession.TimerAddSignal.Elapsed += TimerAddLogLinesTick;
        //ScannerSession.TimerSoundHeartBeat.Elapsed += TimerSoundHeartBeat_Tick;
        //ScannerSession.TimerShowInformation.Elapsed += dashBoardInformation1.TimerShowInformation_Tick;
    }


    private static void InitializeGlobalData()
    {
        System.Diagnostics.Debug.WriteLine($"App.InitializeGlobalData");

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
        GlobalData.ActiveExchange!.GetApiInstance().ExchangeDefaults();

        GlobalData.LoadSymbols();
        GlobalData.LoadSignals();

        ApplySettings();

        //TradeTools.LoadAssets();
        //TradeTools.LoadOpenPositions();
        //TradeTools.LoadClosedPositions();
        //PositionsHaveChangedEvent("");

        ScannerSession.Start(0);
        //LinkTools.InitializeTradingView();

        System.Diagnostics.Debug.WriteLine($"GlobalData initialized - Symbols: {GlobalData.ActiveExchange?.SymbolListName.Count ?? 0}, Signals: {GlobalData.SignalQueue.Count}");
    }


    private static void ApplySettings()
    {
        System.Diagnostics.Debug.WriteLine($"App.ApplySettings");
        // Is done multiple times, but that is okay
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Core.Model.CryptoExchange? exchange))
        {
            GlobalData.ActiveExchange = exchange;
        }

        var api = GlobalData.ActiveExchange!.GetApiInstance();
        string? defaultQuote = api.GetExchangeOptions().DefaultQuote;
        if (defaultQuote != null)
        {
            if (!GlobalData.Settings.QuoteCoins.TryGetValue(defaultQuote, out CryptoQuoteData? _))
            {
                CryptoQuoteData defaultQuoteData = GlobalData.AddQuoteData(defaultQuote);
                defaultQuoteData.FetchCandles = true;
                GlobalData.Settings.General.SelectedBarometerQuote = defaultQuote;
            }
        }



        //// Eventueel de nieuwe quotes zetten enz.
        //dashBoardInformation1.InitializeBarometer();

        //if ((GlobalData.Settings.General.FontSizeNew != Font.Size) || (GlobalData.Settings.General.FontNameNew.Equals(Font.Name)))
        //{
        //    Font = new System.Drawing.Font(GlobalData.Settings.General.FontNameNew, GlobalData.Settings.General.FontSizeNew,
        //        System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

        //    dashBoardControl1.Font = Font;
        //}

        //GridSymbolView.InitCommandCaptions();
        //GridSignalView.InitCommandCaptions();
        //GridLiveDataView.InitCommandCaptions();
        //GridPositionOpenView.InitCommandCaptions();
        //GridPositionClosedView.InitCommandCaptions();


        TradingConfig.IndexStrategyInternally();
        TradingConfig.InitWhiteAndBlackListSettings();

        SignalPrepare.Prepare();
        SignalExecute.Prepare();

        //// De timertjes goed zetten
        ScannerSession.SetTimerDefaults();

        //ApplicationTradingBot.Checked = GlobalData.Settings.Trading.Active;
        //ApplicationPlaySounds.Checked = GlobalData.Settings.Signal.SoundsActive;
        //ApplicationCreateSignals.Checked = GlobalData.Settings.Signal.Active;

        //splitContainer1.Panel1Collapsed = GlobalData.Settings.General.HideSymbolsOnTheLeft;

        //GlobalData.StatusesHaveChangedEvent?.Invoke("");
        //SetApplicationTitle();

        //Refresh(); // Redraw
    }



    private static void ConfigureServices(IServiceCollection services)
    {
        // Platform service - alleen desktop platforms
        if (OperatingSystem.IsWindows())
            services.AddSingleton<IPlatformService, WindowsPlatformService>();
        else if (OperatingSystem.IsMacOS())
            services.AddSingleton<IPlatformService, MacOSPlatformService>();
        else if (OperatingSystem.IsLinux())
            services.AddSingleton<IPlatformService, LinuxPlatformService>();
        else
            throw new PlatformNotSupportedException($"Platform not supported: {Environment.OSVersion.Platform}");

        // Register Services as Singleton (één instantie voor hele app)
        services.AddSingleton<ITradingViewService, TradingViewService>();
        services.AddSingleton<IJsonSerializerService, JsonSerializerService>();

        // Register ViewModels as Transient (nieuwe instantie bij elke aanvraag)
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<DashBoardViewModel>();
        services.AddTransient<SymbolGridViewModel>();
        services.AddTransient<SignalGridViewModel>();
        services.AddTransient<LogViewModel>();

        // Register Views
        services.AddTransient<MainWindow>();
    }
    

    public static T? GetService<T>() where T : class
    {
        return Services?.GetService<T>();
    }


    public override void OnFrameworkInitializationCompleted()
    {
        System.Diagnostics.Debug.WriteLine($"App.OnFrameworkInitializationCompleted");
        // Setup DI Container
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Initialize the GridStateService (loads settings from disk into memory)
        GridStateService = new GridStateService();

        // BELANGRIJK: Initialiseer GlobalData VOOR DI setup
        InitializeGlobalData();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Get MainView from DI container
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();

            // Save grid states on application exit
            desktop.Exit += OnApplicationExit;
        }

        base.OnFrameworkInitializationCompleted();
    }


    private async void OnApplicationExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        await DataStore.SaveCandlesAsync();
        // Ensure all grid states are written to disk before exit
        GridStateService.FlushToDisk();
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

    private void AddTextToLogTab(string text)
    {
        // The queue can be overwhelmed (and there is a max array size)
        try
        {
            // Via queue want afzonderlijk regels toevoegen kost relatief veel tijd
            ScannerLog.Logger.Info(text);
            text = text.Trim();

            if (text != "")
            {
                if (GlobalData.BackTest)
                    text = GlobalData.BackTestDateTime.ToLocalTime() + " " + text;
                else
                    text = DateTime.Now.ToLocalTime() + " " + text;
            }
            LogQueue.Enqueue(text);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "adding " + text);
        }
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


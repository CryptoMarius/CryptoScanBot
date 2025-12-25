using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Sounds;
using CryptoScanner.Core.Trader;
using CryptoScanner.Services;
using CryptoScanner.Views;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace CryptoScanner;

public partial class App : Application
{
    //Dialogs/DialogsServices
    //https://github.com/AvaloniaUI/Avalonia/discussions/12551

    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Singleton instance of ApplicationStateService available throughout the application
    /// </summary>
    public static ApplicationStateService ApplicationStateService { get; private set; } = null!;


    //public static HiddenBrowserService HiddenBrowser { get; private set; } = null!;

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

            //GlobalData.AnalyzeSignalCreated = AnalyzeSignalCreated;
            GlobalData.Settings.Signal.SoundsActive = true;
            GlobalData.PlaySound += new PlayMediaEvent(PlaySound);

            ScannerSession.Start(0);
            //LinkTools.InitializeTradingView();
        }

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


    public static T? GetService<T>() where T : class
    {
        return Services?.GetService<T>();
    }


    public override void OnFrameworkInitializationCompleted()
    {
        System.Diagnostics.Debug.WriteLine($"App.OnFrameworkInitializationCompleted");

        // Setup DI Container
        var services = new ServiceCollection();
        MyServices.ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Initialize the ApplicationStateService (loads settings from disk into memory)
        ApplicationStateService = new ApplicationStateService();

        // BELANGRIJK: Initialiseer GlobalData VOOR DI setup
        InitializeGlobalData();
        //var hiddenBrowser = Services.GetRequiredService<HiddenBrowserService>();
        //hiddenBrowser.Initialize();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Get MainView from DI container
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();

            // Save grid states on application exit
            desktop.Exit += OnApplicationExit;

            // Initialize CefGlue browser
            //CustomCefApp.InitializeCefRuntime();
        }

        base.OnFrameworkInitializationCompleted();
    }


    private async void OnApplicationExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        ThreadSoundPlayer.StopSoundThread();
        await ScannerSession.StopAsync(); // Blocks..
        await DataStore.SaveCandlesAsync();
        // Ensure all grid states are written to disk before exit
        ApplicationStateService.FlushToDisk();

        // Dispose hidden browser
        //var hiddenBrowser = Services.GetService<HiddenBrowserService>();
        //hiddenBrowser?.Dispose();
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
        if (GlobalData.Settings.Signal.SoundsActive)
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


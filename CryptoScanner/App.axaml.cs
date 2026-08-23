using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.SignalR;
using CryptoScanner.Core.Sounds;
using CryptoScanner.Services;
using CryptoScanner.Views;

using Microsoft.Extensions.DependencyInjection;

namespace CryptoScanner;

public partial class App : Application
{
    // Listen to shutsown messages (multi platform)
    private static PowerMonitorService _powerMonitor = new();

    // Forward url to our visible browser tabsheet
    public static event Action<string, bool>? EventOpenInInternalBrowser;
    public static void OpenInInternalBrowser(string url, bool switchTab) => EventOpenInInternalBrowser?.Invoke(url, switchTab);

    // Forward url to our not visible browser tabsheet (to avoid an extra dialog)
    internal static HiddenBrowserService EventOpenHiddenBrowser { get; private set; } = null!;
    internal static void OpenInHiddenBrowser(string url) => EventOpenHiddenBrowser?.Navigate(url);


    public override void Initialize()
    {
        System.Diagnostics.Debug.WriteLine($"App.Initialize");

        InitializeComponent();
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

        // Wire the shared chart project's browser launchers to this app's browser (the chart project
        // has no hard dependency on App; the emulator leaves these null and only uses the system browser).
        CryptoScanner.Helpers.CommandHelper.OpenInternalBrowser = OpenInInternalBrowser;
        CryptoScanner.Helpers.CommandHelper.OpenHiddenBrowser = OpenInHiddenBrowser;

        GlobalData.RunOnUiThread = action => Dispatcher.UIThread.Post(action);

        // Repaint the grids when the theme changes. The rows cache the brush they were drawn with,
        // so without this a switch left the already visible rows in the colours of the previous
        // theme. Both events matter: ActualThemeVariantChanged covers a switch from the settings,
        // ColorValuesChanged covers the operating system changing its theme while the application is
        // set to follow it (ActualThemeVariant stays Default in that case and does not fire).
        ActualThemeVariantChanged += (_, _) => GlobalData.SendMvvmMessage(new ThemeChangedMessage());
        if (PlatformSettings != null)
            PlatformSettings.ColorValuesChanged += (_, _) => GlobalData.SendMvvmMessage(new ThemeChangedMessage());

        GlobalData.SetTheme = theme =>
        {
            if (Application.Current != null)
            {
                var currentTheme = Application.Current.ActualThemeVariant;
                ThemeVariant choosenTheme = theme switch
                {
                    "Light" => ThemeVariant.Light,
                    "Dark" => ThemeVariant.Dark,
                    _ => ThemeVariant.Default
                };
                if (currentTheme != choosenTheme)
                    Application.Current.RequestedThemeVariant = choosenTheme;
            }
        };
        GlobalData.SetTitle = title =>
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.DataContext != null)
            {
                try
                {
                    dynamic viewModel = desktop.MainWindow.DataContext;
                    viewModel.Title = title;
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
                {
                    System.Diagnostics.Debug.WriteLine("Property not found on ViewModel");
                }
            }
        };

        // Basicly start the whole scanner
        InitializeGlobalDataAsync(); // Needs the DI services

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Only close when requested
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Get MainView from DI container
            desktop.MainWindow = GlobalData.Services.GetRequiredService<MainWindow>();

            // Save states on application exit
            desktop.ShutdownRequested += DoWhenShutdownRequested;
        }

        base.OnFrameworkInitializationCompleted();
    }


    private static void InitializeGlobalDataAsync()
    {
        System.Diagnostics.Debug.WriteLine($"App.InitializeGlobalData");

        if (!Design.IsDesignMode)
        {
            // Subscribe the global event handler
            Dispatcher.UIThread.UnhandledException += DoWhenUnhandledException;
            // Add the event handler for handling non-UI thread exceptions to the event.
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(DoWhenUnhandledException);

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                ScannerLog.Logger.Info("");
                ScannerLog.Logger.Info("Error " + e.Exception.Message);
                // No blank Error() line here: it lands in the error log as an empty entry, which the
                // exchange-check report then counts as a second, nameless error next to the real one.
                ScannerLog.LogGlobalException(e.Exception, "unobserved task");

                Console.WriteLine($"UnobservedTaskException exception: {e.Exception.Message}");
                e.SetObserved(); // Mark as observed to avoid crash
            };

#if DEBUG
            // FirstChanceException fires for EVERY exception the runtime raises — even ones
            // that get caught immediately by a downstream try/catch. Invaluable when an
            // exception silently disappears (Avalonia/OxyPlot rendering, async-void handlers,
            // property-changed callbacks). DEBUG-only because it's verbose; flip the filter
            // below to narrow the noise if a specific area is being investigated.
            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                // Skip the noise sources that almost never indicate a real bug:
                //  - Task/Operation cancellation: routine async cancellation
                //  - IOException with "transport connection" / "thread exit" / "application
                //    request": WebSocket / HTTP socket teardown during stream restart or
                //    reconnect. Thrown deep inside the network stack, immediately caught by
                //    the exchange library.
                //  - ObjectDisposedException on SslStream / Socket / NetworkStream: same
                //    teardown story.
                string typeName = e.Exception.GetType().Name;
                if (typeName == "TaskCanceledException" || typeName == "OperationCanceledException")
                    return;

                string message = e.Exception.Message;
                if (typeName == "IOException" &&
                    (message.Contains("transport connection") || message.Contains("thread exit") || message.Contains("application request")))
                    return;

                if (typeName == "ObjectDisposedException" &&
                    (message.Contains("SslStream") || message.Contains("Socket") || message.Contains("NetworkStream")))
                    return;

                ScannerLog.Logger.Trace($"FirstChance: {typeName}: {message}");
            };
#endif

            _powerMonitor.PowerModeChanged += DoWhenPowerModeChanged;

            Analyzers.AnalyzerRegistration.RegisterAll();

            var scannerSession = GlobalData.GetService<IScannerSession>()
                ?? throw new InvalidOperationException("ScannerSession not registered");
            scannerSession.AfterStartup();

            // Apply the theme BEFORE the main window and its grids are built. ApplyConfigurationAsync
            // sets it too, but it runs fire-and-forget, so the rows the signal, live data and position
            // grids load from the database at startup resolved their colours against a theme that was
            // still Default - and kept the light green and red for the rest of the session. The symbol
            // grid is filled from ApplyConfigurationAsync itself (after the theme is set), which is why
            // only that one, and the information dashboard, showed the right colours.
            GlobalData.SetTheme?.Invoke(GlobalData.Settings.General.Theme ?? "Default");

            _ = scannerSession.ApplyConfigurationAsync(true);
            scannerSession.Start(0);

            GlobalData.SignalRService = new SignalRService();
            _ = GlobalData.SignalRService.StartAsync();

            // Initialize a hidden browser to avoid the Altrady start question in the browser
            EventOpenHiddenBrowser = GlobalData.Services.GetRequiredService<HiddenBrowserService>();
            EventOpenHiddenBrowser.Initialize();
        }

        System.Diagnostics.Debug.WriteLine($"GlobalData initialized - Symbols: {GlobalData.ActiveExchange?.SymbolListName.Count ?? 0}, Signals: {GlobalData.SignalQueue.Count}");
    }

    private void DoWhenShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"OnApplicationExit(start) - Canceling shutdown to run async cleanup");

        // Cancel the shutdown request to prevent immediate closure
        e.Cancel = true;

        // Run shutdown async and then manually exit
        _ = Task.Run(async () =>
        {
            try
            {
                await PerformShutdownAsync();
            }
            catch (Exception ex)
            {
                ScannerLog.Logger.Error(ex, "Error during shutdown");
                System.Diagnostics.Debug.WriteLine($"Shutdown error: {ex.Message}");
            }
            finally
            {
                // After all cleanup is done, force the shutdown
                System.Diagnostics.Debug.WriteLine($"OnApplicationExit(forcing shutdown now)");

                Dispatcher.UIThread.Post(() =>
                {
                    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        // Unsubscribe to prevent re-entry
                        desktop.ShutdownRequested -= DoWhenShutdownRequested;
                        // Force shutdown
                        desktop.Shutdown();
                    }
                });
            }
        });
    }

    private static async Task PerformShutdownAsync()
    {
        // Not a neat solution, replace with a global token??
        GlobalData.ApplicationIsClosing = true;
        ScannerLog.Logger.Trace($"OnApplicationExit(start async operations)");

        ScannerLog.Logger.Trace($"OnApplicationExit(powerMonitor?.Dispose)");
        _powerMonitor?.Dispose();
        _powerMonitor = null!;

        ScannerLog.Logger.Trace($"OnApplicationExit(ThreadSoundPlayer.StopSoundThread)");
        ThreadSoundPlayer.StopSoundThread();

        ScannerLog.Logger.Trace($"OnApplicationExit(SignalRService.StopAsync)");
        if (GlobalData.SignalRService != null)
        {
            await GlobalData.SignalRService.StopAsync();
            GlobalData.SignalRService = null;
        }

        ScannerLog.Logger.Trace($"OnApplicationExit(ScannerSession.StopAsync)");
        var scannersession = GlobalData.GetService<IScannerSession>()
            ?? throw new InvalidOperationException("ScannerSession not registered");
        await scannersession.StopAsync(); // This will now complete!

        //ScannerLog.Logger.Trace($"OnApplicationExit(DataStore.SaveCandlesAsync)");
        //await DataStore.SaveCandlesAsync(); included in scannersession1.StopAsync()

        // Ensure all states are written to disk before exit
        ScannerLog.Logger.Trace($"OnApplicationExit(GlobalData.SaveSettings)");
        // SaveConfiguration rethrows now; a failure must not abort the rest of the shutdown
        try { GlobalData.SaveConfiguration(); }
        catch (Exception error) { ScannerLog.Logger.Error(error, "OnApplicationExit(SaveConfiguration)"); }

        // TODO: Rethink this boolean storage
        ScannerLog.Logger.Trace($"OnApplicationExit(applicationStateService.FlushToDisk)");
        ApplicationStateService applicationStateService = GlobalData.GetService<ApplicationStateService>()
            ?? throw new InvalidOperationException("ApplicationStateService not registered");
        applicationStateService.FlushToDisk();

        // Dispose hidden browser
        ScannerLog.Logger.Trace($"OnApplicationExit(hiddenBrowser?.Dispose)");
        var hiddenBrowser = GlobalData.GetService<HiddenBrowserService>();
        if (hiddenBrowser != null)
        {
            // Dispatch to UI thread for browser disposal
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    hiddenBrowser.Dispose();
                    ScannerLog.Logger.Trace($"OnApplicationExit(hiddenBrowser disposed successfully)");
                }
                catch (Exception ex)
                {
                    ScannerLog.Logger.Error(ex, "Error disposing hidden browser");
                    ScannerLog.Logger.Trace($"OnApplicationExit(hiddenBrowser dispose error: {ex.Message})");
                }
            });
        }

        System.Diagnostics.Debug.WriteLine($"OnApplicationExit(all operations completed)");
    }


    private static void DoWhenPowerModeChanged(object? sender, PowerModeEventArgs e)
    {
        // Fire-and-forget with error handling
        _ = HandlePowerModeChangeAsync(e.Mode);
    }

    private static async Task HandlePowerModeChangeAsync(PowerMode mode)
    {
        try
        {
            switch (mode)
            {
                case PowerMode.Suspend:
                    ScannerLog.Logger.Trace("System going to sleep - disconnecting...");
                    GlobalData.AddTextToLogTab("System going to sleep - disconnecting...");
                    if (GlobalData.SignalRService != null)
                        await GlobalData.SignalRService.StopAsync();
                    var scannersession1 = GlobalData.GetService<IScannerSession>()
                        ?? throw new InvalidOperationException("ScannerSession not registered");
                    await scannersession1.StopAsync();
                    ThreadSoundPlayer.StopSoundThread();
                    //await DataStore.SaveCandlesAsync(); included in scannersession1.StopAsync()
                    GlobalData.AddTextToLogTab("Disconnected successfully");
                    break;

                case PowerMode.Resume:
                    ScannerLog.Logger.Trace("System resumed - reconnecting...");
                    GlobalData.AddTextToLogTab("System resumed - reconnecting...");
                    await Task.Delay(2000); // wait for network
                    if (GlobalData.SignalRService != null)
                        await GlobalData.SignalRService.StartAsync();
                    var scannersession2 = GlobalData.GetService<IScannerSession>()
                        ?? throw new InvalidOperationException("ScannerSession not registered");
                    scannersession2.Start(5000);
                    GlobalData.AddTextToLogTab("Reconnected successfully");
                    break;
            }
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, $"Error handling power mode change: {mode}");
            GlobalData.AddTextToLogTab($"Power mode {mode} error: {ex.Message}");
        }
    }

    public static IBrush GetBrushResource(string resourceKey)
    {
        var app = Application.Current;
        if (app != null && app.TryGetResource(resourceKey, ResolveThemeVariant(app), out var resource)
            && resource is IBrush brush)
        {
            return brush;
        }

        // Fallback
        System.Diagnostics.Debug.WriteLine($"GetBrushResource({resourceKey}) returned default");
        return Brushes.Black;
    }


    /// <summary>
    /// The theme variant to look a themed resource up with, never ThemeVariant.Default.
    /// <para>
    /// ActualThemeVariant stays Default until the theme from the settings has been applied, and it
    /// also stays Default for as long as the application follows the operating system. Asking for a
    /// resource with Default silently falls back to the FIRST theme dictionary - the light one -
    /// while the controls themselves are already drawn dark. That is how the grids ended up with the
    /// light green and red on the dark theme.
    /// </para>
    /// </summary>
    private static ThemeVariant ResolveThemeVariant(Application app)
    {
        var variant = app.ActualThemeVariant;
        if (variant != ThemeVariant.Default)
            return variant;

        return app.PlatformSettings?.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }


    public static IBrush PriceUp => App.GetBrushResource("PriceUpBrush");
    public static IBrush PriceDown => App.GetBrushResource("PriceDownBrush");
    public static IBrush PriceNeutral => App.GetBrushResource("PriceNeutralBrush");


    private static void DoWhenUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Handle the exception
        ScannerLog.Logger.Info("");
        ScannerLog.Logger.Info("Unhandled UI exception " + e.Exception.Message);

        ScannerLog.LogGlobalException(e.Exception, "ui dispatcher");
        e.Handled = true;
    }

    static void DoWhenUnhandledException(object? sender, UnhandledExceptionEventArgs eventArgs)
    {
        // The application will still crash, but at least the error is logged

        Exception e = (Exception)eventArgs.ExceptionObject;
        ScannerLog.Logger.Info("");
        ScannerLog.Logger.Info("Unhandled exception " + e.Message);

        ScannerLog.LogGlobalException(e, eventArgs.IsTerminating
            ? "appdomain (terminating)"
            : "appdomain (not terminating)");
    }

}
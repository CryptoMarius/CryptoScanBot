using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Analyzers;
using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.SignalR;
using CryptoScanner.Core.Sounds;
using CryptoScanner.Photino.Services;
using CryptoScanner.UI;
using CryptoScanner.UI.Services;

using Microsoft.Extensions.DependencyInjection;

using Photino.Blazor;

using System.Reflection;

namespace CryptoScanner.Photino;

class Program
{
    private static PowerMonitorService? _powerMonitor;
    private static bool _isShuttingDown;
    /// <summary>Kept alive for the theme subscription; the messenger holds its subscribers weakly.</summary>
    private static readonly object _themeRecipient = new();
    private static PhotinoBlazorApp? _app;
    private static ApplicationStateService? _stateService;
    private static IScannerSession? _scannerSession;
    private static MarketIndicatorService? _marketIndicatorService;
    private static DashboardService? _dashboardService;
    private static SignalService? _signalService;
    private static PositionService? _positionService;
    private static LiveDataService? _liveDataService;
    private static LogService? _logService;
    private static SymbolService? _symbolService;
    private static InternalBrowserService? _internalBrowser;
    private static TradingViewWindow? _tradingViewWindow;
    private static HiddenBrowserWindow? _hiddenBrowserWindow;

    [STAThread]
    static void Main(string[] args)
    {
        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appVersion = assembly.Version!.ToString();
        while (appVersion.EndsWith(".0.0"))
            appVersion = appVersion[0..^2];

        GlobalData.AppVersion = appVersion;
        GlobalData.AppPath = Path.GetDirectoryName(AppContext.BaseDirectory)!;

        // WebView2 reads WEBVIEW2_USER_DATA_FOLDER at process level before any initialization,
        // so it has to be set before the Photino window is created. The data folder itself is
        // resolved by the platform service, which is safe to build stand-alone here.
        if (OperatingSystem.IsWindows())
        {
            var bootstrapServices = new ServiceCollection();
            bootstrapServices.AddSingleton<IPlatformService, WindowsPlatformService>();
            var dataFolder = bootstrapServices.BuildServiceProvider().GetRequiredService<IPlatformService>().GetDataDirectory();
            // Note: WebView2 appends "\EBWebView" to this path automatically.
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", dataFolder);
        }

        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        builder.Services.AddLogging();

        // Platform services
        if (OperatingSystem.IsWindows())
        {
            builder.Services.AddSingleton<IPlatformService, WindowsPlatformService>();
            builder.Services.AddSingleton<IStringProtectorService, WindowsStringProtectorService>();
        }
        else if (OperatingSystem.IsMacOS())
        {
            builder.Services.AddSingleton<IPlatformService, MacOSPlatformService>();
            builder.Services.AddSingleton<IStringProtectorService, MacStringProtectorService>();
        }
        else if (OperatingSystem.IsLinux())
        {
            builder.Services.AddSingleton<IPlatformService, LinuxPlatformService>();
            builder.Services.AddSingleton<IStringProtectorService, LinuxStringProtectorService>();
        }
        else
            throw new PlatformNotSupportedException($"Platform not supported: {Environment.OSVersion.Platform}");

        // Core services (same as Avalonia MyServices.ConfigureServices)
        builder.Services.AddSingleton<ApplicationStateService>();
        builder.Services.AddSingleton<IJsonSerializerService, JsonSerializerService>();
        builder.Services.AddSingleton<IScannerSession, ScannerSession>();

        // UI services
        builder.Services.AddSingleton<SignalService>();
        builder.Services.AddSingleton<LogService>();
        builder.Services.AddSingleton<DashboardService>();
        builder.Services.AddSingleton<SymbolService>();
        builder.Services.AddSingleton<PositionService>();
        builder.Services.AddSingleton<LiveDataService>();
        builder.Services.AddSingleton<DashboardPositionService>();
        builder.Services.AddSingleton<MarketIndicatorService>();
        builder.Services.AddSingleton<InternalBrowserService>();
        // Native open-file dialog for the components that need a real path (sound files). Registered
        // as a factory over the static _app because the application is only built further down.
        builder.Services.AddSingleton<IFileDialogService>(
            _ => new PhotinoFileDialogService(() => _app!));
        builder.Services.AddScoped<GridCommandService>();

        builder.RootComponents.Add<Routes>("app");

        _app = builder.Build();
        var app = _app;

        // Wire GlobalData to the DI container (same as Avalonia App.OnFrameworkInitializationCompleted)
        GlobalData.Services = app.Services;

        var platformService = app.Services.GetRequiredService<IPlatformService>();
        GlobalData.AppDataFolder = platformService.GetDataDirectory();

        ScannerLog.InitializeLogging(false);

        // One application per data folder, exactly like the Avalonia host. This has to happen before
        // any service is started: the shutdown path of a refused instance would otherwise write its
        // own (empty) settings over the ones belonging to the process that owns the folder.
        if (!DataFolderLock.TryAcquire(GlobalData.AppDataFolder))
        {
            string conflict = DataFolderLock.ConflictMessage(GlobalData.AppDataFolder);
            Console.WriteLine(conflict);
            // Not app.MainWindow.ShowMessage: the native window does not exist until app.Run(),
            // and this instance never gets that far
            platformService.ShowMessage($"{Constants.AppName} is already running", conflict);
            Environment.Exit(1);
        }

        // Wire UI delegates.
        // The MVVM messages are delivered synchronously on the calling thread here; every Blazor
        // subscriber marshals to the renderer itself with InvokeAsync, so this must NOT block or
        // swallow. Exceptions in a handler would otherwise take down the caller (a scanner
        // thread), so they are logged instead.
        GlobalData.RunOnUiThread = action =>
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "RunOnUiThread");
            }
        };
        GlobalData.SetTitle = title => { try { app.MainWindow.SetTitle(title); } catch { } };
        // Normalize the spelling so a settings file written here stays valid for the Avalonia host
        // (which switches on "Light"/"Dark"). Broadcasting the change lets the layout repaint at
        // once; relying on its poll timer made a theme switch look like the application had hung.
        GlobalData.SetTheme = theme =>
        {
            GlobalData.Settings.General.Theme = ThemeHelper.Normalize(theme);
            GlobalData.SendMvvmMessage(new CryptoScanner.Core.Messages.ThemeChangedMessage());
        };

        // Global exception handlers — surface errors that would otherwise be silently swallowed
        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            ScannerLog.Logger.Error($"Unhandled exception: {error.ExceptionObject}");
            app.MainWindow.ShowMessage("Fatal Exception", error.ExceptionObject.ToString());
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            ScannerLog.Logger.Info("");
            ScannerLog.Logger.Info("Error " + e.Exception.Message);
            // No blank Error() line here: it lands in the error log as an empty entry, which the
            // exchange-check report then counts as a second, nameless error next to the real one.
            ScannerLog.Logger.Error(e.Exception, "Global Thread Exception");

            Console.WriteLine($"UnobservedTaskException exception: {e.Exception.Message}");
            e.SetObserved();
        };

#if DEBUG
        AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
        {
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

        // Power monitor for standby/resume handling
        _powerMonitor = new PowerMonitorService();
        _powerMonitor.PowerModeChanged += OnPowerModeChanged;

        // Register all signal analyzers
        AnalyzerRegistration.RegisterAll();

        // Route "open internally" requests to the Tradingview tab instead of the system browser
        _internalBrowser = app.Services.GetRequiredService<InternalBrowserService>();
        _internalBrowser.Register();

        // Every "open internally" request now opens a second window with a browser of its own,
        // instead of the Tradingview tab. That tab was an iframe inside this same WebView, and
        // www.tradingview.com refuses to be framed - which is why it could only show the anonymous
        // embed widget, without a login and without the user's own indicators.
        _tradingViewWindow = new TradingViewWindow(app.MainWindow);
        _internalBrowser.OpenBrowserWindow = url => _tradingViewWindow.Show(url);

        // The Altrady/Hypertrader deep links get an invisible window with a browser of its own, so
        // the trading application comes up without the user first landing on the Altrady website.
        _hiddenBrowserWindow = new HiddenBrowserWindow(app.MainWindow);
        _internalBrowser.OpenHiddenBrowserWindow = url => _hiddenBrowserWindow.Navigate(url);

        // Start the scanner engine (same sequence as Avalonia App.InitializeGlobalDataAsync)
        _scannerSession = app.Services.GetRequiredService<IScannerSession>();
        _scannerSession.AfterStartup();
        _ = _scannerSession.ApplyConfigurationAsync(true);
        _scannerSession.Start(0);

        // Start SignalR
        GlobalData.SignalRService = new SignalRService();
        _ = GlobalData.SignalRService.StartAsync();

        // Start symbol service (subscribes to SymbolsHaveChangedMessage)
        _symbolService = app.Services.GetRequiredService<SymbolService>();
        _symbolService.Start();

        // Start signal service
        _signalService = app.Services.GetRequiredService<SignalService>();
        _signalService.Start();

        // Start log service
        _logService = app.Services.GetRequiredService<LogService>();
        _logService.Start();

        // Start dashboard service
        _dashboardService = app.Services.GetRequiredService<DashboardService>();
        _dashboardService.Start();

        // Start position service
        _positionService = app.Services.GetRequiredService<PositionService>();
        _positionService.Start();

        // Start live data service
        _liveDataService = app.Services.GetRequiredService<LiveDataService>();
        _liveDataService.Start();

        // Start market indicator service
        _marketIndicatorService = app.Services.GetRequiredService<MarketIndicatorService>();
        _marketIndicatorService.Start();

        // Wire sound playback
        GlobalData.PlaySound += ThreadSoundPlayer.AddToQueue;

        _stateService = app.Services.GetRequiredService<ApplicationStateService>();

        app.MainWindow
            // Not a constant: several instances run side by side and the exchange in the title is
            // what tells them apart in the taskbar and in the task manager. A constant here also
            // overwrote the title that ApplyConfigurationAsync had just pushed through
            // GlobalData.SetTitle, which is why the Photino instances all showed up as plain
            // "CryptoScanBot". The settings are loaded by AfterStartup() above, so the exchange
            // name is known at this point.
            .SetTitle(GlobalData.ApplicationTitle)
            // The icon has to be set before the native window is created, so it belongs here and
            // not in the window-created handler below.
            .ApplyIcon()
            // Both of these default to true, and while they are on Photino lets the OS decide the
            // size and the position and silently ignores SetSize / SetLeft / SetTop. Only the size
            // one was switched off, which is why the saved position never came back.
            .SetUseOsDefaultSize(false)
            .SetUseOsDefaultLocation(false);

        // The title bar is drawn by the operating system and stays white until it is told otherwise,
        // which looks broken under the dark theme. It needs the native window handle, so it can only
        // be done once the window exists - and again on every theme switch.
        app.MainWindow.RegisterWindowCreatedHandler((_, _) => WindowChrome.ApplyTitleBarTheme(app.MainWindow));
        // Same reason for the title: a caption set before the native window exists is not always
        // carried over to it, and without it the task manager shows every instance under the same
        // name.
        app.MainWindow.RegisterWindowCreatedHandler((_, _) => app.MainWindow.SetTitle(GlobalData.ApplicationTitle));
        // _themeRecipient is a static field on purpose: the messenger holds its subscribers weakly,
        // so a throwaway object here would be collected and the registration would stop working.
        WeakReferenceMessenger.Default.Register<CryptoScanner.Core.Messages.ThemeChangedMessage>(
            _themeRecipient, (_, _) => WindowChrome.ApplyTitleBarTheme(app.MainWindow));

        // Restore saved window state or use defaults
        var windowState = _stateService.GetOrCreateWindowState("MainWindow");
        if (windowState.Width > 0 && windowState.Height > 0)
        {
            app.MainWindow
                .SetSize((int)windowState.Width, (int)windowState.Height)
                .SetLeft((int)windowState.X)
                .SetTop((int)windowState.Y);

            if (windowState.State == "Maximized")
                app.MainWindow.SetMaximized(true);

            // Photino can only report the attached monitors once the native window exists, so the
            // check that the saved position still lands on one has to wait until after creation.
            // Without it, a window closed on a second screen that is no longer connected comes back
            // somewhere off-screen.
            app.MainWindow.RegisterWindowCreatedHandler((_, _) => EnsureWindowIsOnAMonitor(windowState));
        }
        else
        {
            app.MainWindow.SetSize(1400, 900).Center();
        }

        // Intercept window close to run async cleanup before exit (mirrors Avalonia ShutdownRequested pattern)
        app.MainWindow.RegisterWindowClosingHandler((sender, window) =>
        {
            if (_isShuttingDown)
                return false;

            _isShuttingDown = true;

            // Read the geometry HERE, on the thread that owns the window. PerformShutdownAsync runs
            // on a background thread, and reading these properties from there returns nothing usable
            // — the whole block sat in a catch{} so the failure was silent and the previous values
            // were kept.
            SaveWindowGeometry();

            // Tell the UI first: the close is cancelled below and the cleanup runs in the
            // background, so the window stays on screen for the whole of it.
            try { GlobalData.SendMvvmMessage(new CryptoScanner.Core.Messages.ShutdownStartedMessage()); }
            catch { }

            _ = Task.Run(async () =>
            {
                await PerformShutdownAsync();
                Environment.Exit(0);
            });

            return true;
        });

        // Wire menu Exit to trigger window close
        GlobalData.RequestShutdown = () =>
        {
            try { app.MainWindow.Close(); }
            catch { Environment.Exit(0); }
        };

        // Fallback for unexpected termination
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            if (_isShuttingDown)
                return;
            _isShuttingDown = true;
            GlobalData.ApplicationIsClosing = true;
            try { GlobalData.SaveConfiguration(); }
            catch (Exception error) { ScannerLog.Logger.Error(error, "ProcessExit(SaveConfiguration)"); }
            _stateService?.FlushToDisk();
            _stateService?.FlushWindowStateToDisk();
            NLog.LogManager.Shutdown();
        };

        app.Run();
    }

    private static bool _windowGeometrySaved;

    /// <summary>
    /// Put the window back on a monitor that actually exists. The restored position is kept as long
    /// as it falls inside one of the attached monitors — that is what brings the window back on the
    /// second screen — and the window is centered on the main monitor when that screen is gone.
    /// The size is capped at the monitor it ends up on, so a size saved on a large display does not
    /// hang off the edge of a smaller one (same as the Avalonia RestoreWindowState).
    /// </summary>
    private static void EnsureWindowIsOnAMonitor(WindowState windowState)
    {
        if (_app == null)
            return;

        try
        {
            var window = _app.MainWindow;
            int x = (int)windowState.X;
            int y = (int)windowState.Y;

            var monitors = window.Monitors.ToList();
            if (monitors.Count == 0)
                return;

            int index = monitors.FindIndex(monitor =>
                x >= monitor.MonitorArea.X && x < monitor.MonitorArea.X + monitor.MonitorArea.Width &&
                y >= monitor.MonitorArea.Y && y < monitor.MonitorArea.Y + monitor.MonitorArea.Height);

            var target = index >= 0 ? monitors[index] : window.MainMonitor;
            if (index < 0)
                window.Center();

            if (windowState.State == "Maximized")
                return;

            int width = Math.Min((int)windowState.Width, target.MonitorArea.Width);
            int height = Math.Min((int)windowState.Height, target.MonitorArea.Height);
            if (width != (int)windowState.Width || height != (int)windowState.Height)
                window.SetSize(width, height);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "EnsureWindowIsOnAMonitor");
        }
    }

    /// <summary>
    /// Store the window position, size and maximized state. Call this from the thread that owns the
    /// window; Photino cannot report its geometry from anywhere else.
    /// </summary>
    private static void SaveWindowGeometry()
    {
        if (_windowGeometrySaved || _app == null || _stateService == null)
            return;

        try
        {
            bool isMaximized = _app.MainWindow.Maximized;
            int width = _app.MainWindow.Width;
            int height = _app.MainWindow.Height;

            // Guard against a window that reports nothing usable (already destroyed, or minimized);
            // writing zeros would make the next start fall back to the 1400x900 default.
            // NOTE: while maximized these are the maximized bounds, not the restore bounds, so
            // un-maximizing after a restart gives the full-screen size back. Photino exposes no
            // restore rectangle, so tracking that would mean remembering the size ourselves on
            // every resize.
            if (width <= 0 || height <= 0)
                return;

            _stateService.SaveWindowStateValues("MainWindow",
                _app.MainWindow.Left, _app.MainWindow.Top,
                width, height,
                isMaximized ? "Maximized" : "Normal");

            // Window positions live in a SECOND, exchange-independent file, and the startup merge
            // lets that file win over the one in the data folder. Only writing the data folder file
            // meant the position was overwritten again on every start with whatever the Avalonia
            // host had left behind — which is why closing on the second screen never came back.
            // Both files, exactly like the Avalonia SaveWindowState extension does.
            _stateService.FlushToDisk();
            _stateService.FlushWindowStateToDisk();

            _windowGeometrySaved = true;
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "SaveWindowGeometry");
        }
    }

    private static async Task PerformShutdownAsync()
    {
        GlobalData.ApplicationIsClosing = true;
        ScannerLog.Logger.Trace("Shutdown(start async operations)");

        // Normally already captured by the closing handler on the UI thread; this is the fallback
        // for shutdown paths that do not go through it.
        SaveWindowGeometry();

        // Cancel background services first (WebSockets, timers, event handlers)
        try { _marketIndicatorService?.Dispose(); } catch { }
        try { _dashboardService?.Dispose(); } catch { }
        try { _signalService?.Dispose(); } catch { }
        try { _positionService?.Dispose(); } catch { }
        try { _liveDataService?.Dispose(); } catch { }
        try { _logService?.Dispose(); } catch { }
        try { _symbolService?.Dispose(); } catch { }
        try { _tradingViewWindow?.Close(); } catch { }
        try { _hiddenBrowserWindow?.Close(); } catch { }
        try { _internalBrowser?.Dispose(); } catch { }

        try
        {
            _powerMonitor?.Dispose();
            _powerMonitor = null;
        }
        catch { }
        ThreadSoundPlayer.StopSoundThread();

        // Stop async services (with generous timeout, we are not inside ProcessExit here)
        try
        {
            var stopTask = Task.Run(async () =>
            {
                ScannerLog.Logger.Trace("Shutdown(SignalRService.StopAsync)");
                if (GlobalData.SignalRService != null)
                {
                    await GlobalData.SignalRService.StopAsync();
                    GlobalData.SignalRService = null;
                }

                ScannerLog.Logger.Trace("Shutdown(ScannerSession.StopAsync)");
                if (_scannerSession != null)
                    await _scannerSession.StopAsync();
            });
            await stopTask.WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, "Shutdown(error stopping services)");
        }

        ScannerLog.Logger.Trace("Shutdown(SaveConfiguration)");
        // SaveConfiguration rethrows now, and a failure here must not take the rest of the
        // shutdown (window state, log flush) down with it
        try { GlobalData.SaveConfiguration(); }
        catch (Exception error) { ScannerLog.Logger.Error(error, "Shutdown(SaveConfiguration)"); }
        _stateService?.FlushToDisk();
        _stateService?.FlushWindowStateToDisk();

        ScannerLog.Logger.Trace("Shutdown(completed)");
        NLog.LogManager.Shutdown();
    }

    private static void OnPowerModeChanged(object? sender, PowerModeEventArgs e)
    {
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
                    var scannerSession = GlobalData.GetService<IScannerSession>()
                        ?? throw new InvalidOperationException("ScannerSession not registered");
                    await scannerSession.StopAsync();
                    ThreadSoundPlayer.StopSoundThread();
                    GlobalData.AddTextToLogTab("Disconnected successfully");
                    break;

                case PowerMode.Resume:
                    ScannerLog.Logger.Trace("System resumed - reconnecting...");
                    GlobalData.AddTextToLogTab("System resumed - reconnecting...");
                    await Task.Delay(2000);
                    if (GlobalData.SignalRService != null)
                        await GlobalData.SignalRService.StartAsync();
                    var scannerSession2 = GlobalData.GetService<IScannerSession>()
                        ?? throw new InvalidOperationException("ScannerSession not registered");
                    scannerSession2.Start(5000);
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
}

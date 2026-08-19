using CryptoScanner.Analyzers;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.SignalR;
using CryptoScanner.Core.Sounds;
using CryptoScanner.UI.Services;
using CryptoScanner.Web.Components;

using System.Reflection;

namespace CryptoScanner.Web;

class Program
{
    static void Main(string[] args)
    {
        // Make the exchange implementations known to the core. The core loads this assembly by
        // name on first use anyway, but calling it here keeps the project reference visible to
        // the compiler and puts the registration in one predictable place.
        ExchangeProvider.Register();

        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appVersion = assembly.Version!.ToString();
        while (appVersion.EndsWith(".0.0"))
            appVersion = appVersion[0..^2];

        GlobalData.AppVersion = appVersion;
        GlobalData.AppPath = Path.GetDirectoryName(AppContext.BaseDirectory)!;

        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseStaticWebAssets();

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
        builder.Services.AddScoped<GridCommandService>();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var app = builder.Build();

        // Wire GlobalData to the DI container (same as Avalonia App.OnFrameworkInitializationCompleted)
        GlobalData.Services = app.Services;

        var platformService = app.Services.GetRequiredService<IPlatformService>();
        GlobalData.AppDataFolder = platformService.GetDataDirectory();

        ScannerLog.InitializeLogging(false);

        // Wire UI delegates. The MVVM messages are delivered on the calling thread; every Blazor
        // subscriber marshals to the renderer itself with InvokeAsync. A throwing handler must
        // not take down the scanner thread that raised the message, hence the try/catch.
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
        GlobalData.SetTheme = theme => GlobalData.Settings.General.Theme = ThemeHelper.Normalize(theme);
        GlobalData.SetTitle = _ => { };

        // Sounds are played on the machine hosting the scanner, same as the desktop hosts
        GlobalData.PlaySound += ThreadSoundPlayer.AddToQueue;

        // Power monitor for standby/resume handling
        var powerMonitor = new PowerMonitorService();
        powerMonitor.PowerModeChanged += (_, e) => _ = HandlePowerModeChangeAsync(e.Mode);

        // Register all signal analyzers
        AnalyzerRegistration.RegisterAll();

        // Route "open internally" requests to the Tradingview tab instead of the system browser
        var internalBrowser = app.Services.GetRequiredService<InternalBrowserService>();
        internalBrowser.Register();

        // Start the scanner engine (same sequence as Avalonia App.InitializeGlobalDataAsync)
        var scannerSession = app.Services.GetRequiredService<IScannerSession>();
        scannerSession.AfterStartup();
        _ = scannerSession.ApplyConfigurationAsync(true);
        scannerSession.Start(0);

        // Start SignalR
        GlobalData.SignalRService = new SignalRService();
        _ = GlobalData.SignalRService.StartAsync();

        // Start symbol service (subscribes to SymbolsHaveChangedMessage)
        var symbolService = app.Services.GetRequiredService<SymbolService>();
        symbolService.Start();

        // Start signal service
        var signalService = app.Services.GetRequiredService<SignalService>();
        signalService.Start();

        // Start log service
        var logService = app.Services.GetRequiredService<LogService>();
        logService.Start();

        // Start dashboard service
        var dashboardService = app.Services.GetRequiredService<DashboardService>();
        dashboardService.Start();

        // Start position service
        var positionService = app.Services.GetRequiredService<PositionService>();
        positionService.Start();

        // Start live data service
        var liveDataService = app.Services.GetRequiredService<LiveDataService>();
        liveDataService.Start();

        // Start market indicator service
        var marketIndicatorService = app.Services.GetRequiredService<MarketIndicatorService>();
        marketIndicatorService.Start();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(typeof(CryptoScanner.UI.Routes).Assembly);

        // Persist everything on shutdown (Ctrl-C / SIGTERM / host stop)
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Register(() =>
        {
            GlobalData.ApplicationIsClosing = true;
            try { marketIndicatorService.Dispose(); } catch { }
            try { dashboardService.Dispose(); } catch { }
            try { signalService.Dispose(); } catch { }
            try { positionService.Dispose(); } catch { }
            try { liveDataService.Dispose(); } catch { }
            try { logService.Dispose(); } catch { }
            try { symbolService.Dispose(); } catch { }
            try { internalBrowser.Dispose(); } catch { }
            try { powerMonitor.Dispose(); } catch { }
            ThreadSoundPlayer.StopSoundThread();

            try
            {
                if (GlobalData.SignalRService != null)
                {
                    GlobalData.SignalRService.StopAsync().GetAwaiter().GetResult();
                    GlobalData.SignalRService = null;
                }
                scannerSession.StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "Shutdown(error stopping services)");
            }

            GlobalData.SaveConfiguration();
            app.Services.GetRequiredService<ApplicationStateService>().FlushToDisk();
            NLog.LogManager.Shutdown();
        });

        Console.WriteLine($"CryptoScanBot Web v{GlobalData.AppVersion}");
        Console.WriteLine($"Open http://localhost:5000 in your browser");

        app.Run("http://localhost:5000");
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

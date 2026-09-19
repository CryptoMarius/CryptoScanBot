using CryptoScanner.Analyzers;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.SignalR;
using CryptoScanner.Core.Sounds;
using CryptoScanner.UI.Services;
using CryptoScanner.Web.Components;

using System.Net.NetworkInformation;
using System.Net.Sockets;
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

        // The port comes from the -p option, next to the -f option that picks the data folder: one
        // instance is one data folder plus one port, and several scanners run side by side.
        ApplicationParams.InitApplicationOptions();
        int webPort = ApplicationParams.Options?.WebPort ?? DefaultWebPort;

        // ListenAnyIP instead of the loopback address the host started with. Loopback answers only
        // to the machine itself, so a phone on the same wifi never gets a reply; binding every
        // interface is what makes the front end reachable from the local network.
        //
        // Configuring Kestrel in code also settles the question of who wins: an endpoint defined
        // here overrules ASPNETCORE_URLS and the applicationUrl of a launch profile, so the port
        // stays the one that was asked for no matter how the host was started.
        //
        // THERE IS NO LOGIN IN FRONT OF THIS. Anyone on the same network can open the scanner and
        // press its buttons, including the ones that trade. Only run it on a network you trust and
        // do not forward the port on a router.
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(webPort));

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
        // Shared with the Avalonia and Photino front ends: suspend and resume have to be handled
        // one at a time, see PowerModeHandler
        powerMonitor.PowerModeChanged += PowerModeHandler.Handle;

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
        foreach (string address in GetReachableAddresses())
            Console.WriteLine($"Open http://{address}:{webPort} in your browser");
        Console.WriteLine("Anyone on this network can open it, there is no login yet");

        // The endpoint is configured on the host above, so no url here: passing one would add a
        // second endpoint instead of replacing the first.
        app.Run();
    }


    /// <summary>
    /// Default port of the web front end, used when -p is not given.
    /// </summary>
    private const int DefaultWebPort = 5000;


    /// <summary>
    /// The addresses this machine answers on, so the console prints what to type on a phone instead
    /// of leaving that to ipconfig. Loopback comes first because it always works; after it comes
    /// every network adapter that is up and carries an ordinary IPv4 address.
    /// </summary>
    private static List<string> GetReachableAddresses()
    {
        List<string> result = ["localhost"];

        try
        {
            foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
            {
                // An adapter that is down has no address to hand out, loopback is already in the
                // list, and a tunnel is not what a phone on the same wifi reaches the machine on.
                if (network.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (network.NetworkInterfaceType == NetworkInterfaceType.Loopback || network.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    continue;

                foreach (var unicast in network.GetIPProperties().UnicastAddresses)
                {
                    // IPv6 addresses are skipped: correct, but nothing anyone types on a phone
                    if (unicast.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    // 169.254.x.x means the adapter never got an address from the router, so it
                    // cannot be reached from the network either
                    string address = unicast.Address.ToString();
                    if (address.StartsWith("169.254."))
                        continue;

                    result.Add(address);
                }
            }
        }
        catch (Exception error)
        {
            // A missing address list is a worse console message, not a reason to refuse to start
            ScannerLog.Logger.Error(error, "GetReachableAddresses");
        }

        return result;
    }

}

using CryptoScanner.Core.Services;
using CryptoScanner.Services;
using CryptoScanner.ViewModels;
using CryptoScanner.Views;

using Microsoft.Extensions.DependencyInjection;

namespace CryptoScanner;

internal class MyServices
{
    public static void ConfigurePlatformServices(IServiceCollection services)
    {
        if (OperatingSystem.IsWindows())
            services.AddSingleton<IPlatformService, WindowsPlatformService>();
        else if (OperatingSystem.IsMacOS())
            services.AddSingleton<IPlatformService, MacOSPlatformService>();
        else if (OperatingSystem.IsLinux())
            services.AddSingleton<IPlatformService, LinuxPlatformService>();
        else
            throw new PlatformNotSupportedException($"Platform not supported: {Environment.OSVersion.Platform}");
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        ConfigurePlatformServices(services);

        // Register Services as Singleton
        services.AddSingleton<ApplicationStateService>();
        services.AddSingleton<ITradingViewService, TradingViewService>();
        services.AddSingleton<IJsonSerializerService, JsonSerializerService>();
        services.AddSingleton<IScannerSession, ScannerSession>();
        services.AddSingleton<HiddenBrowserService>();

        // Register ViewModels as Transient (nieuwe instantie bij elke aanvraag)
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<DashBoardInformationViewModel>();
        services.AddTransient<DashboardPositionsViewModel>();
        services.AddTransient<SymbolGridViewModel>();
        services.AddTransient<SignalGridViewModel>();
        services.AddTransient<LiveDataGridViewModel>();
        services.AddTransient<PositionOpenGridViewModel>();
        services.AddTransient<PositionClosedGridViewModel>();
        services.AddTransient<BrowserViewModel>();
        services.AddTransient<LogViewModel>();

        // Register Views
        services.AddTransient<MainWindow>();
    }

}

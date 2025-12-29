using CryptoScanner.Browser.ViewModels;
using CryptoScanner.DashBoard.Services;
using CryptoScanner.DashBoard.ViewModels;
using CryptoScanner.Log.ViewModels;
using CryptoScanner.Signal.ViewModels;
using CryptoScanner.Symbol.ViewModels;
using CryptoScanner.ViewModels;
using CryptoScanner.Views;
using CryptoScanner.Services;

using Microsoft.Extensions.DependencyInjection;
using CryptoScanner.LiveData.ViewModels;

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

        //services.AddSingleton<HiddenBrowserService>();

        // Register ViewModels as Transient (nieuwe instantie bij elke aanvraag)
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<DashBoardViewModel>();
        services.AddTransient<SymbolGridViewModel>();
        services.AddTransient<SignalGridViewModel>();
        services.AddTransient<LiveDataGridViewModel>();
        services.AddTransient<BrowserViewModel>();
        services.AddTransient<LogViewModel>();

        //services.AddTransient<IDialogService, DialogService>(); // Als DialogService parameterless constructor heeft
        //services.AddTransient<IDialogService>(provider => new DialogService(mainWindow));


        // Register Views
        services.AddTransient<MainWindow>();
    }

}

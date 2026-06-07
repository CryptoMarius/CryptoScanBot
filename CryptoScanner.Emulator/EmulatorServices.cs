using CryptoScanner.Core.Services;

using Microsoft.Extensions.DependencyInjection;

namespace CryptoScanner.Emulator;

/// <summary>
/// DI container setup for the emulator app. Mirrors <c>CryptoScanner.MyServices</c> in the
/// live scanner but registers only what the emulator actually needs: platform abstractions,
/// secure-string protection (consumed by <c>SecureStringConverter</c> the moment we
/// (de)serialise settings.json that contains credentials), and JSON serialization defaults.
/// Anything tied to the live scanner pipeline — ScannerSession, HiddenBrowserService,
/// TradingView, websocket monitors — is deliberately absent.
/// </summary>
internal static class EmulatorServices
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        // Platform-specific data-directory lookup. SetupWindow already used this for the
        // folder picker, but several Core services pull it from DI later (most prominently
        // ApplicationStateService in the scanner — not used here but the pattern stays).
        if (OperatingSystem.IsWindows())
            services.AddSingleton<IPlatformService, WindowsPlatformService>();
        else if (OperatingSystem.IsMacOS())
            services.AddSingleton<IPlatformService, MacOSPlatformService>();
        else if (OperatingSystem.IsLinux())
            services.AddSingleton<IPlatformService, LinuxPlatformService>();
        else
            throw new PlatformNotSupportedException($"Platform not supported: {Environment.OSVersion.Platform}");

        // SecureStringConverter is constructed by System.Text.Json's type-info pipeline as
        // soon as a property tagged with [JsonConverter(typeof(SecureStringConverter))] is
        // touched (e.g. Telegram.Token during SaveConfiguration). Without an IStringProtector
        // the ctor throws — that was the InvalidOperationException at the call site.
        if (OperatingSystem.IsWindows())
            services.AddSingleton<IStringProtectorService, WindowsStringProtectorService>();
        else if (OperatingSystem.IsMacOS())
            services.AddSingleton<IStringProtectorService, MacStringProtectorService>();
        else if (OperatingSystem.IsLinux())
            services.AddSingleton<IStringProtectorService, LinuxStringProtectorService>();
        else
            throw new PlatformNotSupportedException($"Platform not supported: {Environment.OSVersion.Platform}");

        services.AddSingleton<IJsonSerializerService, JsonSerializerService>();

        return services.BuildServiceProvider();
    }
}

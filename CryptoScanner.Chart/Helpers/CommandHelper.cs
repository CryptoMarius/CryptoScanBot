using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Helpers;

/// <summary>
/// Thin forwarder to <see cref="Core.Helpers.ExternalLinkHelper"/>. The implementation moved to
/// Core so the Blazor hosts share it; this type stays so every existing call site (scanner
/// commands, chart window, emulator) keeps compiling and behaving exactly the same.
/// </summary>
public static class CommandHelper
{
    // Host-supplied browser launchers. The scanner wires these to its App.OpenInInternalBrowser /
    // App.OpenInHiddenBrowser at startup; the emulator (which has no embedded browser) leaves them
    // null, so the internal/hidden path falls back to the system browser there.
    public static Action<string, bool>? OpenInternalBrowser
    {
        get => Core.Helpers.ExternalLinkHelper.OpenInternalBrowser;
        set => Core.Helpers.ExternalLinkHelper.OpenInternalBrowser = value;
    }

    public static Action<string>? OpenHiddenBrowser
    {
        get => Core.Helpers.ExternalLinkHelper.OpenHiddenBrowser;
        set => Core.Helpers.ExternalLinkHelper.OpenHiddenBrowser = value;
    }

    public static void ActivateTradingApp(CryptoTradingApp tradingApp,
        CryptoSymbol symbol, CryptoInterval interval, CryptoExternalUrlType viaTradingBrowser, bool activateTab = true)
    {
        Core.Helpers.ExternalLinkHelper.ActivateTradingApp(tradingApp, symbol, interval, viaTradingBrowser, activateTab);
    }
}

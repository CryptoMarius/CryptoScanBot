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
    // Host-supplied browser launcher. The scanner wires this to its App.OpenInInternalBrowser at
    // startup; the emulator (which has no embedded browser) leaves it null, so the internal path
    // falls back to the system browser there. (OpenHiddenBrowser went with the hidden browser
    // window on 11-10-2026; the Altrady socket replaced it, see ExternalLinkHelper.OpenTradingAppLink.)
    public static Action<string, bool>? OpenInternalBrowser
    {
        get => Core.Helpers.ExternalLinkHelper.OpenInternalBrowser;
        set => Core.Helpers.ExternalLinkHelper.OpenInternalBrowser = value;
    }

    public static void ActivateTradingApp(CryptoTradingApp tradingApp,
        CryptoSymbol symbol, CryptoInterval interval, CryptoExternalUrlType viaTradingBrowser, bool activateTab = true)
    {
        Core.Helpers.ExternalLinkHelper.ActivateTradingApp(tradingApp, symbol, interval, viaTradingBrowser, activateTab);
    }
}

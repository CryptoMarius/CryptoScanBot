using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Helpers;

/// <summary>
/// Opens the configured weblink for a symbol in the trading application of choice.
/// <para>
/// This used to live in the Avalonia chart project, which meant the Blazor hosts had their own
/// (different) copy: "internal" opened the system browser there and the hidden-browser path was a
/// bare HTTP GET that cannot carry the Altrady session. Both hosts now share this implementation
/// and only supply their own browser launchers.
/// </para>
/// </summary>
public static class ExternalLinkHelper
{
    // Host supplied browser launchers. The Avalonia scanner wires these to its embedded WebView
    // tabs, the Photino/Web hosts wire them to their own browser tab; the emulator (which has no
    // embedded browser) leaves them null, so the internal/hidden path is a no-op there while the
    // external-browser path still works.
    public static Action<string, bool>? OpenInternalBrowser { get; set; }
    public static Action<string>? OpenHiddenBrowser { get; set; }

    /// <summary>Open a url in the system browser.</summary>
    public static void OpenSystemBrowser(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception error)
        {
            GlobalData.AddErrorToLogTab($"Could not open {url}: {error.Message}");
        }
    }

    /// <summary>
    /// Point the internal browser at BTC against the given quote, so the tab is not empty on
    /// startup. Silently does nothing when the symbol does not exist yet.
    /// </summary>
    public static void ActivateStartupSymbol(string quote)
    {
        if (GlobalData.ActiveExchange == null)
            return;
        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval30m, out CryptoInterval? interval))
            return;
        if (!GlobalData.ActiveExchange.SymbolListName.TryGetValue("BTC" + quote, out CryptoSymbol? symbol))
            return;

        ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.Internal, false);
    }

    public static void ActivateTradingApp(CryptoTradingApp tradingApp,
        CryptoSymbol symbol, CryptoInterval interval, CryptoExternalUrlType viaTradingBrowser, bool activateTab = true)
    {
        // Activate the trading application (and we use a dummy browser for Altrady)
        GlobalData.LoadWebLinkConfiguration(); // refresh links
        (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(tradingApp, false, symbol, interval);
        if (Url == "")
        {
            // BUGFIX: silent return previously hid mis-configured weblinks. Surface it so
            // the user can tell whether the symbol open failed because of a missing URL or
            // because of the browser launch itself (e.g. WebView2 init in Release).
            GlobalData.AddTextToLogTab($"Linktools: no URL configured for tradingApp={tradingApp} exchange={GlobalData.Settings.General.ActivateExchangeName} symbol={symbol.Name}");
        }
        if (Url != "")
        {
            GlobalData.AddTextToLogTab($"Linktools activate {Url}");

            // Open the url via our own hidden browser (to avoid the Altrady jump-step)
            if (viaTradingBrowser == CryptoExternalUrlType.Internal)
            {
                OpenInternalBrowser?.Invoke(Url, activateTab);
            }
            else
            {
                if (Execute == CryptoExternalUrlType.Internal)
                {
                    OpenHiddenBrowser?.Invoke(Url);
                }
                else
                {
                    // Open via the external (system) browser
                    OpenSystemBrowser(Url);
                }
            }
        }
    }
}

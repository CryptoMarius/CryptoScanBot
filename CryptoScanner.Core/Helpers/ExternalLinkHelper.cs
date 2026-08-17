using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
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
    /// Point the internal browser at bitcoin, so the tab is not empty on startup.
    /// <para>
    /// The name is looked up twice. First "BTC" plus the quote the user is looking at, because that
    /// follows the barometer selection. When that does not exist, the exchange's own
    /// <see cref="ExchangeOptions.PauseSymbol"/> is used - the same coin, under the name that market
    /// gives it: XBTUSDT on Kucoin Futures, UBTCUSDC on HyperLiquid Spot, BTCUSD on Kraken Futures.
    /// Only the first lookup existed before, so on those markets the startup symbol silently found
    /// nothing and the browser tab stayed empty for the whole session.
    /// </para>
    /// <para>
    /// Every way out now says why in the log. The previous silent returns made it impossible to tell
    /// a market without a matching name apart from one where the symbol list was not loaded yet.
    /// </para>
    /// </summary>
    public static void ActivateStartupSymbol(string quote)
    {
        if (GlobalData.ActiveExchange == null)
        {
            GlobalData.AddTextToLogTab("Linktools: no startup symbol, there is no active exchange");
            return;
        }
        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval30m, out CryptoInterval? interval))
        {
            GlobalData.AddTextToLogTab("Linktools: no startup symbol, the 30m interval is not loaded");
            return;
        }

        string wanted = "BTC" + quote;
        if (!GlobalData.ActiveExchange.SymbolListName.TryGetValue(wanted, out CryptoSymbol? symbol))
        {
            string pauseSymbol = ExchangeBase.ExchangeOptions.PauseSymbol;
            if (pauseSymbol != "" && pauseSymbol != wanted)
                GlobalData.ActiveExchange.SymbolListName.TryGetValue(pauseSymbol, out symbol);

            if (symbol == null)
            {
                GlobalData.AddTextToLogTab($"Linktools: no startup symbol, neither {wanted} nor {pauseSymbol} " +
                    $"is in the symbol list of {GlobalData.ActiveExchange.Name} ({GlobalData.ActiveExchange.SymbolListName.Count} symbols)");
                return;
            }
        }

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

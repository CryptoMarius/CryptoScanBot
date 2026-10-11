using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Exchange.Altrady;
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
    // Host supplied browser launcher. The Avalonia scanner wires this to its embedded WebView
    // tab, the Photino/Web hosts wire it to their own browser tab; the emulator (which has no
    // embedded browser) leaves it null, so the internal path is a no-op there while the
    // external-browser path still works.
    // Until 11-10-2026 there was a second launcher here, OpenHiddenBrowser, for an invisible
    // browser window that loaded the Altrady deep link so its redirect would bring the desktop
    // application to the front. The local WebSocket of Altrady made that window superfluous
    // (see OpenTradingAppLink), so both hosts dropped it.
    public static Action<string, bool>? OpenInternalBrowser { get; set; }

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

        // A bare pair; the symbol list is keyed on the product-suffixed name
        string wanted = "BTC" + quote;
        if (!GlobalData.ActiveExchange.TryGetSymbolByPair(wanted, out CryptoSymbol? symbol))
        {
            string pauseSymbol = ExchangeBase.ExchangeOptions.PauseSymbol;
            if (pauseSymbol != "" && pauseSymbol != wanted)
                GlobalData.ActiveExchange.TryGetSymbolByPair(pauseSymbol, out symbol);

            if (symbol == null)
            {
                GlobalData.AddTextToLogTab($"Linktools: no startup symbol, neither {wanted} nor {pauseSymbol} " +
                    $"is in the symbol list of {GlobalData.ActiveExchange.Name} ({GlobalData.ActiveExchange.SymbolListName.Count} symbols)");
                return;
            }
        }

        ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.Internal, false);
    }

    /// <summary>
    /// Hand an address to the trading application. An Altrady deep link is sent over the local
    /// WebSocket of the desktop application (<see cref="AltradyDeepLink"/>), so the user sees no
    /// browser at all; when that does not answer (Altrady is not running, or an older version
    /// without the socket), or the address is not an Altrady link at all, the system browser
    /// gets it. The web link then takes the detour past the Altrady website, which hands over to
    /// the desktop application when it runs and otherwise opens the web version.
    /// </summary>
    public static void OpenTradingAppLink(string url)
    {
        AltradyDeepLink.Open(url, OpenSystemBrowser);
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
            // At error level, so a click that cannot go anywhere also lands in the error log
            // instead of only scrolling past in the log tab.
            GlobalData.AddErrorToLogTab($"Linktools: no URL configured for tradingApp={tradingApp} exchange={GlobalData.Settings.General.ActivateExchangeName} symbol={symbol.Name}");
        }
        if (Url != "")
        {
            // Which of the three routes below is taken, decided here so the log line can name it.
            // From the outside they are indistinguishable: the hidden browser sits off-screen on
            // purpose and the system browser hands the address to another application, so a click
            // that ends up somewhere unexpected looks exactly like a click that did nothing at all.
            // (The hidden browser is gone since 11-10-2026; the Altrady socket took its place and
            // the system browser is the fallback, see OpenTradingAppLink.)
            bool useInternalBrowser = viaTradingBrowser == CryptoExternalUrlType.Internal;
            bool useTradingAppLink = !useInternalBrowser && Execute == CryptoExternalUrlType.Internal;
            string route = useInternalBrowser ? "internal browser"
                : useTradingAppLink ? "Altrady socket, else the system browser"
                : "system browser";
            GlobalData.AddTextToLogTab($"Linktools activate {tradingApp} {symbol.Name} {interval.Name} via the {route}: {Url}");

            // Open the url via our own hidden browser (to avoid the Altrady jump-step)
            if (useInternalBrowser)
            {
                // A host that wired no browser at all used to swallow the request without a word
                if (OpenInternalBrowser == null)
                    GlobalData.AddErrorToLogTab($"Linktools: this application has no internal browser, {Url} was not opened");
                else
                    OpenInternalBrowser.Invoke(Url, activateTab);
            }
            else
            {
                if (useTradingAppLink)
                {
                    // Since 06-10-2026 the Altrady deep link goes over the local WebSocket of the
                    // desktop application first; the system browser is the fallback (see OpenTradingAppLink)
                    OpenTradingAppLink(Url);
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

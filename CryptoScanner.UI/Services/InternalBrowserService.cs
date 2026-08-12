using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Helpers;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Services;

namespace CryptoScanner.UI.Services;

/// <summary>
/// The Blazor counterpart of the Avalonia embedded browser tab. Hosts wire
/// <see cref="ExternalLinkHelper.OpenInternalBrowser"/> to <see cref="Navigate"/> so that every
/// "open internally" request (grid context menus, dashboard indicators, chart window) ends up on
/// the Tradingview tab instead of spawning a system browser window.
/// </summary>
public class InternalBrowserService : IDisposable
{
    private readonly ApplicationStateService _stateService;
    private bool _startupSymbolActivated;

    public InternalBrowserService(ApplicationStateService stateService)
    {
        _stateService = stateService;
    }

    /// <summary>Url currently shown in the internal browser tab, or null when nothing was requested yet.</summary>
    public string? CurrentUrl { get; private set; }

    /// <summary>Raised with (url, switchTab) whenever a navigation is requested.</summary>
    public event Action<string, bool>? NavigateRequested;

    /// <summary>
    /// Set by the host when it can open a real browser window instead of an in-page frame. The
    /// Photino host does; it opens a second window with its own WebView, which is the only way to
    /// reach a signed-in TradingView with the user's own indicators.
    /// </summary>
    public Action<string>? OpenBrowserWindow { get; set; }

    public void Navigate(string url, bool switchTab)
    {
        if (string.IsNullOrEmpty(url))
            return;

        // A real window loads the page as a normal navigation, so it needs the full address rather
        // than the anonymous widget the iframe had to fall back on
        if (OpenBrowserWindow != null)
        {
            url = WithTheme(url);
            CurrentUrl = url;

            // switchTab false is the silent activation the application does at startup. A tab could
            // be filled in the background; a window would jump in front of the user unasked, so it
            // only opens when the request came from the user.
            if (switchTab)
                OpenBrowserWindow(url);
            return;
        }

        url = ToEmbeddableUrl(url);
        CurrentUrl = url;
        NavigateRequested?.Invoke(url, switchTab);
    }

    /// <summary>
    /// Hand the scanner's dark/light choice to the page. Opening a TradingView chart from a dark
    /// application into its default light page is a jolt, and the widget path below has always done
    /// this - the window path skipped it because it deliberately keeps the full address.
    /// <para>
    /// Only added when the address does not already carry a theme, so a link that was configured
    /// with one of its own keeps it. Once signed in TradingView follows the account setting, which
    /// then overrides this again.
    /// </para>
    /// </summary>
    private static string WithTheme(string url)
    {
        if (url.Contains("theme=", StringComparison.OrdinalIgnoreCase))
            return url;

        string theme = ThemeHelper.ToCssTheme(GlobalData.Settings.General.Theme);
        if (string.IsNullOrEmpty(theme))
            return url;

        return url + (url.Contains('?') ? "&" : "?") + "theme=" + theme;
    }

    /// <summary>
    /// The internal browser is an iframe here, not a full WebView as in Avalonia, and
    /// www.tradingview.com refuses to be framed (X-Frame-Options), which left an empty tab saying
    /// "refused to connect". TradingView publishes s.tradingview.com/widgetembed for exactly this
    /// purpose, so a chart link is rewritten to that. Any other address is passed through.
    /// </summary>
    private static string ToEmbeddableUrl(string url)
    {
        if (url.IndexOf("tradingview.com/chart", StringComparison.OrdinalIgnoreCase) < 0)
            return url;

        string symbol = ReadQueryValue(url, "symbol");
        if (string.IsNullOrEmpty(symbol))
            return url;

        string theme = ThemeHelper.ToCssTheme(GlobalData.Settings.General.Theme);
        string result = "https://s.tradingview.com/widgetembed/?symbol=" + Uri.EscapeDataString(symbol)
            + "&hidesidetoolbar=0&symboledit=1&saveimage=0&allow_symbol_change=1&locale=en"
            + "&theme=" + theme;

        // The chart url carries the interval as "interval=60"; keep it when it is there
        string interval = ReadQueryValue(url, "interval");
        if (!string.IsNullOrEmpty(interval))
            result += "&interval=" + Uri.EscapeDataString(interval);

        return result;
    }

    private static string ReadQueryValue(string url, string name)
    {
        int start = url.IndexOf('?');
        if (start < 0)
            return "";

        foreach (string pair in url[(start + 1)..].Split('&'))
        {
            int equals = pair.IndexOf('=');
            if (equals > 0 && pair[..equals].Equals(name, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(pair[(equals + 1)..]);
        }
        return "";
    }

    /// <summary>
    /// Hook this service into the shared link helper. Called once at host startup.
    /// </summary>
    public void Register()
    {
        ExternalLinkHelper.OpenInternalBrowser = Navigate;

        // There is no hidden WebView here, so the "hidden browser" targets (Altrady deep links)
        // go to the system browser. That browser does carry the user's Altrady session — unlike
        // the plain HttpClient GET this host used before, which could never authenticate.
        ExternalLinkHelper.OpenHiddenBrowser = ExternalLinkHelper.OpenSystemBrowser;

        // The Avalonia MainWindow points the browser at BTC+quote as soon as it opens. Here the
        // symbols are not loaded yet at startup, so wait for the first symbols-loaded message.
        WeakReferenceMessenger.Default.Register<SymbolsHaveChangedMessage>(this, (_, _) => ActivateStartupSymbol());
    }

    private void ActivateStartupSymbol()
    {
        if (_startupSymbolActivated)
            return;
        _startupSymbolActivated = true;

        try
        {
            ExternalLinkHelper.ActivateStartupSymbol(_stateService.BarometerQuote);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "ActivateStartupSymbol");
        }
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        GC.SuppressFinalize(this);
    }
}

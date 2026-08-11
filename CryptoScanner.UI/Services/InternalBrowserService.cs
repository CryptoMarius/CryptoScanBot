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

    public void Navigate(string url, bool switchTab)
    {
        if (string.IsNullOrEmpty(url))
            return;

        url = ToEmbeddableUrl(url);
        CurrentUrl = url;
        NavigateRequested?.Invoke(url, switchTab);
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

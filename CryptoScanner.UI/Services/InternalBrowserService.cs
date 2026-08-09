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

        CurrentUrl = url;
        NavigateRequested?.Invoke(url, switchTab);
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

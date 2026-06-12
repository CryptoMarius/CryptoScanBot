using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Helpers;

public static class CommandHelper
{
    // Host-supplied browser launchers. The scanner wires these to its App.OpenInInternalBrowser /
    // App.OpenInHiddenBrowser at startup; the emulator (which has no embedded browser) leaves them
    // null, so the internal/hidden path is a no-op there while the external-browser path still works.
    // Keeps this shared chart project free of a hard dependency on the scanner's App.
    public static Action<string, bool>? OpenInternalBrowser;
    public static Action<string>? OpenHiddenBrowser;

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
            //App.EventOpenInInternalBrowser?.Invoke(this, Url);

            // Open the url via our own hidden browser (to avoid the Altrady jump-step)
            if (viaTradingBrowser == CryptoExternalUrlType.Internal)
            {
                //await WebViewTradingView.ActivateUrlAsync(Url);
                //if (activateTab && TabControl != null)
                //    TabControl.SelectedTab = TabPageBrowser;
                // Usage anywhere:
                //App.OpenInHiddenBrowser(Url);
                OpenInternalBrowser?.Invoke(Url, activateTab);
            }
            else
            {
                if (Execute == CryptoExternalUrlType.Internal)
                {
                    // Send url-event via the MainWindowViewModel
                    //EventOpenInInternalBrowser?.Invoke(this, Url);
                    //App.OpenInInternalBrowser(commandBase, Url);
                    OpenHiddenBrowser?.Invoke(Url);
                }
                else
                {
                    // Open via the external (system) browser
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Url) { UseShellExecute = true });
                }
            }
        }
    }

}
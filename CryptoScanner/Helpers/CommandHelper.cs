using CryptoScanner.Commands;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Helpers;

public static class CommandHelper
{
    public static void ActivateTradingApp(CryptoTradingApp tradingApp,
        CryptoSymbol symbol, CryptoInterval interval, CryptoExternalUrlType viaTradingBrowser, bool activateTab = true)
    {
        // Activate the trading application (and we use a dummy browser for Altrady)
        GlobalData.LoadWebLinkSettings(); // refresh links
        (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(tradingApp, false, symbol, interval);
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
                App.OpenInInternalBrowser(Url, activateTab);
            }
            else
            {
                if (Execute == CryptoExternalUrlType.Internal)
                {
                    // Send url-event via the MainWindowViewModel
                    //EventOpenInInternalBrowser?.Invoke(this, Url);
                    //App.OpenInInternalBrowser(commandBase, Url);
                    App.OpenInHiddenBrowser(Url);
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
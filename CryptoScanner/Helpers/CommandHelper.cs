using CryptoScanner.Commands;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Helpers;

public static class CommandHelper
{
    public static void ActivateTradingApp(this CommandBase commandBase, CryptoTradingApp externalTradingApp, CryptoSymbol symbol, CryptoInterval interval, CryptoExternalUrlType viaTradingBrowser, bool activateTab = true)
    {
        // Activate the trading application (and we use a dummy browser for AltradyStandard)

        (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(externalTradingApp, false, symbol, interval);
        if (Url != "")
        {
            GlobalData.AddTextToLogTab($"Linktools activate {Url}");
            //App.EventOpenInInternalBrowser?.Invoke(this, Url);
            App.OpenInInternalBrowser(commandBase, Url);

            //// Open the url via our own hidden browser (to avoid the AltradyStandard jump-step)
            //if (viaTradingBrowser == CryptoExternalUrlType.Internal)
            //{
            //    //await WebViewTradingView.ActivateUrlAsync(Url);
            //    //if (activateTab && TabControl != null)
            //    //    TabControl.SelectedTab = TabPageBrowser;
            //    // Usage anywhere:
            //    App.HiddenBrowser.Navigate(Url);
            //}
            //else
            //{
            //    if (Execute == CryptoExternalUrlType.Internal)
            //    {
            //        // Send url-event via the MainWindowViewModel
            //        EventOpenInInternalBrowser?.Invoke(this, Url);
            //    }
            //    else
            //    {
            //        // Open via the external (system) browser
            //        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Url) { UseShellExecute = true });
            //    }
            //}
        }
    }

}
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Intern;


public static class LinkTools
{
    public static TabControl? TabControl = null;
    public static TabPage? TabPageBrowser = null;

    public static WebBrowser WebViewTradingApp = new();
    public static WebBrowser WebViewTradingView = new();


    public static async void ActivateTradingApp(CryptoTradingApp externalTradingApp, CryptoSymbol symbol, CryptoInterval interval, CryptoExternalUrlType viaTradingBrowser, bool activateTab = true)
    {
        // Activate the trading application (and we use a dummy browser for Altrady)

        (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(externalTradingApp, false, symbol, interval);
        if (Url != "")
        {
            GlobalData.AddTextToLogTab($"Linktools activate {Url}");
            if (viaTradingBrowser == CryptoExternalUrlType.Internal)
            {
                await WebViewTradingView.ActivateUrlAsync(Url);
                if (activateTab && TabControl != null)
                    TabControl.SelectedTab = TabPageBrowser;
            }
            else
            {
                if (Execute == CryptoExternalUrlType.Internal)
                {
                    await WebViewTradingApp.ActivateUrlAsync(Url);
                }
                else
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Url) { UseShellExecute = true });
                }
            }
        }
    }


    public static void ActivateExternalTradingApp(CryptoTradingApp externalTradingApp, CryptoSymbol symbol, CryptoInterval interval)
    {
        // Activeer de externe applicatie (soms gebruik makend van de dummy browser)
        GlobalData.LoadLinkSettings(); // refresh links
        ActivateTradingApp(externalTradingApp, symbol, interval, CryptoExternalUrlType.External);
    }


    public static void InitializeTradingView()
    {
        if (WebViewTradingView != null)
        {
            CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m];
            if (GlobalData.Settings.General.Exchange!.SymbolListName.TryGetValue("BTCUSDT", out CryptoSymbol? symbol))
                ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.Internal, false);
        }
    }

}

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;

using Microsoft.Web.WebView2.Core;

namespace CryptoSbmScanner.Intern;


public static class LinkTools
{
    private static bool WebViewDummyInitialized;
    private static bool WebViewTradingViewInitialized;

    public static TabControl tabControl;
    public static TabPage tabPageBrowser;

    public static Microsoft.Web.WebView2.WinForms.WebView2 _webViewDummy;
    public static Microsoft.Web.WebView2.WinForms.WebView2 webViewTradingView;


    private static async Task InitializeWebView(Microsoft.Web.WebView2.WinForms.WebView2 webView2)
    {
        // https://stackoverflow.com/questions/63404822/how-to-disable-cors-in-wpf-webview2
        var userPath = GlobalData.GetBaseDir();
        CoreWebView2Environment cwv2Environment = await CoreWebView2Environment.CreateAsync(null, userPath, new CoreWebView2EnvironmentOptions());
        await webView2.EnsureCoreWebView2Async(cwv2Environment);
    }

    private static async Task InitializeWebViewDummy()
    {
        if (!WebViewDummyInitialized)
        {
            WebViewDummyInitialized = true;
            await InitializeWebView(_webViewDummy);
        }
    }


    private static async Task InitializeWebViewTradingView()
    {
        if (!WebViewTradingViewInitialized)
        {
            WebViewTradingViewInitialized = true;
            await InitializeWebView(webViewTradingView);
        }
    }


    public static async void ActivateExternalTradingApp(CryptoTradingApp externalTradingApp, CryptoSymbol symbol, CryptoInterval interval)
    {
        // Activeer de externe applicatie (soms gebruik makend van de dummy browser)

        (string Url, CryptoExternalUrlType Execute) refInfo = GlobalData.ExternalUrls.GetExternalRef(externalTradingApp, false, symbol, interval);
        if (refInfo.Url != "")
        {

            if (refInfo.Execute == CryptoExternalUrlType.Internal)
            {
                GlobalData.AddTextToLogTab($"Linktools activate external app (via internal browser) {refInfo.Url}");
                await InitializeWebViewDummy();
                _webViewDummy.Source = new(refInfo.Url);
            }
            else
            {
                GlobalData.AddTextToLogTab($"Linktools activate external app (via execute command) {refInfo.Url}");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(refInfo.Url) { UseShellExecute = true });
            }
        }
    }


    public static async void ActivateInternalTradingApp(CryptoTradingApp externalTradingApp, CryptoSymbol symbol, CryptoInterval interval, bool activateTab = true)
    {
        // Activeer de interne Tradingview Browser op het zoveelste tabblad

        (string Url, CryptoExternalUrlType Execute) refInfo = GlobalData.ExternalUrls.GetExternalRef(externalTradingApp, false, symbol, interval);
        if (refInfo.Url != "")
        {
            await InitializeWebViewTradingView();

            webViewTradingView.Source = new(refInfo.Url);
            if (activateTab)
                tabControl.SelectedTab = tabPageBrowser;
        }
    }


    public static void ActivateTradingViewBrowser(string symbolname = "BTCUSDT")
    {
        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval interval))
            return;

        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolname, out CryptoSymbol symbol))
                ActivateInternalTradingApp(CryptoTradingApp.TradingView, symbol, interval, false);
        }
    }

}

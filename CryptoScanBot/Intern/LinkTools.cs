using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Microsoft.Web.WebView2.Core;

namespace CryptoScanBot.Intern;

public static class LinkTools
{
    private static bool WebViewDummyInitialized;
    private static bool WebViewTradingViewInitialized;

    public static TabControl? TabControl = null;
    public static TabPage? TabPageBrowser = null;

    public static Microsoft.Web.WebView2.WinForms.WebView2? WebViewDummy;
    public static Microsoft.Web.WebView2.WinForms.WebView2? WebViewTradingView;


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
            await InitializeWebView(WebViewDummy!);
        }
    }


    private static async Task InitializeWebViewTradingView()
    {
        if (WebViewTradingView != null && !WebViewTradingViewInitialized)
        {
            WebViewTradingViewInitialized = true;
            await InitializeWebView(WebViewTradingView);
        }
    }


    public static async void ActivateTradingApp(CryptoTradingApp externalTradingApp, CryptoSymbol symbol, CryptoInterval interval, CryptoExternalUrlType viaTradingBrowser, bool activateTab = true)
    {
        // Activeer de applicatie (soms gebruik makend van de dummy browser)

        (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(externalTradingApp, false, symbol, interval);
        if (Url != "")
        {
            GlobalData.AddTextToLogTab($"Linktools activate {Url}");
            if (viaTradingBrowser == CryptoExternalUrlType.Internal)
            {
                await InitializeWebViewTradingView();

                if (WebViewTradingView != null)
                    WebViewTradingView.Source = new(Url);
                if (activateTab && TabControl != null)
                    TabControl.SelectedTab = TabPageBrowser;
            }
            else
            {
                if (Execute == CryptoExternalUrlType.Internal)
                {
                    await InitializeWebViewDummy();
                    if (WebViewDummy != null)
                        WebViewDummy.Source = new(Url);
                }
                else
                {
                    //GlobalData.AddTextToLogTab($"Linktools activate external app (via execute command) {Url}");
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

using CryptoScanBot.Core.Core;

using Microsoft.Web.WebView2.Core;

namespace CryptoScanBot.Intern;


public class WebBrowser
{
    public bool BrowserInitialized { get; set; }
    public Microsoft.Web.WebView2.WinForms.WebView2? Browser { get; set; }

    public async Task InitializeBrowserAsync()
    {
        if (Browser != null && !BrowserInitialized)
        {
            BrowserInitialized = true;

            // https://stackoverflow.com/questions/63404822/how-to-disable-cors-in-wpf-webview2
            var userPath = GlobalData.GetBaseDir();
            CoreWebView2Environment cwv2Environment = await CoreWebView2Environment.CreateAsync(null, userPath, new CoreWebView2EnvironmentOptions());
            await Browser.EnsureCoreWebView2Async(cwv2Environment);
        }
    }

    public async Task ActivateUrlAsync(string url)
    {
        await InitializeBrowserAsync();
        if (Browser != null)
            Browser.Source = new(url);
    }
}


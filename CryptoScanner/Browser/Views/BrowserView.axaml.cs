using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia;

using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Handlers;
using Xilium.CefGlue;

using CryptoScanner.Browser.ViewModels;

namespace CryptoScanner.Browser.Views
{
    public partial class BrowserView : UserControl
    {
        private AvaloniaCefBrowser? _browser;
        private BrowserViewModel? _viewModel;

        public BrowserView()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            // Unsubscribe from old ViewModel
            if (_viewModel != null)
            {
                _viewModel.NavigateRequested -= OnNavigateRequested;
            }

            // Subscribe to new ViewModel
            if (DataContext is BrowserViewModel vm)
            {
                _viewModel = vm;
                _viewModel.NavigateRequested += OnNavigateRequested;

                // Initialize browser if not done yet
                if (_browser == null)
                    InitializeBrowser();
                
                // Navigate to initial URL
                if (_browser != null && !string.IsNullOrEmpty(_viewModel.CurrentUrl))
                    _browser.Address = _viewModel.CurrentUrl;
            }
        }

        private void InitializeBrowser()
        {
            var browserWrapper = this.FindControl<Decorator>("browserWrapper");
            if (browserWrapper == null)
                return;

            _browser = new AvaloniaCefBrowser
            {
                Address = _viewModel?.CurrentUrl ?? "https://www.tradingview.com"
            };
            _browser.LoadStart += OnBrowserLoadStart;
            _browser.LifeSpanHandler = new BrowserLifeSpanHandler();

            browserWrapper.Child = _browser;
        }

        private void OnNavigateRequested(object? sender, string url)
        {
            _browser?.Address = url;
        }

        //private void OnReloadRequested(object? sender, System.EventArgs e)
        //{
        //    _browser?.Reload();
        //}

        private void OnBrowserLoadStart(object? sender, Xilium.CefGlue.Common.Events.LoadStartEventArgs e)
        {
            if (e.Frame.Browser.IsPopup || !e.Frame.IsMain)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                _viewModel?.UpdateUrl(e.Frame.Url);
            });
        }

        public void Dispose()
        {
            if (_viewModel != null)
            {
                _viewModel.NavigateRequested -= OnNavigateRequested;
            }

            _browser?.Dispose();
        }

        private class BrowserLifeSpanHandler : LifeSpanHandler
        {
            protected override bool OnBeforePopup(
                CefBrowser browser,
                CefFrame frame,
                string targetUrl,
                string targetFrameName,
                CefWindowOpenDisposition targetDisposition,
                bool userGesture,
                CefPopupFeatures popupFeatures,
                CefWindowInfo windowInfo,
                ref CefClient client,
                CefBrowserSettings settings,
                ref CefDictionaryValue extraInfo,
                ref bool noJavascriptAccess)
            {
                var bounds = windowInfo.Bounds;
                Dispatcher.UIThread.Post(() =>
                {
                    var window = new Window();
                    var popupBrowser = new AvaloniaCefBrowser
                    {
                        Address = targetUrl
                    };
                    window.Content = popupBrowser;
                    window.Position = new PixelPoint(bounds.X, bounds.Y);
                    window.Height = bounds.Height;
                    window.Width = bounds.Width;
                    window.Title = targetUrl;
                    window.Show();
                });
                return true;
            }
        }
    }
}

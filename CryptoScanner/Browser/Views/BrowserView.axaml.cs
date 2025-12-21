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
        
            // BELANGRIJK: Initialize browser bij Loaded event
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("BrowserView Loaded event");

            // Initialize browser als het nog niet bestaat
            if (_browser == null)
            {
                InitializeBrowser();
            }
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("BrowserView DataContextChanged");

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

                System.Diagnostics.Debug.WriteLine($"ViewModel attached, CurrentUrl: {_viewModel.CurrentUrl}");

                // Navigate naar initial URL als browser al bestaat
                if (_browser != null && !string.IsNullOrEmpty(_viewModel.CurrentUrl))
                {
                    System.Diagnostics.Debug.WriteLine($"Navigating to initial URL: {_viewModel.CurrentUrl}");
                    _browser.Address = _viewModel.CurrentUrl;
                }
            }
        }

        private void InitializeBrowser()
        {
            var browserWrapper = this.FindControl<Decorator>("browserWrapper");
            if (browserWrapper == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: browserWrapper not found!");
                return;
            }

            System.Diagnostics.Debug.WriteLine("Initializing CefBrowser...");

            try
            {
                _browser = new AvaloniaCefBrowser();

                // Set initial address
                var initialUrl = _viewModel?.CurrentUrl ?? "https://www.tradingview.com";
                System.Diagnostics.Debug.WriteLine($"Setting browser address to: {initialUrl}");
                _browser.Address = initialUrl;

                // Subscribe to events
                _browser.LoadEnd += OnBrowserLoadEnd;
                _browser.LoadError += OnBrowserLoadError;
                _browser.LifeSpanHandler = new BrowserLifeSpanHandler();

                // Add to UI
                browserWrapper.Child = _browser;

                System.Diagnostics.Debug.WriteLine($"Browser initialized successfully. IsInitialized: {_browser.IsInitialized}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR initializing browser: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void OnNavigateRequested(object? sender, string url)
        {
            System.Diagnostics.Debug.WriteLine($"OnNavigateRequested: {url}");

            if (_browser == null)
            {
                System.Diagnostics.Debug.WriteLine("WARNING: Browser is null, initializing now...");
                InitializeBrowser();
            }

            if (_browser != null)
            {
                System.Diagnostics.Debug.WriteLine($"Setting browser.Address to: {url}");
                _browser.Address = url;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Browser still null after initialization attempt!");
            }
        }

        private void OnBrowserLoadEnd(object? sender, Xilium.CefGlue.Common.Events.LoadEndEventArgs e)
        {
            if (e.Frame.Browser.IsPopup || !e.Frame.IsMain)
                return;

            System.Diagnostics.Debug.WriteLine($"LoadEnd: {e.Frame.Url} (HttpStatusCode: {e.HttpStatusCode})");
        }

        private void OnBrowserLoadError(object? sender, Xilium.CefGlue.Common.Events.LoadErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"LoadError: {e.ErrorCode} - {e.ErrorText} for URL: {e.FailedUrl}");
        }

        public void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("BrowserView Dispose");

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
                System.Diagnostics.Debug.WriteLine($"Popup requested: {targetUrl}");

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
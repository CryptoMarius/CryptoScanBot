using Avalonia.Controls;
using Avalonia.Threading;

using AvaloniaWebView;


namespace CryptoScanner.Views
{
    /// <summary>
    /// Browser view using official Avalonia.Controls.WebView
    /// Uses platform-native browsers:
    /// - Windows: WebView2 (Edge/Chromium)
    /// - macOS: WKWebView (Safari)
    /// - Linux: WebKitGTK
    /// </summary>
    public partial class BrowserView : UserControl
    {
        private WebView? _webView;
        private bool _isInitializing;
        private string? _pendingUrl;

        public BrowserView()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("BrowserView created (WebView NOT initialized yet)");
        }

        /// <summary>
        /// Navigate to URL - creates browser on first call
        /// </summary>
        public void Navigate(string url)
        {
            System.Diagnostics.Debug.WriteLine($"Navigate called: {url}");

            // If browser exists, just navigate
            if (_webView != null)
            {
                System.Diagnostics.Debug.WriteLine("WebView exists, navigating directly");

                // Navigate using Uri
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    _webView.Url = uri;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid URL: {url}");
                }
                return;
            }

            // Browser doesn't exist yet - create it
            System.Diagnostics.Debug.WriteLine("WebView doesn't exist, initializing now...");
            _pendingUrl = url;

            if (!_isInitializing)
            {
                _isInitializing = true;
                InitializeBrowserLazy();
            }
        }

        private void InitializeBrowserLazy()
        {
            System.Diagnostics.Debug.WriteLine("InitializeBrowserLazy starting...");

            // Force tab to become visible first
            // Find parent TabControl and switch to this tab
            var parent = this.Parent;
            while (parent != null)
            {
                if (parent is TabControl tabControl)
                {
                    // Find which tab contains this view
                    for (int i = 0; i < tabControl.Items.Count; i++)
                    {
                        var tabItem = tabControl.Items[i] as TabItem;
                        if (tabItem?.Content == this || IsChildOf(tabItem?.Content, this))
                        {
                            System.Diagnostics.Debug.WriteLine($"Switching to tab {i} to make browser visible");
                            tabControl.SelectedIndex = i;
                            break;
                        }
                    }
                    break;
                }
                parent = parent.Parent;
            }

            // Wait a bit for tab to become visible
            Dispatcher.UIThread.Post(() =>
            {
                CreateBrowser();
            }, DispatcherPriority.Background);
        }

        private static bool IsChildOf(object? potentialParent, Control child)
        {
            if (potentialParent == child) return true;
            if (potentialParent is Panel panel)
            {
                foreach (var c in panel.Children)
                {
                    if (c == child) return true;
                }
            }
            return false;
        }

        private void CreateBrowser()
        {
            System.Diagnostics.Debug.WriteLine("CreateBrowser starting...");

            try
            {
                var browserWrapper = this.FindControl<Decorator>("browserWrapper");
                if (browserWrapper == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: browserWrapper not found!");
                    _isInitializing = false;
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Creating official Avalonia WebView instance...");

                // Create Avalonia.Controls.WebView
                _webView = new WebView();

                // Subscribe to navigation events
                //_webView.NavigationStarted += WebView_NavigationStarted2;
                //_webView.NavigationCompleted += WebView_NavigationCompleted2;

                // Navigate to URL if we have one
                if (!string.IsNullOrEmpty(_pendingUrl))
                {
                    if (Uri.TryCreate(_pendingUrl, UriKind.Absolute, out var uri))
                    {
                        _webView.Url = uri;
                    }
                }

                browserWrapper.Child = _webView;

                System.Diagnostics.Debug.WriteLine($"WebView created successfully, navigated to: {_pendingUrl}");
                _isInitializing = false;
                _pendingUrl = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR creating WebView: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                _isInitializing = false;

                // Show error message in the browser wrapper
                ShowError(ex.Message);
            }
        }

        //private void WebView_NavigationStarted2(object? sender, WebViewNavigationStartingEventArgs e)
        //{
        //    System.Diagnostics.Debug.WriteLine($"WebView navigation starting: {e.Uri}");
        //}

        //private void WebView_NavigationCompleted(object? sender, NavigationCompletedEventArgs e)
        //{
        //    System.Diagnostics.Debug.WriteLine($"WebView navigation completed: {e.Url}");
        //}

        private void ShowError(string errorMessage)
        {
            var browserWrapper = this.FindControl<Decorator>("browserWrapper");
            if (browserWrapper != null)
            {
                var textBlock = new TextBlock
                {
                    Text = $"⚠️ Browser Error\n\n{errorMessage}\n\nRequested URL: {_pendingUrl}",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(20),
                    FontSize = 14
                };
                browserWrapper.Child = textBlock;
            }
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

using AvaloniaWebView;

using CryptoScanner.ViewModels;

namespace CryptoScanner.Views
{
    public partial class BrowserView : UserControl
    {
        private WebView? _webView;
        private bool _isInitializing;
        private string? _pendingUrl;

        public BrowserView()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("BrowserView created");
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is BrowserViewModel vm)
            {
                vm.NavigateRequested -= OnNavigateRequested;
                vm.NavigateRequested += OnNavigateRequested;
            }
        }

        private void OnNavigateRequested(object? sender, string url)
        {
            Navigate(url);
        }

        public void Navigate(string url)
        {
            System.Diagnostics.Debug.WriteLine($"Navigate called: {url}");

            if (_webView != null)
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    _webView.Url = uri;

                return;
            }

            _pendingUrl = url;

            if (!_isInitializing)
            {
                _isInitializing = true;
                Dispatcher.UIThread.Post(CreateBrowser, DispatcherPriority.Background);
            }
        }

        private void CreateBrowser()
        {
            try
            {
                var wrapper = this.FindControl<Decorator>("browserWrapper");
                if (wrapper == null)
                {
                    System.Diagnostics.Debug.WriteLine("browserWrapper not found!");
                    return;
                }

                _webView = new WebView();

                if (!string.IsNullOrEmpty(_pendingUrl) &&
                    Uri.TryCreate(_pendingUrl, UriKind.Absolute, out var uri))
                {
                    _webView.Url = uri;
                }

                wrapper.Child = _webView;

                System.Diagnostics.Debug.WriteLine("WebView created successfully");

                _pendingUrl = null;
                _isInitializing = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Browser error: {ex.Message}");
                ShowError(ex.Message);
                _isInitializing = false;
            }
        }

        private void ShowError(string errorMessage)
        {
            var wrapper = this.FindControl<Decorator>("browserWrapper");
            if (wrapper != null)
            {
                wrapper.Child = new TextBlock
                {
                    Text = $"Browser error:\n{errorMessage}",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Thickness(20)
                };
            }
        }
    }
}

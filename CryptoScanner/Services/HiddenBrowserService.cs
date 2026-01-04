using Avalonia.Controls;
using Avalonia.Threading;

using CryptoScanner.Views;

using System.Diagnostics;

namespace CryptoScanner.Services;

/// <summary>
/// Hidden browser service for handling URL redirects (e.g., Altrady OAuth)
/// </summary>
public class HiddenBrowserService : IDisposable
{
    private BrowserView? _hiddenBrowser;
    private Window? _hiddenWindow;
    private bool _isInitialized;

    /// <summary>
    /// Initialize the hidden browser
    /// Call this once at startup
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                // Create hidden window (1x1 pixel, invisible)
                _hiddenWindow = new Window
                {
                    Width = 1,
                    Height = 1,
                    ShowInTaskbar = false,
                    Opacity = 0,
                    CanResize = false,
                    SystemDecorations = SystemDecorations.None
                };

                // Create hidden browser
                _hiddenBrowser = new BrowserView();

                // Subscribe to navigation events
                //_hiddenBrowser.LoadStart += OnLoadStart;
                //_hiddenBrowser.LoadEnd += OnLoadEnd;

                _hiddenWindow.Content = _hiddenBrowser;
                
                // Show window (but it's invisible)
                _hiddenWindow.Show();
                
                // Immediately hide from view
                _hiddenWindow.WindowState = Avalonia.Controls.WindowState.Minimized;

                _isInitialized = true;
                Debug.WriteLine("HiddenBrowserService initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR initializing HiddenBrowserService: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Navigate to URL and capture redirects
    /// </summary>
    /// <param name="url">URL to navigate to</param>
    public void Navigate(string url)
    {
        if (!_isInitialized)
        {
            Debug.WriteLine("WARNING: HiddenBrowserService not initialized, initializing now...");
            Initialize();
        }

        Dispatcher.UIThread.Post(() =>
        {
            Debug.WriteLine($"HiddenBrowser navigating to: {url}");
            _hiddenBrowser?.Navigate(url);
        });
    }


    //private void OnLoadStart(object? sender, Xilium.CefGlue.Common.Events.LoadStartEventArgs e)
    //{
    //    Debug.WriteLine($"HiddenBrowser LoadStart: {e.Frame.Url}");
    //}

    //private void OnLoadEnd(object? sender, Xilium.CefGlue.Common.Events.LoadEndEventArgs e)
    //{
    //    Debug.WriteLine($"HiddenBrowser LoadEnd: {e.Frame.Url}");
    //}

    public void Dispose()
    {
        Debug.WriteLine("HiddenBrowserService Dispose");
        
        //if (_hiddenBrowser != null)
        //{
        //    _hiddenBrowser.LoadStart -= OnLoadStart;
        //    _hiddenBrowser.LoadEnd -= OnLoadEnd;
        //    _hiddenBrowser.Dispose();
        //}

        _hiddenWindow?.Close();
        _isInitialized = false;
        GC.SuppressFinalize(this);
    }
}

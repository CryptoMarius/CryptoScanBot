using Avalonia.Controls;
using Avalonia.Threading;

using AvaloniaWebView;
//https://github.com/MicroSugarDeveloperOrg/Webviews.Avalonia

namespace CryptoScanner.Services;

/// <summary>
/// Hidden browser service for background operations
/// Uses official Avalonia.Controls.WebView with platform-native browser engines
/// </summary>
public class HiddenBrowserService : IDisposable
{
    private WebView? _webView;
    private Window? _hiddenWindow;
    private bool _disposed;

    public void Initialize()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("HiddenBrowserService: Initializing WebView...");

            //// Create official Avalonia WebView
            //_webView = new WebView();

            ////// Subscribe to events
            ////_webView.NavigationStarting += (s, e) =>
            ////{
            ////    System.Diagnostics.Debug.WriteLine($"HiddenBrowserService: Navigation starting: {e.Url}");
            ////};

            ////_webView.NavigationCompleted += (s, e) =>
            ////{
            ////    System.Diagnostics.Debug.WriteLine($"HiddenBrowserService: Navigation completed: {e.Url}");
            ////};

            //System.Diagnostics.Debug.WriteLine("HiddenBrowserService: WebView initialized successfully");
            // Run op UI-thread om visual tree te initialiseren
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Creëer de hidden window
                _hiddenWindow = new Window
                {
                    Width = 800, // Geef het een minimale size voor rendering
                    Height = 600,
                    WindowState = WindowState.Minimized, // Of gebruik Position voor off-screen
                    // Position = new PixelPoint(-10000, -10000), // Alternatief: Off-screen verplaatsen
                    ShowInTaskbar = false, // Niet in taskbar tonen
                    CanResize = false,
                    Title = "Hidden Browser"
                };

                // Create official Avalonia WebView
                _webView = new WebView();

                // Set WebView als content van de window
                _hiddenWindow.Content = _webView;

                // Show de window (nodig voor init), maar houd het hidden/minimized
                _hiddenWindow.Show();
                _hiddenWindow.Hide(); // Direct verbergen na show, of houd minimized

                System.Diagnostics.Debug.WriteLine("HiddenBrowserService: WebView initialized successfully");
            }).Wait(); // Wacht synchroon als je niet async bent (pas aan als nodig)

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HiddenBrowserService: Failed to initialize WebView: {ex.Message}");
        }
    }

    public void Navigate(string url)
    {
        if (_webView == null)
        {
            System.Diagnostics.Debug.WriteLine("HiddenBrowserService: WebView not initialized, initializing now...");
            Initialize();
        }

        if (_webView != null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"HiddenBrowserService: Navigating to {url}");

                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    // Update UI op UI thread (in Avalonia gebruik Dispatcher)
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        _webView.Url = uri;
                    });

                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"HiddenBrowserService: Invalid URL: {url}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HiddenBrowserService: Navigation error: {ex.Message}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"HiddenBrowserService: Failed to navigate to {url} - WebView is null");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        System.Diagnostics.Debug.WriteLine("HiddenBrowserService: Disposing...");


        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_hiddenWindow != null)
            {
                _hiddenWindow.Close();
                _hiddenWindow = null;
            }

            // Unsubscribe events
            if (_webView != null)
            {
                // Clear any event handlers if needed
                _webView = null;
            }
        }).Wait();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

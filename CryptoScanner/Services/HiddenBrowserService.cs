using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

using AvaloniaWebView;

using CryptoScanner.Core.Core;
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

    // BUGFIX: previously every log line in this class went through System.Diagnostics.Debug.WriteLine,
    // which is marked [Conditional("DEBUG")] — the C# compiler STRIPS those calls from Release builds.
    // Result: in Release, any WebView2/Altrady-launch failure was silently swallowed inside the catch
    // blocks below, producing the "works in Debug, does nothing in Release" symptom. Route everything
    // through ScannerLog (NLog) + AddTextToLogTab so failures are visible in both configurations.
    //
    // NOTE on log level: ScannerLog.InitializeLogging only registers the Trace target under
    // #if DEBUG. Using Logger.Trace(...) here would silently disappear in Release just like
    // Debug.WriteLine did. Use Info so the message lands in the "default" file target
    // (Info+) in both Debug and Release.
    private static void Log(string message)
    {
        ScannerLog.Logger.Info("HiddenBrowserService: " + message);
        System.Diagnostics.Debug.WriteLine("HiddenBrowserService: " + message);
    }

    private static void LogError(string message, Exception? ex = null)
    {
        if (ex != null)
        {
            ScannerLog.Logger.Error(ex, "HiddenBrowserService: " + message);
            GlobalData.AddTextToLogTab("HiddenBrowserService: " + message + " — " + ex.Message);
        }
        else
        {
            ScannerLog.Logger.Error("HiddenBrowserService: " + message);
            GlobalData.AddTextToLogTab("HiddenBrowserService: " + message);
        }
        System.Diagnostics.Debug.WriteLine("HiddenBrowserService: " + message + (ex != null ? " — " + ex.Message : ""));
    }

    public void Initialize()
    {
        try
        {
            Log("Initializing WebView...");

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
                try
                {
                    // Cre�er de hidden window
                    // BUGFIX (Release-only "Altrady opent niet"):
                    // Previously the window was constructed with WindowState.Minimized and then
                    // Show() + Hide() was called immediately. In Release the JIT-compiled code
                    // ran fast enough that Hide() destroyed the native HWND BEFORE WebView2's
                    // async init had completed → subsequent `_webView.Url = uri` had no platform
                    // host to draw onto and the navigation never reached the Altrady redirect
                    // step. Symptom in the log: "PopupRoot: PlatformImpl is null".
                    // In Debug the slower instruction timing happened to give WebView2 just
                    // enough time, masking the bug.
                    //
                    // Fix: keep the window in Normal state but parked far off-screen, with
                    // Topmost=false and ShowInTaskbar=false, so the native HWND stays alive
                    // for WebView2's lifetime while remaining invisible to the user. Drop the
                    // Hide() call entirely — Show() once is all we need.
                    _hiddenWindow = new Window
                    {
                        Width = 800, // Geef het een minimale size voor rendering
                        Height = 600,
                        Position = new PixelPoint(-32000, -32000), // Off-screen — voorkomt dat Hide() de native host sloopt
                        ShowInTaskbar = false, // Niet in taskbar tonen
                        CanResize = false,
                        SystemDecorations = SystemDecorations.None,
                        Title = "Hidden Browser"
                    };

                    // Create official Avalonia WebView
                    _webView = new WebView();

                    // Diagnostic event hooks — log every nav lifecycle event with Info-level
                    // so we can see in the Release log whether the WebView actually starts
                    // navigating, completes, or stays silent. Without this we are blind.
                    // Using e?.ToString() because the actual property layout of
                    // WebViewUrlLoadedEventArg is wrapper-version-specific; ToString returns
                    // enough to know the event fired and which URL it relates to.
                    _webView.NavigationStarting += (s, e) =>
                    {
                        try { Log($"NavigationStarting fired: {e}"); }
                        catch (Exception evtEx) { LogError("NavigationStarting handler threw", evtEx); }
                    };
                    _webView.NavigationCompleted += (s, e) =>
                    {
                        try { Log($"NavigationCompleted fired: {e}"); }
                        catch (Exception evtEx) { LogError("NavigationCompleted handler threw", evtEx); }
                    };

                    // Set WebView als content van de window
                    _hiddenWindow.Content = _webView;

                    // Show de window off-screen so WebView2 receives a valid native HWND it can
                    // keep using for the whole app lifetime. Do NOT call Hide() afterwards.
                    _hiddenWindow.Show();
                    _hiddenWindow.Position = new PixelPoint(-32000, -32000); // re-park after Show in case Position was reset

                    Log("WebView initialized successfully");
                }
                catch (Exception innerEx)
                {
                    LogError("Failed to create hidden window / WebView on UI thread", innerEx);
                    throw;
                }
            }).Wait(); // Wacht synchroon als je niet async bent (pas aan als nodig)

        }
        catch (Exception ex)
        {
            LogError("Failed to initialize WebView", ex);
        }
    }

    public void Navigate(string url)
    {
        if (_webView == null)
        {
            Log("WebView not initialized, initializing now...");
            Initialize();
        }

        if (_webView != null)
        {
            try
            {
                Log($"Navigating to {url}");

                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    // Update UI op UI thread (in Avalonia gebruik Dispatcher)
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            Log($"Pre-assign: _webView.Url current={_webView.Url}");
                            _webView.Url = uri;
                            Log($"Post-assign: _webView.Url is now={_webView.Url} (target was {uri})");
                        }
                        catch (Exception postEx)
                        {
                            LogError($"Failed to assign Url={uri}", postEx);
                        }
                    });

                }
                else
                {
                    LogError($"Invalid URL: {url}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Navigation error for url={url}", ex);
            }
        }
        else
        {
            LogError($"Failed to navigate to {url} — WebView is null after Initialize()");
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

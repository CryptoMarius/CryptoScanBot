using Avalonia.Controls;
using Avalonia.Threading;

using AvaloniaWebView;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;
using CryptoScanner.Services;

namespace CryptoScanner.Views;

/// <summary>
/// A window of its own holding nothing but a browser pointed at TradingView, the counterpart of the
/// Photino TradingViewWindow (since 08-11-2026).
/// <para>
/// It replaces the Tradingview tab. That tab kept a WebView alive inside the main window for the
/// whole session, shrunk to one pixel whenever another tab was selected because the browser needs
/// to stay visible to keep working, and it loaded a chart at startup so the tab would not be empty.
/// The window is only created on the first "open internally" request, and one window is reused:
/// a later request points it at the new address and brings it to the front.
/// </para>
/// </summary>
public static class TradingViewWindow
{
    /// <summary>The entry in the window file that remembers position, size and screen, the same name the Photino window uses.</summary>
    private const string WindowName = "TradingViewWindow";

    /// <summary>Only ever touched on the UI thread.</summary>
    private static Window? _window;
    private static WebView? _webView;

    /// <summary>
    /// Show the window, or point the existing one at another address. Safe to call from any thread.
    /// <paramref name="activate"/> false is the silent activation the application used to do at
    /// startup: a tab could be filled in the background, a window would jump in front of the user
    /// unasked, so then nothing opens (the same rule as the Photino host).
    /// </summary>
    public static void Show(string url, bool activate)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            GlobalData.AddErrorToLogTab($"TradingView window: not a valid address, {url} was not shown");
            return;
        }

        if (!activate)
        {
            GlobalData.AddTextToLogTab($"TradingView window: remembered without opening a window: {url}");
            return;
        }

        Dispatcher.UIThread.Post(() => ShowOnUiThread(uri));
    }

    private static void ShowOnUiThread(Uri uri)
    {
        try
        {
            if (_window != null && _webView != null)
            {
                GlobalData.AddTextToLogTab($"TradingView window navigates to: {uri}");
                _webView.Url = uri;
                if (_window.WindowState == Avalonia.Controls.WindowState.Minimized)
                    _window.WindowState = Avalonia.Controls.WindowState.Normal;
                // Already open but behind another window: without this it looked as if nothing happened
                _window.Activate();
                return;
            }

            GlobalData.AddTextToLogTab($"Opening the TradingView window: {uri}");

            var webView = new WebView { Url = uri };
            var window = new Window
            {
                Title = $"TradingView ({GlobalData.Settings.General.ExchangeName})",
                Width = 1400,
                Height = 900,
                CanResize = true,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = webView,
            };
            App.ApplyExchangeIcon(window);

            // Position, size, screen and maximized state come back from the last time, the way the
            // main window and the configuration window do it
            var stateService = GlobalData.GetService<ApplicationStateService>();
            if (stateService != null)
            {
                if (!string.IsNullOrEmpty(stateService.GetOrCreateWindowState(WindowName).State))
                    window.WindowStartupLocation = WindowStartupLocation.Manual;
                stateService.RestoreWindowState(WindowName, window);
                stateService.TrackWindowState(WindowName, window);
                window.Closing += (_, _) => stateService.SaveWindowState(WindowName, window);
            }

            window.Closed += (_, _) =>
            {
                _window = null;
                _webView = null;
            };

            _window = window;
            _webView = webView;
            window.Show();
        }
        catch (Exception error)
        {
            _window = null;
            _webView = null;
            ScannerLog.Logger.Error(error, "TradingViewWindow");
            GlobalData.AddTextToLogTab("Could not open the TradingView window: " + error.Message);
        }
    }

    /// <summary>Close the window if it is open. Called when the main window closes.</summary>
    public static void Close()
    {
        try
        {
            _window?.Close();
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "TradingViewWindow.Close");
        }
    }
}

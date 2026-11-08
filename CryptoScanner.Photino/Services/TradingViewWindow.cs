using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;

using Photino.NET;

namespace CryptoScanner.Photino.Services;

/// <summary>
/// A second window holding nothing but a browser pointed at TradingView.
/// <para>
/// It replaces the Tradingview tab, which was an iframe inside the one WebView the Blazor
/// application runs in. www.tradingview.com refuses to be framed, so that tab could only ever show
/// the anonymous s.tradingview.com/widgetembed widget: no account, no saved layouts and none of the
/// user's own indicators. A window of its own navigates there as a normal page, so signing in works
/// and everything that comes with the account is available.
/// </para>
/// <para>
/// Everything here happens on the thread that owns the main window, and that is not a detail.
/// PhotinoWindow.WaitForClose creates the native window and then runs a message loop, but only for
/// the FIRST window: it guards on a static _messageLoopIsStarted and returns immediately for every
/// window after that, leaving them to be pumped by the loop that is already running. A window
/// created on a thread of its own therefore had nothing pumping it, and a Win32 window dies with
/// the thread that created it - which is why it appeared for a moment and vanished again.
/// </para>
/// </summary>
public sealed class TradingViewWindow
{
    private const string DefaultUrl = "https://www.tradingview.com/chart/";

    /// <summary>The entry in the window file that remembers position, size and screen.</summary>
    private const string WindowName = "TradingViewWindow";

    private readonly PhotinoWindow _mainWindow;
    private readonly ApplicationStateService _stateService;

    /// <summary>Only ever touched on the main window's thread.</summary>
    private PhotinoWindow? _window;
    private WindowPlacement? _placement;

    public TradingViewWindow(PhotinoWindow mainWindow, ApplicationStateService stateService)
    {
        _mainWindow = mainWindow;
        _stateService = stateService;
    }

    /// <summary>
    /// Show the window, or point the existing one at another address. Safe to call from any thread.
    /// </summary>
    public void Show(string url = "")
    {
        if (string.IsNullOrEmpty(url))
            url = DefaultUrl;

        try
        {
            // Marshalled onto the main window's thread, which is where the message loop runs
            _mainWindow.Invoke(() => ShowOnMainThread(url));
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "TradingViewWindow.Show");
            GlobalData.AddTextToLogTab("Could not open the TradingView window: " + error.Message);
        }
    }

    /// <summary>Close the window if it is open. Called during shutdown.</summary>
    public void Close()
    {
        try
        {
            _mainWindow.Invoke(() =>
            {
                try { _window?.Close(); }
                catch { /* already gone */ }
                _window = null;
            });
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "TradingViewWindow.Close");
        }
    }

    private void ShowOnMainThread(string url)
    {
        try
        {
            if (_window != null)
            {
                // Same reason as the hidden browser: only the creation used to be logged, so every
                // later address change was invisible in the log.
                GlobalData.AddTextToLogTab($"TradingView window navigates to: {url}");
                _window.SetMinimized(false);
                _window.Load(new Uri(url));
                // Already open but behind another window: without this it looked as if nothing happened
                WindowChrome.BringToFront(_window);
                return;
            }

            // Deliberately NO SetTemporaryFilesPath. That is the WebView2 user data folder, and
            // every WebView in one process has to use the same one - asking for a second folder
            // makes the environment fail to create. The default is a fixed folder, so the login
            // survives a restart anyway, which was the only reason for wanting to set it.
            GlobalData.AddTextToLogTab($"Opening the TradingView window: {url}");

            var window = new PhotinoWindow()
                .SetTitle($"TradingView ({GlobalData.Settings.General.ExchangeName})")
                .ApplyIcon()
                // The same browser switches as the main window, or this window gets no browser
                .ApplyBrowserArguments()
                .SetResizable(true)
                .SetContextMenuEnabled(true)
                .SetDevToolsEnabled(false)
                .SetGrantBrowserPermissions(true);

            // Position, size, screen and maximized state come back from the last time. Until
            // 07-11-2026 the operating system picked a new place on every open.
            _placement = new WindowPlacement(window, WindowName, _stateService, 1400, 900);

            window.RegisterWindowClosingHandler((sender, e) =>
            {
                _placement?.Save();
                _placement = null;
                _window = null;
                return false; // false = do not cancel, let it close
            });

            _window = window;
            window.Load(new Uri(url));

            // Creates the native window. It does NOT block: the main window already started the
            // message loop, and Photino runs only one - see the note on the class above.
            window.WaitForClose();

            // The handle only exists once the window has been created, so this comes after it.
            WindowChrome.ApplyTitleBarTheme(window);
            // Same for the list of monitors, which the check on the restored position needs
            _placement?.AfterCreate();
        }
        catch (Exception error)
        {
            _window = null;
            _placement = null;
            ScannerLog.Logger.Error(error, "TradingViewWindow");
            GlobalData.AddTextToLogTab("Could not open the TradingView window: " + error.Message);
        }
    }
}

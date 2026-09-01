using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;

using Photino.NET;

namespace CryptoScanner.Photino.Services;

/// <summary>
/// An invisible window with a browser of its own, used for the deep links that hand a symbol to a
/// trading application (Altrady, Hypertrader).
/// <para>
/// Those links are ordinary https addresses (https://app.altrady.com/d/BINA_USDT_BTC:60) that only
/// redirect to the altrady:// protocol, which is what actually brings the desktop application to
/// the front. Sending them to the system browser works, but it leaves the user looking at the
/// Altrady website with the application opening behind it - the extra step this window removes.
/// This is the same trick the Avalonia host has always used (HiddenBrowserService), and it needs a
/// real browser rather than an http request because the redirect page depends on the Altrady
/// session, which lives in the WebView2 profile.
/// </para>
/// <para>
/// The window is parked far off-screen instead of being hidden. A window that is genuinely hidden
/// gives the browser engine every reason to suspend the page, and the redirect is exactly what must
/// not be suspended; off-screen it is a normal, visible window as far as the engine is concerned.
/// On Windows the taskbar button is then removed separately (see <see cref="KeepOutOfTheTaskbar"/>).
/// </para>
/// <para>
/// Everything happens on the thread that owns the main window - see the note on
/// <see cref="TradingViewWindow"/> for why a window created on any other thread does not survive.
/// </para>
/// </summary>
public sealed class HiddenBrowserWindow
{
    /// <summary>Well outside any monitor, but still inside the range Windows accepts (SHRT_MIN).</summary>
    private const int OffScreenPosition = -32000;

    private readonly PhotinoWindow _mainWindow;

    /// <summary>Only ever touched on the main window's thread.</summary>
    private PhotinoWindow? _window;

    public HiddenBrowserWindow(PhotinoWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    /// <summary>
    /// Load the address in the invisible browser, creating it on first use. Safe to call from any
    /// thread.
    /// </summary>
    public void Navigate(string url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        try
        {
            // Marshalled onto the main window's thread, which is where the message loop runs
            _mainWindow.Invoke(() => NavigateOnMainThread(url));
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "HiddenBrowserWindow.Navigate");
            GlobalData.AddTextToLogTab("Could not open the hidden browser: " + error.Message);
        }
    }

    /// <summary>Close the window if it was created. Called during shutdown.</summary>
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
            ScannerLog.Logger.Error(error, "HiddenBrowserWindow.Close");
        }
    }

    private void NavigateOnMainThread(string url)
    {
        try
        {
            if (_window != null)
            {
                // The window only announces itself when it is created, so without this line every
                // click after the first one left no trace at all - and this window is invisible, so
                // the log is the only place the navigation can be seen.
                GlobalData.AddTextToLogTab($"Hidden browser navigates to: {url}");
                _window.Load(new Uri(url));
                return;
            }

            // Deliberately NO SetTemporaryFilesPath, for the same reason as the TradingView window:
            // every WebView in one process has to share the one user data folder, which is set
            // process wide in Program.Main. Sharing it is what gives this window the Altrady session.
            GlobalData.AddTextToLogTab($"Opening the hidden browser: {url}");

            var window = new PhotinoWindow()
                .SetTitle($"CryptoScanBot trading app launcher ({GlobalData.Settings.General.ExchangeName})")
                .SetUseOsDefaultSize(false)
                .SetSize(800, 600)
                .SetUseOsDefaultLocation(false)
                .SetLeft(OffScreenPosition)
                .SetTop(OffScreenPosition)
                .SetResizable(false)
                .SetContextMenuEnabled(false)
                .SetDevToolsEnabled(false)
                .SetGrantBrowserPermissions(true);

            window.RegisterWindowClosingHandler((sender, e) =>
            {
                _window = null;
                return false; // false = do not cancel, let it close
            });

            // The taskbar button can only be removed once the native window exists. It is done from
            // the created handler and once more below, because which of the two runs first depends
            // on the Photino version; both are harmless when the style is already right.
            window.RegisterWindowCreatedHandler((_, _) => KeepOutOfTheTaskbar(window));

            _window = window;
            window.Load(new Uri(url));

            // Creates the native window. It does NOT block: the main window already started the
            // message loop, and Photino runs only one - see the note on TradingViewWindow.
            window.WaitForClose();

            KeepOutOfTheTaskbar(window);
        }
        catch (Exception error)
        {
            _window = null;
            ScannerLog.Logger.Error(error, "HiddenBrowserWindow");
            GlobalData.AddTextToLogTab("Could not open the hidden browser: " + error.Message);
        }
    }

    /// <summary>
    /// Take the window out of the taskbar and out of Alt-Tab. Windows only; elsewhere the window is
    /// merely off-screen. The window styles this needs live in the platform service, because the
    /// Avalonia host has the very same off-screen browser window and needs the very same treatment.
    /// </summary>
    private static void KeepOutOfTheTaskbar(PhotinoWindow window)
    {
        try
        {
            // refreshWindow: the window is already on screen (off-screen, but shown), so the shell
            // has made its taskbar button and only drops it when the window is shown again.
            GlobalData.GetService<IPlatformService>()?.KeepWindowOutOfTheTaskbar(window.WindowHandle, true);
        }
        catch (Exception error)
        {
            // A stray taskbar button is not worth failing the navigation over
            ScannerLog.Logger.Error(error, "HiddenBrowserWindow.KeepOutOfTheTaskbar");
        }
    }
}

namespace CryptoScanner.Core.Services;

public interface IPlatformService
{
    string GetDataDirectory();

    /// <summary>
    /// Paint the title bar of a native window to match the application theme. Windows draws that bar
    /// itself and keeps it white until the window asks for the dark variant, which looks broken next
    /// to a dark application. Hosts that do not draw their own title bar (or platforms that theme it
    /// for you) inherit the empty implementation below and do nothing.
    /// </summary>
    /// <param name="windowHandle">The native window handle (HWND on Windows).</param>
    /// <param name="dark">True for a dark title bar, false for the light one.</param>
    void ApplyWindowTheme(nint windowHandle, bool dark)
    {
    }

    /// <summary>
    /// Take a window out of the taskbar and out of the Alt-Tab list. Meant for the helper windows
    /// that have to stay alive off-screen (the browser that hands a symbol to the trading
    /// application): the user cannot switch to them - they are nowhere near the screen - so an entry
    /// per running scanner is only in the way. Platforms that do not make the distinction inherit
    /// the empty implementation below and do nothing.
    /// </summary>
    /// <param name="windowHandle">The native window handle (HWND on Windows).</param>
    /// <param name="refreshWindow">
    /// Hide the window and show it again, which is what makes the shell drop a taskbar button it has
    /// already created. Leave this false for a window that has not been shown yet, or one the host
    /// keeps out of the taskbar by itself: hiding a window with a browser in it can suspend the page.
    /// </param>
    void KeepWindowOutOfTheTaskbar(nint windowHandle, bool refreshWindow)
    {
    }

    /// <summary>
    /// Show a blocking message box using whatever the platform provides. Meant for the startup
    /// path, where the application refuses to continue and there is no window yet to hang a
    /// dialog on. The fallback below writes to the console, which is all a host without a
    /// desktop can do.
    /// </summary>
    void ShowMessage(string title, string message)
    {
        Console.WriteLine($"{title}: {message}");
    }
}

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

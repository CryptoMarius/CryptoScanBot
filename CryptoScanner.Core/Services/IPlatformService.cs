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
    /// Bring a window of this application to the front: restore it when it is minimized and make it
    /// the active window. Used for a secondary window that is already open somewhere behind other
    /// windows when it is asked for again (the chart, TradingView). Hosts and platforms without a
    /// way to do that inherit the empty implementation and do nothing.
    /// </summary>
    /// <param name="windowHandle">The native window handle (HWND on Windows).</param>
    void BringWindowToFront(nint windowHandle)
    {
    }

    /// <summary>
    /// Show a picture as the icon of the running application, for platforms whose task bar shows
    /// the application rather than its windows: the Dock on macOS ignores the icon of a window.
    /// Windows and Linux take the icon from the window itself, so the default does nothing. May be
    /// called from any thread.
    /// </summary>
    /// <param name="png">The picture as a PNG.</param>
    void ApplyApplicationIcon(byte[] png)
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

    /// <summary>
    /// Open a file with the application the platform associates with it (an .xls in Excel or
    /// LibreOffice, for instance), the way a double click in the file manager would. The default
    /// leaves it to .NET's shell execute; the platforms that need their own way override it.
    /// Failures are reported, not thrown: the file itself is already written.
    /// </summary>
    /// <param name="path">The full path of the file.</param>
    void OpenFile(string path)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception error)
        {
            Console.WriteLine($"OpenFile {path}: {error.Message}");
        }
    }

    /// <summary>
    /// Run the current process below the normal priority, for batch work that should only get the
    /// processor time nobody else needs. The live scanners keep the normal priority, so whenever one
    /// of them (or its user interface) has work to do it goes first instead of queueing behind the
    /// batch threads. The default works on every platform .NET runs on (a priority class on Windows,
    /// a nice value on Linux and macOS); lowering is always allowed, so a failure is only reported.
    /// </summary>
    void LowerProcessPriority()
    {
        try
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            process.PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
        }
        catch (Exception error)
        {
            Console.WriteLine($"LowerProcessPriority: {error.Message}");
        }
    }
}

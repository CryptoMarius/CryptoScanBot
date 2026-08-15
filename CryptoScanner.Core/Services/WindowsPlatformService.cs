using CryptoScanner.Core.Core;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace CryptoScanner.Core.Services;

public class WindowsPlatformService : IPlatformService
{
    // Desktop Window Manager attribute that switches the title bar to its dark variant. It is 20 on
    // Windows 10 build 18985 and up (and on Windows 11); the builds before that used 19 for the same
    // thing, so a failed call is retried with the older number.
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBeforeBuild18985 = 19;

    // SetWindowPos flags: change nothing about the geometry, only ask for the frame to be redrawn.
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;

    [SupportedOSPlatform("windows")]
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint hwnd, nint hwndInsertAfter, int x, int y, int cx, int cy, uint flags);

    /// <summary>
    /// Switch the title bar between its light and dark variant.
    /// <para>
    /// Setting the attribute is not enough on a window that is already on screen: Windows only
    /// repaints the frame when something asks it to, so the bar would keep its old colour until the
    /// window was moved or resized. The SetWindowPos below is that request - it changes no geometry,
    /// it only carries SWP_FRAMECHANGED.
    /// </para>
    /// </summary>
    public void ApplyWindowTheme(nint windowHandle, bool dark)
    {
        if (windowHandle == 0 || !OperatingSystem.IsWindows())
            return;

        try
        {
            int value = dark ? 1 : 0;
            int result = DwmSetWindowAttribute(windowHandle, DwmwaUseImmersiveDarkMode, ref value, sizeof(int));
            if (result != 0)
                result = DwmSetWindowAttribute(windowHandle, DwmwaUseImmersiveDarkModeBeforeBuild18985, ref value, sizeof(int));
            if (result != 0)
                return;         // too old a Windows for either attribute, leave the bar alone

            SetWindowPos(windowHandle, 0, 0, 0, 0, 0,
                SwpNoSize | SwpNoMove | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
        }
        catch (Exception error)
        {
            // Cosmetic only - never let a missing or refusing dwmapi take the window down
            ScannerLog.Logger.Error(error, "ApplyWindowTheme");
        }
    }

    public string GetDataDirectory()
    {
        // Normally we store data in the user data folder under the name of the application
        var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // But we can overrule that via the -f parameter and that can be a partial or a full path
        ApplicationParams.InitApplicationOptions();
        var folder = ApplicationParams.Options?.AppDataFolder;
        if (string.IsNullOrEmpty(folder))
        {
            // This is the standard path
            return Path.Combine(baseFolder, Const.Constants.AppName);
        }
        else if (!Path.IsPathFullyQualified(folder))
        {
            // This is the standard path + folder parameter
            return Path.Combine(baseFolder, folder);
        }
        else
        {
            // This is a full path given by the parameter
            return folder;
        }
    }

}
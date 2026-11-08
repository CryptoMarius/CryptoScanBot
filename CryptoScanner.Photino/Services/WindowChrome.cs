using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;

using Photino.NET;

namespace CryptoScanner.Photino.Services;

/// <summary>
/// The bits of window decoration Photino leaves to the host: the icon in the title bar and on the
/// task bar, and the colour of the title bar itself.
/// <para>
/// The title bar is drawn by the operating system, not by the WebView, so no amount of CSS reaches
/// it. Windows keeps it white until a window explicitly asks for the dark variant, which is why a
/// dark application still had a white bar on top. The actual call lives in the platform service, so
/// this stays free of operating-system specifics.
/// </para>
/// </summary>
public static class WindowChrome
{
    /// <summary>
    /// The Chromium switches every WebView of this process starts with (see Program.cs for why).
    /// They have to be the same for EVERY window: all WebViews of one process share one WebView2
    /// environment in one data folder, and a window that asks for other switches does not get a
    /// browser at all - it stays empty without an error. That is what happened to the TradingView
    /// and chart windows once the main window got these switches (29-10-2026).
    /// </summary>
    public const string BrowserArguments = "--disable-renderer-backgrounding --disable-backgrounding-occluded-windows";

    /// <summary>Give a window the same browser start options as the main window. Windows only:
    /// on Linux the same Photino setting takes WebKit JSON.</summary>
    public static PhotinoWindow ApplyBrowserArguments(this PhotinoWindow window)
    {
        if (OperatingSystem.IsWindows())
            window.SetBrowserControlInitParameters(BrowserArguments);
        return window;
    }

    /// <summary>
    /// The application icon, copied next to the executable by the project file. Empty when it is
    /// missing, in which case the icon is simply not set - Photino throws on a path that is not
    /// there.
    /// </summary>
    public static string IconFile { get; } = ResolveIconFile();

    private static string ResolveIconFile()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "AppIcon.ico");
            return File.Exists(path) ? path : "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Give the window the application icon. Call this BEFORE the native window is created; it is a
    /// window option, not something that can be changed afterwards.
    /// </summary>
    public static PhotinoWindow ApplyIcon(this PhotinoWindow window)
    {
        string iconFile = ExchangeIconFile();
        if (iconFile.Length > 0)
        {
            try
            {
                window.SetIconFile(iconFile);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "WindowChrome.ApplyIcon");
            }
        }

        // The Dock on macOS shows the application, not the window
        AppIconBuilder.ApplyApplicationIcon();
        return window;
    }

    /// <summary>
    /// Follow a switch to another exchange. Once the native window exists Photino hands SetIconFile
    /// straight to it (Photino_SetIconFile), so this works at any time; before that it only
    /// replaces the icon the window will be created with.
    /// </summary>
    public static void UpdateIcon(PhotinoWindow? window)
    {
        if (window == null)
            return;

        window.ApplyIcon();
    }

    /// <summary>
    /// The application icon with the logo of the exchange on it (AppIconBuilder), written into the
    /// data folder because Photino only takes an icon by file name. Every exchange gets a file of its
    /// own: Windows caches an icon loaded from a file by that name, so overwriting one file would keep
    /// showing the old logo. Falls back to the plain icon next to the executable.
    /// </summary>
    private static string ExchangeIconFile()
    {
        string path = AppIconBuilder.WriteCurrent(Path.Combine(GlobalData.AppDataFolder, "Icons"));
        return path.Length > 0 ? path : IconFile;
    }

    /// <summary>
    /// Repaint the title bar in the colour of the current theme. Call this AFTER the native window
    /// exists (the handle is zero before that) and again whenever the theme changes.
    /// </summary>
    /// <summary>
    /// Bring an open window to the front (restored when minimized). Photino has no call for that;
    /// toggling topmost did not lift a window that sat behind another one.
    /// </summary>
    public static void BringToFront(PhotinoWindow? window)
    {
        if (window == null)
            return;

        try
        {
            GlobalData.GetService<IPlatformService>()?.BringWindowToFront(window.WindowHandle);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "WindowChrome.BringToFront");
        }
    }

    public static void ApplyTitleBarTheme(PhotinoWindow? window)
    {
        if (window == null)
            return;

        try
        {
            var platformService = GlobalData.GetService<IPlatformService>();
            if (platformService == null)
                return;

            // "Follow system" is answered by the browser, which cannot be asked until the page is
            // up - and that is well after this window exists. Painting the guess in the meantime is
            // what put a black bar on top of a light application, so leave the bar as Windows made
            // it (light) and wait: the layout broadcasts a theme change as soon as it knows.
            if (ThemeHelper.Normalize(GlobalData.Settings.General.Theme) == ThemeHelper.Default
                && !ThemeHelper.SystemPreferenceKnown)
                return;

            bool dark = ThemeHelper.ToCssTheme(GlobalData.Settings.General.Theme) == "dark";
            platformService.ApplyWindowTheme(window.WindowHandle, dark);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "WindowChrome.ApplyTitleBarTheme");
        }
    }
}

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
        if (IconFile.Length > 0)
        {
            try
            {
                window.SetIconFile(IconFile);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "WindowChrome.ApplyIcon");
            }
        }
        return window;
    }

    /// <summary>
    /// Repaint the title bar in the colour of the current theme. Call this AFTER the native window
    /// exists (the handle is zero before that) and again whenever the theme changes.
    /// </summary>
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

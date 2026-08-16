namespace CryptoScanner.Core.Core;

/// <summary>
/// Shared theme naming. Settings.General.Theme is written by every host, so all of them have to
/// agree on the spelling: the Avalonia host switches on "Light"/"Dark" and falls back to the
/// system default for anything else, while the Blazor hosts need a lower case value for the
/// <c>data-theme</c> attribute. Normalising here keeps a settings file written by one
/// application valid for the other.
/// </summary>
public static class ThemeHelper
{
    public const string Light = "Light";
    public const string Dark = "Dark";
    public const string Default = "Default";

    /// <summary>Map any stored spelling onto "Light", "Dark" or "Default".</summary>
    public static string Normalize(string? theme)
    {
        if (string.IsNullOrWhiteSpace(theme))
            return Default;

        if (theme.Equals(Light, StringComparison.OrdinalIgnoreCase))
            return Light;
        if (theme.Equals(Dark, StringComparison.OrdinalIgnoreCase))
            return Dark;
        return Default;
    }

    /// <summary>
    /// What the operating system asks for, for the "Follow system" setting. Avalonia resolves that
    /// itself through ThemeVariant.Default; the Blazor hosts have to ask the browser
    /// (prefers-color-scheme) and drop the answer here, so every screen resolves it the same way.
    /// Dark until told otherwise, which is what the application used to assume unconditionally.
    /// </summary>
    public static bool SystemPrefersDark { get; private set; } = true;

    /// <summary>
    /// False as long as <see cref="SystemPrefersDark"/> is still the starting guess above. The
    /// browser can only be asked once the page is up, so everything that runs before that - the
    /// title bar of the native window, for one - would otherwise paint a colour that is not based
    /// on anything. Such a caller can wait for the answer instead of guessing.
    /// </summary>
    public static bool SystemPreferenceKnown { get; private set; }

    /// <summary>
    /// The browser answered prefers-color-scheme (or reported that it changed). A method and not a
    /// setter, because it writes the flag above as well.
    /// </summary>
    public static void ReportSystemPreference(bool prefersDark)
    {
        SystemPrefersDark = prefersDark;
        SystemPreferenceKnown = true;
    }

    /// <summary>The value the Blazor layout puts in the data-theme attribute.</summary>
    public static string ToCssTheme(string? theme)
    {
        string normalized = Normalize(theme);
        if (normalized == Light)
            return "light";
        if (normalized == Dark)
            return "dark";
        // "Follow system"
        return SystemPrefersDark ? "dark" : "light";
    }

    /// <summary>Flip between light and dark, always returning a normalized value.</summary>
    public static string Toggle(string? theme)
    {
        return Normalize(theme) == Light ? Dark : Light;
    }
}

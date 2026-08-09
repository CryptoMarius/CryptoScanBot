namespace CryptoScanner.Core.Messages;

/// <summary>
/// The application theme was changed. Sent so a host can repaint immediately instead of waiting
/// for its next poll — the Avalonia host applies the theme straight from GlobalData.SetTheme, while
/// the Blazor hosts only re-read the setting on a timer, which made a theme switch look like the
/// application had stopped responding.
/// </summary>
public class ThemeChangedMessage
{
}

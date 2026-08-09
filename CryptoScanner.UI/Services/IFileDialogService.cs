namespace CryptoScanner.UI.Services;

/// <summary>
/// Opens the operating system file picker, the same way the Avalonia window does. The Blazor
/// components cannot do this themselves — a web view can only hand back the CONTENT of a file through
/// input[type=file], never its path — so the host application (Photino) supplies the implementation.
///
/// Resolve it through <see cref="IServiceProvider.GetService"/> rather than injecting it directly:
/// a host without a native window (the web host) simply does not register one, and the caller falls
/// back to its own in-page picker.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Shows a single-selection open-file dialog and returns the full path, or null when the user
    /// cancelled.
    /// </summary>
    /// <param name="title">Dialog caption.</param>
    /// <param name="initialPath">Folder to start in. Ignored when it does not exist.</param>
    /// <param name="filters">
    /// Filter entries as (description, extensions), e.g. ("Sound files", ["wav"]). Extensions are
    /// given without a leading dot.
    /// </param>
    string? OpenFile(string title, string? initialPath, params (string Description, string[] Extensions)[] filters);
}

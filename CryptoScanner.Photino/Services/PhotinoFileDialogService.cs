using CryptoScanner.UI.Services;

using Photino.Blazor;

namespace CryptoScanner.Photino.Services;

/// <summary>
/// Native implementation of <see cref="IFileDialogService"/>: forwards to the Photino window's own
/// open-file dialog, which is the operating system dialog — the same one the Avalonia host shows, so
/// the user can browse to any folder instead of picking from a fixed list.
/// </summary>
/// <remarks>
/// The window is resolved lazily through <paramref name="appAccessor"/>: the service is registered on
/// the service collection before the Photino application itself is built, so it cannot take the app as
/// a constructed dependency.
/// </remarks>
public sealed class PhotinoFileDialogService(Func<PhotinoBlazorApp> appAccessor) : IFileDialogService
{
    public string? OpenFile(string title, string? initialPath, params (string Description, string[] Extensions)[] filters)
    {
        // Photino wants an existing folder; a missing one makes the dialog open at an arbitrary place.
        string? startFolder = null;
        if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
            startFolder = initialPath;

        string[]? selected = appAccessor().MainWindow.ShowOpenFile(title, startFolder, false,
            filters.Select(f => (f.Description, f.Extensions)).ToArray());

        if (selected == null || selected.Length == 0)
            return null;

        string result = selected[0];
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CryptoScanner.Emulator.Views;

/// <summary>
/// Small read-only viewer that shows the JSON that was stored with an emulator run (the scanner
/// settings snapshot). Opened from the runs grid context menu for a single row. A Copy button
/// puts the JSON on the clipboard; Close (or Escape) dismisses the dialog.
/// </summary>
public partial class RunJsonWindow : Window
{
    private string _json = "";

    // Parameterless constructor for the XAML designer.
    public RunJsonWindow() : this("Run JSON", "")
    {
    }

    public RunJsonWindow(string title, string json)
    {
        InitializeComponent();
        Title = title;
        _json = json ?? "";
        JsonBox.Text = _json;

        CopyButton.Click += OnCopyClick;
        CloseButton.Click += (_, _) => Close();
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
            await clipboard.SetTextAsync(_json);
    }
}

using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Settings.ViewModels;

public partial class BlackAndWhiteListViewModel : ObservableObject
{
    [ObservableProperty]
    private string _text = string.Empty;


    internal void LoadConfig(List<string> settings)
    {
        Text = string.Join(Environment.NewLine, settings);
    }

    internal void SaveConfig(List<string> settings)
    {
        var lines = Text
            .Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line));

        settings.Clear();
        settings.AddRange(lines);
    }
}

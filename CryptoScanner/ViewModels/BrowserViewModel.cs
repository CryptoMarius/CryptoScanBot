using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CryptoScanner.ViewModels;

public partial class BrowserViewModel : ObservableObject
{
    [ObservableProperty]
    private string? currentUrl;

    public BrowserViewModel()
    {
        System.Diagnostics.Debug.WriteLine("BrowserViewModel created");
    }

    [RelayCommand]
    private void Navigate(string url)
    {
        CurrentUrl = url;
        NavigateRequested?.Invoke(this, url);
    }

    public event EventHandler<string>? NavigateRequested;
}

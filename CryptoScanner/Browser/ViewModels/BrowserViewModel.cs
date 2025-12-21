using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace CryptoScanner.Browser.ViewModels;

/// <summary>
/// ViewModel for the embedded browser
/// Manages URL navigation and browser state
/// </summary>
public partial class BrowserViewModel : ObservableObject
{
    [ObservableProperty]
    private string _currentUrl = "https://www.tradingview.com";

    public BrowserViewModel()
    {
        //System.Diagnostics.Debug.WriteLine("BrowserViewModel constructor called");
    }

    /// <summary>
    /// Event to request URL navigation in View
    /// </summary>
    public event EventHandler<string>? NavigateRequested;

    /// <summary>
    /// Navigate to URL
    /// </summary>
    [RelayCommand]
    private void Navigate(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        // Override for testing
        url = "https://github.com/OutSystems/CefGlue?tab=readme-ov-file";
        CurrentUrl = url;
        NavigateRequested?.Invoke(this, url);
    }

    /// <summary>
    /// Navigate to TradingView with symbol
    /// </summary>
    public void NavigateToTradingView(string url)
    {
        Navigate(url);
    }

}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Browser.Views;

using System;

namespace CryptoScanner.Browser.ViewModels;

/// <summary>
/// ViewModel for the embedded browser
/// Manages URL navigation and browser state
/// </summary>
public partial class BrowserViewModel : ObservableObject
{

    public BrowserView? BrowserView { get; set; }

    //[ObservableProperty]
    //private string _currentUrl = "https://www.google.com";

    //[ObservableProperty]
    //private string _pageTitle = "Browser";

    //[ObservableProperty]
    //private bool _isLoading;

    //[ObservableProperty]
    //private bool _canGoBack;

    //[ObservableProperty]
    //private bool _canGoForward;

    public BrowserViewModel()
    {
        System.Diagnostics.Debug.WriteLine("BrowserViewModel constructor called");
    }

    ///// <summary>
    ///// Event to request URL navigation in View
    ///// </summary>
    //public event EventHandler<string>? NavigateRequested;

    ///// <summary>
    ///// Event to request back navigation
    ///// </summary>
    //public event EventHandler? BackRequested;

    ///// <summary>
    ///// Event to request forward navigation
    ///// </summary>
    //public event EventHandler? ForwardRequested;

    ///// <summary>
    ///// Event to request reload
    ///// </summary>
    //public event EventHandler? ReloadRequested;

    ///// <summary>
    ///// Navigate to URL
    ///// </summary>
    //[RelayCommand]
    //private void Navigate(string? url)
    //{
    //    if (string.IsNullOrWhiteSpace(url))
    //        return;

    //    // Add https:// if missing
    //    //if (!url.StartsWith("http://") && !url.StartsWith("https://"))
    //    //    url = "https://" + url;

    //    CurrentUrl = url;
    //    NavigateRequested?.Invoke(this, url);
    //}

    ///// <summary>
    ///// Navigate to TradingView with symbol
    ///// </summary>
    //public void NavigateToTradingView(string symbol, string? interval = null)
    //{
    //    var url = $"https://www.tradingview.com/chart/?symbol={symbol}";
        
    //    if (!string.IsNullOrEmpty(interval))
    //        url += $"&interval={interval}";

    //    Navigate(url);
    //}

    ///// <summary>
    ///// Go back in history
    ///// </summary>
    //[RelayCommand(CanExecute = nameof(CanGoBack))]
    //private void GoBack()
    //{
    //    BackRequested?.Invoke(this, EventArgs.Empty);
    //}

    ///// <summary>
    ///// Go forward in history
    ///// </summary>
    //[RelayCommand(CanExecute = nameof(CanGoForward))]
    //private void GoForward()
    //{
    //    ForwardRequested?.Invoke(this, EventArgs.Empty);
    //}

    ///// <summary>
    ///// Reload current page
    ///// </summary>
    //[RelayCommand]
    //private void Reload()
    //{
    //    ReloadRequested?.Invoke(this, EventArgs.Empty);
    //}

    ///// <summary>
    ///// Navigate to home (TradingView)
    ///// </summary>
    //[RelayCommand]
    //private void GoHome()
    //{
    //    Navigate("https://www.tradingview.com");
    //}

    ///// <summary>
    ///// Update from View when URL changes
    ///// </summary>
    //public void UpdateUrl(string url)
    //{
    //    CurrentUrl = url;
    //}

    ///// <summary>
    ///// Update from View when title changes
    ///// </summary>
    //public void UpdateTitle(string title)
    //{
    //    PageTitle = title;
    //}

    ///// <summary>
    ///// Update from View when loading state changes
    ///// </summary>
    //public void UpdateLoadingState(bool loading)
    //{
    //    IsLoading = loading;
    //}

    ///// <summary>
    ///// Update from View when navigation state changes
    ///// </summary>
    //public void UpdateNavigationState(bool canGoBack, bool canGoForward)
    //{
    //    CanGoBack = canGoBack;
    //    CanGoForward = canGoForward;
        
    //    // Notify commands to update CanExecute
    //    GoBackCommand.NotifyCanExecuteChanged();
    //    GoForwardCommand.NotifyCanExecuteChanged();
    //}
}

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
    public BrowserViewModel()
    {
        System.Diagnostics.Debug.WriteLine("BrowserViewModel constructor called");
    }

}

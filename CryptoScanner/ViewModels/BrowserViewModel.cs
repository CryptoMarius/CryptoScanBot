using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.ViewModels;

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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;

using System.Collections.ObjectModel;
using System.Reflection;

namespace CryptoScanner.Settings.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = $"About {Constants.AppName}";

    [ObservableProperty]
    private string _version = $"Version {GlobalData.AppVersion}";

    [ObservableProperty]
    private string _copyright = $"{Constants.AppName} © {DateTime.Now.Year}";

    [ObservableProperty]
    private string _author = "Marius";

    [ObservableProperty]
    private ObservableCollection<string> _exchanges = [];

    [ObservableProperty]
    private RsiViewModel _rsiViewModel;

    public SettingsViewModel()
    {
        // Initialize child view models
        _rsiViewModel = new RsiViewModel();
    }

    //private void LoadCopyright()
    //{
    //    // Get copyright from assembly
    //    var assembly = Assembly.GetExecutingAssembly();
    //    var copyrightAttr = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
    //    Copyright = copyrightAttr?.Copyright ?? $"{Constants.AppName} © {DateTime.Now.Year}";
    //}

    //private void LoadExchanges()
    //{
    //    foreach (var exchange in GlobalData.ExchangeListName.Keys.OrderBy(k => k))
    //    {
    //        Exchanges.Add($"-{exchange}");
    //    }
    //}

    [RelayCommand]
    private void Okay()
    {
        // TODO: Save settings
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        // TODO: Revert changes if needed
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? CloseRequested;
}

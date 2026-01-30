using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;

using System.Collections.ObjectModel;
using System.Reflection;

namespace CryptoScanner.ViewModels;

public partial class AboutWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = $"About {CryptoScanner.Core.Const.Constants.AppName}";

    [ObservableProperty]
    private string _version = $"Version {GlobalData.AppVersion}";

    [ObservableProperty]
    private string _copyright = $"{CryptoScanner.Core.Const.Constants.AppName} © {DateTime.Now.Year}";

    [ObservableProperty]
    private string _author = "Marius";

    [ObservableProperty]
    private ObservableCollection<string> _exchanges = [];

    public AboutWindowViewModel()
    {
        LoadCopyright();
        LoadExchanges();
    }

    private void LoadCopyright()
    {
        // Get copyright from assembly
        var assembly = Assembly.GetExecutingAssembly();
        var copyrightAttr = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
        Copyright = copyrightAttr?.Copyright ?? $"{CryptoScanner.Core.Const.Constants.AppName} © {DateTime.Now.Year}";
    }

    private void LoadExchanges()
    {
        foreach (var exchange in GlobalData.ExchangeListName.Keys.OrderBy(k => k))
        {
            Exchanges.Add($"-{exchange}");
        }
    }

    [RelayCommand]
    private void Okay()
    {
        //TargetWindow?.Close();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? CloseRequested;
}

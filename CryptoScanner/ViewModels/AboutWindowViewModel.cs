using Avalonia.Media.Imaging;

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
    //private string _version = $"Version {GlobalData.AppVersion}";
    // With the variant since 08-11-2026, so it is clear which of the three applications this is
    private string _version = $"Version {GlobalData.AppVersion} ({GlobalData.AppVariant})";

    // The commit this build was made from (see BuildInfo). The version number alone covers weeks of
    // commits, so it does not say which code is running; this does. Empty when the build was not
    // stamped, and then the line is hidden rather than showing an empty label.
    [ObservableProperty]
    private string _build = BuildInfo.Description == "" ? "" : $"Build {BuildInfo.Description}";

    public bool HasBuild => Build != "";

    [ObservableProperty]
    private string _copyright = $"{CryptoScanner.Core.Const.Constants.AppName} © {DateTime.Now.Year}";

    [ObservableProperty]
    //private string _author = "Marius";
    private string _author = $"Author: {CryptoScanner.Core.Const.Constants.Author}";

    // The licence and the address where its full text can be read. Both come from Constants so the
    // Avalonia and the Photino About screen always show the same wording.
    [ObservableProperty]
    private string _license = CryptoScanner.Core.Const.Constants.License;

    [ObservableProperty]
    private string _licenseUrl = CryptoScanner.Core.Const.Constants.LicenseUrl;

    [ObservableProperty]
    private ObservableCollection<string> _exchanges = [];

    /// <summary>
    /// The application icon with the logo of the exchange, the same picture the window and the task
    /// bar show (since 08-11-2026; before that the plain AppIcon.ico). Null when it cannot be drawn,
    /// which leaves the spot empty.
    /// </summary>
    public Bitmap? Icon { get; } = LoadIcon();

    private static Bitmap? LoadIcon()
    {
        try
        {
            return new Bitmap(new MemoryStream(AppIconBuilder.BuildCurrentPng()));
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "AboutWindowViewModel.LoadIcon");
            return null;
        }
    }

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

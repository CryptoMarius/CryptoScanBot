using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using CryptoScanner.Core.Core;
using CryptoScanner.DashBoard.Services;
using CryptoScanner.ViewModels;

using System.Reflection;

namespace CryptoScanner.Views;

public partial class MainWindow: Window
{
    private readonly ITradingViewService _tradingViewService;

    public MainWindow(MainWindowViewModel viewModel, ITradingViewService tradingViewService)
    {
        _tradingViewService = tradingViewService;
        InitializeComponent();

        DataContext = viewModel;

        // Start TradingView service
        _tradingViewService.Start();
    }


    public static void InitAppVariables()
    {
        GlobalData.AppName = "CryptoScanBot";
        GlobalData.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;

        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appVersion = assembly.Version!.ToString();
        while (appVersion.EndsWith(".0.0"))
            appVersion = appVersion[0..^2];

        GlobalData.AppVersion = appVersion;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ExitMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        // Placeholder: Open instellingen-window of dialog later
    }
}
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Services;
using CryptoScanner.Services;

namespace CryptoScanner.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public required ITradingViewService TradingViewService { get; set; }
    public required LogGridViewModel LogGridViewModel { get; set; }
    public required ApplicationStateService ApplicationStateService { get; set; }
    public required DashBoardInformationViewModel DashBoardInformationViewModel { get; set; }
    public required DashboardPositionsViewModel DashboardPositionsViewModel { get; set; }
    public required SymbolGridViewModel SymbolGridViewModel { get; set; }
    public required BrowserViewModel BrowserViewModel { get; set; }
    public required SignalGridViewModel SignalGridViewModel { get; set; }
    public required LiveDataGridViewModel LiveDataGridViewModel { get; set; }
    public required PositionOpenGridViewModel PositionOpenGridViewModel { get; set; }
    public required PositionClosedGridViewModel PositionClosedGridViewModel { get; set; }


    [ObservableProperty]
    private string _title = Core.Const.Constants.AppName;


    //[ObservableProperty] // needs extra notify
    //private bool _analyzerActive = false;
    public bool AnalyzerActive
    {
        get => GlobalData.Settings.Signal.Active;
        set
        {
            if (GlobalData.Settings.Signal.Active != value)
            {
                GlobalData.Settings.Signal.Active = value;
                OnPropertyChanged(nameof(AnalyzerActive));
                GlobalData.SendMvvmMessage(new StatusesHaveChangedMessage());
            }
        }
    }

    //[ObservableProperty] // needs extra notify
    //private bool _soundsActive = false;
    public bool SoundsActive
    {
        get => GlobalData.Settings.Signal.SoundsActive;
        set
        {
            if (GlobalData.Settings.Signal.SoundsActive != value)
            {
                GlobalData.Settings.Signal.SoundsActive = value;
                OnPropertyChanged(nameof(SoundsActive));
                GlobalData.SendMvvmMessage(new StatusesHaveChangedMessage());
            }
        }
    }

    //[ObservableProperty] // needs extra notify
    //private bool _traderActive = false;
    public bool TraderActive
    {
        get => GlobalData.Settings.Trading.Active;
        set
        {
            if (GlobalData.Settings.Trading.Active != value)
            {
                GlobalData.Settings.Trading.Active = value;
                OnPropertyChanged(nameof(TraderActive));
                GlobalData.SendMvvmMessage(new StatusesHaveChangedMessage());
            }
        }
    }


    [ObservableProperty]
    private int _selectedTabIndex = 0;

    [ObservableProperty]
    private string _symbolFilterText = string.Empty;
    public event EventHandler<string>? FilterTextChanged;


    public MainWindowViewModel()
    {
        System.Diagnostics.Debug.WriteLine($"MainViewModel default constructor called");
    }

    public MainWindowViewModel(
        ITradingViewService tradingViewService,
        ApplicationStateService applicationStateService,
        DashBoardInformationViewModel dashBoardInformationViewModel,
        DashboardPositionsViewModel dashBoardPositionsViewModel,
        SymbolGridViewModel symbolGridViewModel,
        SignalGridViewModel signalGridViewModel,
        LiveDataGridViewModel liveDataGridViewModel,
        PositionOpenGridViewModel positionOpenGridViewModel,
        PositionClosedGridViewModel positionClosedGridViewModel,
        BrowserViewModel browserViewModel,
        LogGridViewModel logGridViewModel)
    {
        TradingViewService = tradingViewService;
        ApplicationStateService = applicationStateService;
        DashBoardInformationViewModel = dashBoardInformationViewModel;
        DashboardPositionsViewModel = dashBoardPositionsViewModel;
        SymbolGridViewModel = symbolGridViewModel;
        SignalGridViewModel = signalGridViewModel;
        LiveDataGridViewModel = liveDataGridViewModel;
        PositionOpenGridViewModel = positionOpenGridViewModel;
        PositionClosedGridViewModel = positionClosedGridViewModel;
        BrowserViewModel = browserViewModel;
        LogGridViewModel = logGridViewModel;

        // Subscribe the child ViewModels to filter the contents
        FilterTextChanged += SymbolGridViewModel.OnFilterTextChanged;
        FilterTextChanged += SignalGridViewModel.OnFilterTextChanged;

        App.EventOpenInInternalBrowser += OnOpenInInternalBrowserRequested;

        // Keep the menu checkboxes in sync when the dashboard icons toggle a setting.
        WeakReferenceMessenger.Default.Register<StatusesHaveChangedMessage>(this, OnStatusesHaveChanged);
    }


    private void OnStatusesHaveChanged(object recipient, StatusesHaveChangedMessage message)
    {
        OnPropertyChanged(nameof(AnalyzerActive));
        OnPropertyChanged(nameof(TraderActive));
        OnPropertyChanged(nameof(SoundsActive));
    }

    private void OnOpenInInternalBrowserRequested(string url, bool switchTab)
    {
        //BrowserViewModel.NavigateToTradingView(url);
        if (BrowserViewModel != null)
        {
            System.Diagnostics.Debug.WriteLine($"OpenInBrowser: {url}");

            // Debug.WriteLine is compiled away in Release, so the line above proved nothing about
            // the build that is actually used. The log tab carries it in both.
            GlobalData.AddTextToLogTab($"Tradingview tab navigates to: {url}");

            // switch to the Tradingview tab with the browser
            if (switchTab)
                SelectedTabIndex = 1;

            // Navigate triggers initialization + tab switch automatically
            BrowserViewModel.NavigateCommand.Execute(url);
        }
        else
            GlobalData.AddErrorToLogTab($"Tradingview tab: it does not exist, {url} was not shown");
    }


    public async Task ExitApp()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.TryShutdown();
        }
        else
        {
            // Fallback
            Environment.Exit(0);
        }
    }


    [RelayCommand]
    private void ApplyFilter(string? filterText)
    {
        if (filterText != null)
        {
            SymbolFilterText = filterText;
            FilterTextChanged?.Invoke(this, filterText);
        }
    }
}
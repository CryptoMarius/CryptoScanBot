using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

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
    public required LogViewModel LogGridViewModel { get; set; }
    public required ApplicationStateService ApplicationStateService { get; set; }
    public required DashBoardInformationViewModel DashBoardInformationViewModel { get; set; }
    public required DashboardPositionsViewModel DashboardPositionsViewModel { get; set; }
    public required SymbolViewModel SymbolViewModel { get; set; }
    public required BrowserViewModel BrowserViewModel { get; set; }
    public required SignalViewModel SignalViewModel { get; set; }
    public required LiveDataViewModel LiveDataViewModel { get; set; }
    public required PositionOpenViewModel PositionOpenViewModel { get; set; }
    public required PositionClosedViewModel PositionClosedViewModel { get; set; }


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
                Dispatcher.UIThread.Post(() => { WeakReferenceMessenger.Default.Send(new StatusesHaveChangedMessage()); });
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
                Dispatcher.UIThread.Post(() => { WeakReferenceMessenger.Default.Send(new StatusesHaveChangedMessage()); });
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
                Dispatcher.UIThread.Post(() => { WeakReferenceMessenger.Default.Send(new StatusesHaveChangedMessage()); });
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
        SymbolViewModel symbolGridViewModel,
        SignalViewModel signalGridViewModel,
        LiveDataViewModel liveDataGridViewModel,
        PositionOpenViewModel positionOpenGridViewModel,
        PositionClosedViewModel positionClosedGridViewModel,
        BrowserViewModel browserViewModel,
        LogViewModel logGridViewModel)
    {
        TradingViewService = tradingViewService;
        ApplicationStateService = applicationStateService;
        DashBoardInformationViewModel = dashBoardInformationViewModel;
        DashboardPositionsViewModel = dashBoardPositionsViewModel;
        SymbolViewModel = symbolGridViewModel;
        SignalViewModel = signalGridViewModel;
        LiveDataViewModel = liveDataGridViewModel;
        PositionOpenViewModel = positionOpenGridViewModel;
        PositionClosedViewModel = positionClosedGridViewModel;
        BrowserViewModel = browserViewModel;
        LogGridViewModel = logGridViewModel;

        // Subscribe the child ViewModels to filter the contents
        FilterTextChanged += SymbolViewModel.OnFilterTextChanged;
        FilterTextChanged += SignalViewModel.OnFilterTextChanged;

        App.EventOpenInInternalBrowser += OnOpenInInternalBrowserRequested;
    }

    private void OnOpenInInternalBrowserRequested(string url, bool switchTab)
    {
        //BrowserViewModel.NavigateToTradingView(url);
        if (BrowserViewModel != null)
        {
            System.Diagnostics.Debug.WriteLine($"OpenInBrowser: {url}");

            // switch to the Tradingview tab with the browser
            if (switchTab)
                SelectedTabIndex = 1;

            // Navigate triggers initialization + tab switch automatically
            BrowserViewModel.NavigateCommand.Execute(url);
        }
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
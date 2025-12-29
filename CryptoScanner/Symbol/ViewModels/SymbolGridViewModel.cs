using Avalonia.Interactivity;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Model;
using CryptoScanner.Symbol.Model;


namespace CryptoScanner.Symbol.ViewModels;

public partial class SymbolGridViewModel : ObservableObjectWithOwner
{
    /// <summary>
    /// Collection of signals to display in the grid
    /// </summary>
    [ObservableProperty]
    private ObservableRangeCollection<SymbolInfo> _symbols = [];

    // Event voor parent ViewModel
    public event EventHandler<string>? EventOpenInInternalBrowser;

    public SymbolGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("SymbolGridViewModel constructor called");
        GlobalData.SymbolsHaveChangedEvent += new AddTextEvent(SymbolsHaveChangedEvent);
        SymbolsHaveChangedEvent("");
    }


    public event EventHandler<SymbolInfo>? RequestSortedInsert;
    public event EventHandler? RequestSort;


    private void SymbolsHaveChangedEvent(string text)
    {
        // Laad symbols direct in de observable collection
        List<SymbolInfo> symbols = [];
        foreach (var symbol in GlobalData.ActiveExchange?.SymbolListName.Values ?? [])
        {
            symbols.Add(new SymbolInfo
            {
                SymbolObject = symbol,
                Id = symbol.Id,
                Symbol = symbol.Name,
                Volume = symbol.Volume,
                Distance = 0.0
            });
        }
        Symbols.AddRange(symbols);
    }


    /// <summary>
    /// Command to open signal in external program
    /// Triggered from context menu
    /// </summary>
    [RelayCommand]
    private static void OpenExternalProgram(object? parameter)
    {
        if (parameter is not SymbolInfo symbol)
            return;

        // Implement your external program logic here
        System.Diagnostics.Debug.WriteLine($"Opening {symbol} in external program");
    }

    [RelayCommand]
    private void LaunchTradingApp(object? parameter)
    {
        if (parameter is not SymbolInfo signal)
            return;

        // Implement your external program logic here
        System.Diagnostics.Debug.WriteLine($"Opening {signal.Symbol} in external program");

        CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;


        // Voor Altrady en Hypertrader werkt dit kunstje natuurlijk niet
        if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
            tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
        GlobalData.LoadLinkSettings(); // refresh links

        var symbol = signal.SymbolObject;
        var interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m];
        if (symbol != null && interval != null)
            ActivateTradingApp(GlobalData.Settings.General.TradingApp, symbol, interval, tradingAppInternExtern);
    }


    [RelayCommand]
    private void LaunchTradingViewInternal(object? parameter)
    {
        if (parameter is not SymbolInfo signal)
            return;

        // Implement your external program logic here
        System.Diagnostics.Debug.WriteLine($"Opening {signal.Symbol} in external program");

        CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;


        // Voor Altrady en Hypertrader werkt dit kunstje natuurlijk niet
        if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
            tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
        GlobalData.LoadLinkSettings(); // refresh links

        var symbol = signal.SymbolObject;
        var interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m];
        if (symbol != null && interval != null)
            ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, tradingAppInternExtern);
    }

    [RelayCommand]
    private void LaunchTradingViewExternal(object? parameter)
    {
        if (parameter is not SymbolInfo signal)
            return;

        // Implement your external program logic here
        System.Diagnostics.Debug.WriteLine($"Opening {signal.Symbol} in external program");

        CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;


        // Voor Altrady en Hypertrader werkt dit kunstje natuurlijk niet
        if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
            tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
        GlobalData.LoadLinkSettings(); // refresh links

        var symbol = signal.SymbolObject;
        var interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m];
        if (symbol != null && interval != null)
            ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, tradingAppInternExtern);
    }


    /// <summary>
    /// Command to view signal details
    /// </summary>
    [RelayCommand]
    private static void ViewDetails(object? parameter)
    {
        if (parameter is not SymbolInfo symbol)
            return;

        System.Diagnostics.Debug.WriteLine($"Viewing details for symbol: {symbol}");
    }

    /// <summary>
    /// Command to copy signal to clipboard
    /// </summary>
    [RelayCommand]
    private static void CopySignal(object? parameter)
    {
        if (parameter is not SymbolInfo symbol)
            return;

        var text = $"{symbol.Symbol}";
        System.Diagnostics.Debug.WriteLine($"Copying signal to clipboard: {text}");
    }

    public void ActivateTradingApp(CryptoTradingApp externalTradingApp, CryptoSymbol symbol, CryptoInterval interval, CryptoExternalUrlType viaTradingBrowser, bool activateTab = true)
    {
        // Activate the trading application (and we use a dummy browser for Altrady)

        (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(externalTradingApp, false, symbol, interval);
        if (Url != "")
        {
            GlobalData.AddTextToLogTab($"Linktools activate {Url}");
            EventOpenInInternalBrowser?.Invoke(this, Url);

            //// Open the url via our own hidden browser (to avoid the Altrady jump-step)
            //if (viaTradingBrowser == CryptoExternalUrlType.Internal)
            //{
            //    //await WebViewTradingView.ActivateUrlAsync(Url);
            //    //if (activateTab && TabControl != null)
            //    //    TabControl.SelectedTab = TabPageBrowser;
            //    // Usage anywhere:
            //    App.HiddenBrowser.Navigate(Url);
            //}
            //else
            //{
            //    if (Execute == CryptoExternalUrlType.Internal)
            //    {
            //        // Send url-event via the MainWindowViewModel
            //        EventOpenInInternalBrowser?.Invoke(this, Url);
            //    }
            //    else
            //    {
            //        // Open via the external (system) browser
            //        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Url) { UseShellExecute = true });
            //    }
            //}
        }
    }




}
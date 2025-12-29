using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.Visualisation.ViewModels;

public partial class SymbolSelectorViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<string> _baseSymbols = [];

    [ObservableProperty]
    private ObservableCollection<string> _quoteSymbols = [];

    [ObservableProperty]
    private ObservableCollection<string> _intervals = [];

    [ObservableProperty]
    private string _selectedBase = "BTC";

    [ObservableProperty]
    private string _selectedQuote = "USDT";

    [ObservableProperty]
    private string _selectedInterval = "1h";

    // Combined symbol (e.g., "BTCUSDT")
    public string SelectedSymbol => SelectedBase + SelectedQuote;

    public SymbolSelectorViewModel()
    {
        InitializeSymbolLists();
        InitializeIntervals();
    }

    private void InitializeSymbolLists()
    {
        // Base symbols
        BaseSymbols.Clear();
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out var exchange))
        {
            foreach (var symbol in exchange.SymbolListName.Values.OrderBy(x => x.Base))
            {
                if (symbol.QuoteData.FetchCandles)
                    BaseSymbols.Add(symbol.Base);
            }
        }

        // Quote symbols
        QuoteSymbols.Clear();
        foreach (var quoteData in GlobalData.Settings.QuoteCoins.Values.OrderBy(x => x.Name))
        {
            if (quoteData.FetchCandles)
                QuoteSymbols.Add(quoteData.Name);
        }
    }

    private void InitializeIntervals()
    {
        Intervals.Clear();
        foreach (var interval in GlobalData.IntervalList)
        {
            Intervals.Add(interval.Name);
        }
    }

    partial void OnSelectedBaseChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedSymbol));
    }

    partial void OnSelectedQuoteChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedSymbol));
    }

    public void LoadFromSession(ZoneSession session)
    {
        SelectedBase = session.SymbolBase;
        SelectedQuote = session.SymbolQuote;
        SelectedInterval = session.IntervalName;
    }

    public void SaveToSession(ZoneSession session)
    {
        session.SymbolBase = SelectedBase;
        session.SymbolQuote = SelectedQuote;
        session.IntervalName = SelectedInterval;
    }
}

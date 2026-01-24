using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Zones;
using CryptoScanner.Model;


namespace CryptoScanner.ViewModels;

public partial class SymbolGridViewModel : ObservableObject
{
    private DispatcherTimer? _timerRefreshZones = new() { Interval = TimeSpan.FromSeconds(15) };

    /// <summary>
    /// Collection of signals to display in the grid
    /// </summary>
    [ObservableProperty]
    private ObservableRangeCollection<SymbolViewModel> _symbols = [];

    //public static bool readSymbols = true;

    public SymbolGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("SymbolGridViewModel constructor called");

        _timerRefreshZones.Tick += TimerRefreshZonesTick;
        _timerRefreshZones.Start();

        GlobalData.SymbolsHaveChangedEvent += new AddTextEvent(SymbolsHaveChangedEvent);
        SymbolsHaveChangedEvent("");
    }


    //public event EventHandler<SymbolViewModel>? RequestSortedInsert;
    //public event EventHandler? RequestSort;

    private string _currentFilter = string.Empty;
    private void SymbolsHaveChangedEvent(string text)
    {
        // Delayed load of the symbols
        //if (readSymbols)
        //    GlobalData.LoadSymbols();
        //readSymbols = false;

        // Laad symbols direct in de observable collection
        List<SymbolViewModel> symbols = [];
        foreach (var symbol in GlobalData.ActiveExchange?.SymbolListName.Values ?? [])
        {
            if (symbol.QuoteData.FetchCandles && symbol.Status == 1 && !symbol.IsBarometerSymbol())
            {
                if (string.IsNullOrWhiteSpace(_currentFilter) || symbol.Name.Contains(_currentFilter, StringComparison.OrdinalIgnoreCase))
                {
                    symbols.Add(new SymbolViewModel
                    {
                        Object = symbol,
                        Id = symbol.Id,
                        Symbol = symbol.Name,
                        Volume = symbol.Volume,
                        Distance = ZoneTools.ZoneDistance(symbol),
                    });
                }
            }
        }
        Symbols.Replace(symbols);
        
        // Request sort na filtering
        //RequestSort?.Invoke(this, EventArgs.Empty);
    }

    public void OnFilterTextChanged(object? sender, string filterText)
    {
        _currentFilter = filterText;
        SymbolsHaveChangedEvent("");
    }


    private void TimerRefreshZonesTick(object? sender, EventArgs e)
    {
        foreach (var symbol in Symbols)
        {
            symbol.Distance = ZoneTools.ZoneDistance(symbol.Object);
        }
    }
}
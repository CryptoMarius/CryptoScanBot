using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Zones;
using CryptoScanner.Model;


namespace CryptoScanner.ViewModels;

public partial class SymbolGridViewModel : ObservableObject
{
    /// <summary>
    /// Collection of signals to display in the grid
    /// </summary>
    [ObservableProperty]
    private ObservableRangeCollection<SymbolViewModel> _symbols = [];


    public SymbolGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("SymbolGridViewModel constructor called");
        GlobalData.SymbolsHaveChangedEvent += new AddTextEvent(SymbolsHaveChangedEvent);
        SymbolsHaveChangedEvent("");
    }

    public event EventHandler<SymbolViewModel>? RequestSortedInsert;
    public event EventHandler? RequestSort;

    private string _currentFilter = string.Empty;
    private void SymbolsHaveChangedEvent(string text)
    {
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
        RequestSort?.Invoke(this, EventArgs.Empty);
    }

    public void OnFilterTextChanged(object? sender, string filterText)
    {
        _currentFilter = filterText;
        SymbolsHaveChangedEvent("");
    }

}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;
using CryptoScanner.Symbol.Model;

using System.Collections.ObjectModel;

namespace CryptoScanner.Symbol.ViewModels;

public partial class SymbolGridViewModel : ObservableObject
{
    /// <summary>
    /// Collection of signals to display in the grid
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SymbolInfo> _symbols = [];


    public SymbolGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("SymbolGridViewModel constructor called");
        GlobalData.SymbolsHaveChangedEvent += new AddTextEvent(SymbolsHaveChangedEvent);
        SymbolsHaveChangedEvent("");
    }


    private void SymbolsHaveChangedEvent(string text)
    {
        // Laad symbols direct in de observable collection
        foreach (var symbol in GlobalData.ActiveExchange?.SymbolListName.Values ?? [])
        {
            Symbols.Add(new SymbolInfo
            {
                SymbolObject = symbol,
                Id = symbol.Id,
                Symbol = symbol.Name,
                Volume = symbol.Volume,
                Distance = 0.0
            });
        }
    }


    //// Voeg nieuwe symbol toe (bijvoorbeeld bij live updates)
    //public void AddSymbol(SymbolInfo symbol)
    //{
    //    Symbols.Add(symbol);
    //    // Optioneel: direct op juiste plek invoegen als gesorteerd
    //}

    //// Update bestaande symbol
    //public void UpdateSymbol(SymbolInfo symbol)
    //{
    //    var existing = Symbols.FirstOrDefault(s => s.Id == symbol.Id);
    //    if (existing != null)
    //    {
    //        existing.Symbol = symbol.Symbol;
    //?        existing.Volume = symbol.Volume;
    //        existing.Distance = symbol.Distance;
    //    }
    //}

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

}

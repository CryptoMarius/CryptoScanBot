using Avalonia.Collections;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;

namespace CryptoScanner.ViewModels;

public partial class SymbolGridViewModel : ObservableObject
{
    private DispatcherTimer _timerRefreshZones = new() { Interval = TimeSpan.FromSeconds(15) };

    /// <summary>
    /// Collection of signals to display in the grid
    /// </summary>
    [ObservableProperty]
    private AvaloniaList<SymbolViewModel> _symbols = [];

    //public static bool readSymbols = true;

    public SymbolGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("SymbolGridViewModel constructor called");

        WeakReferenceMessenger.Default.Register<SymbolsHaveChangedMessage>(this, OnSymbolsHaveChanged);
        WeakReferenceMessenger.Default.Register<ZonesCalculatedForSymbolMessage>(this, OnZonesCalculatedForSymbol);

        _timerRefreshZones.Tick += TimerRefreshZonesTick;
        _timerRefreshZones.Start();

        ReloadSymbolsWithFilter();
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<SymbolsHaveChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ZonesCalculatedForSymbolMessage>(this);

        _timerRefreshZones.Stop();
        _timerRefreshZones.Tick -= TimerRefreshZonesTick;
    }

    private string _currentFilter = string.Empty;
    private void ReloadSymbolsWithFilter()
    {
        // Laad symbols
        List<SymbolViewModel> viewModels = [];
        foreach (var symbol in GlobalData.ActiveExchange?.SymbolListName.Values ?? [])
        {
            if (symbol.QuoteData.FetchCandles && symbol.Status == 1 && !symbol.IsBarometerSymbol())
            {
                if (string.IsNullOrWhiteSpace(_currentFilter) || symbol.Name.Contains(_currentFilter, StringComparison.OrdinalIgnoreCase))
                {
                    viewModels.Add(new SymbolViewModel { Object = symbol, });
                }
            }
        }
        Symbols.Clear();
        Symbols.AddRange([.. viewModels]);
    }

    private void OnSymbolsHaveChanged(object recipient, SymbolsHaveChangedMessage message)
    {
        ReloadSymbolsWithFilter(); // for now..
    }

    public void OnFilterTextChanged(object? sender, string filterText)
    {
        _currentFilter = filterText;
        ReloadSymbolsWithFilter();
    }


    private void TimerRefreshZonesTick(object? sender, EventArgs e)
    {
        foreach (var symbol in Symbols)
        {
            symbol.Distance = string.Empty; // Just reset it
        }
    }


    // Targeted refresh: after ZoneDlz/ZoneFvg.CalculateZonesAsync finishes for a single
    // symbol, clear that row's cached Distance text. The setter on SymbolViewModel.Distance
    // raises PropertyChanged, which forces the DataGrid cell to re-read the value via
    // ZoneTools.ZoneDistance(symbol). Cheaper than the 15-second timer that resets every row.
    private void OnZonesCalculatedForSymbol(object recipient, ZonesCalculatedForSymbolMessage message)
    {
        foreach (var symbol in Symbols)
        {
            if (symbol.Object == message.Symbol)
            {
                symbol.Distance = string.Empty;
                break;
            }
        }
    }
}
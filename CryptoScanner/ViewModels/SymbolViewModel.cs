using Avalonia.Collections;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;

using System.Collections.ObjectModel;

namespace CryptoScanner.ViewModels;

public partial class SymbolViewModel : BaseGridViewModel<CryptoSymbol, SymbolColumnEnum, SymbolColumnComparer>
{
    private DispatcherTimer _timerRefreshZones = new() { Interval = TimeSpan.FromSeconds(15) };

    private string _currentFilter = string.Empty;


    public SymbolViewModel()
    {
        System.Diagnostics.Debug.WriteLine("SymbolGridViewModel constructor called");
        SortColumn = SymbolColumnEnum.Symbol;
        _columns = SymbolColumns.GetColumns();
        _columnWidths = GetWidths(_columns);
        System.Diagnostics.Debug.WriteLine($"SymbolGridViewModel: {_columns.Count} columns, {_columnWidths.Count} widths");

        WeakReferenceMessenger.Default.Register<SymbolsHaveChangedMessage>(this, OnSymbolsHaveChanged);

        //_timerRefreshZones.Tick += TimerRefreshZonesTick;
        //_timerRefreshZones.Start();

        ReloadSymbolsWithFilter();
    }

    public void Dispose()
    {
        _timerRefreshZones.Stop();
        //_timerRefreshZones.Tick -= TimerRefreshZonesTick;
    }

    private void ReloadSymbolsWithFilter()
    {
        // Laad symbols
        List<CryptoSymbol> list = [];
        foreach (var symbol in GlobalData.ActiveExchange?.SymbolListName.Values ?? [])
        {
            if (symbol.QuoteData.FetchCandles && symbol.Status == 1 && !symbol.IsBarometerSymbol())
            {
                if (string.IsNullOrWhiteSpace(_currentFilter) || symbol.Name.Contains(_currentFilter, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(symbol);
                }
            }
        }

        lock (_lock)
        {
            _allObjects = list;
            ApplySort(SortColumn);
        }

        RefreshVisibleItems();
    }

    public void OnFilterTextChanged(object? sender, string filterText)
    {
        _currentFilter = filterText;
        ReloadSymbolsWithFilter();
    }

    protected override void RefreshVisibleItems()
    {
        System.Diagnostics.Debug.WriteLine("RefreshVisibleItems called");

        if (Dispatcher.UIThread.CheckAccess())
        {
            lock (_lock)
            {
                var selectedId = SelectedObject?.Id;
                VisibleObjects = new AvaloniaList<CryptoSymbol>(_allObjects);
                if (selectedId.HasValue)
                    SelectedObject = VisibleObjects.FirstOrDefault(p => p.Id == selectedId.Value);
            }
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                lock (_lock)
                {
                    var selectedId = SelectedObject?.Id;
                    VisibleObjects = new AvaloniaList<CryptoSymbol>(_allObjects);
                    if (selectedId.HasValue)
                        SelectedObject = VisibleObjects.FirstOrDefault(p => p.Id == selectedId.Value);
                }
            });
        }
    }

    private void OnSymbolsHaveChanged(object recipient, SymbolsHaveChangedMessage message)
    {
        ReloadSymbolsWithFilter(); // for now..
    }



    //private void TimerRefreshZonesTick(object? sender, EventArgs e)
    //{
    //    foreach (var symbol in Symbols)
    //    {
    //        symbol.Distance = string.Empty; // Just reset it
    //    }
    //}
}
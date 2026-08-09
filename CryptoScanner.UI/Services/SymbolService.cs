using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;
using CryptoScanner.UI.ViewModels;

using System.ComponentModel;

namespace CryptoScanner.UI.Services;

public class SymbolService : IDisposable
{
    private const string GridName = "Symbol";
    private readonly object _lock = new();
    private readonly ApplicationStateService _stateService;
    private List<SymbolViewModel> _symbols = [];
    private Dictionary<string, SymbolViewModel> _symbolsByName = new(StringComparer.OrdinalIgnoreCase);
    private string _filter = "";

    // Set by InvalidateDistance, drained by the timer below. See InvalidateDistance for the why.
    private volatile bool _distancesInvalidated;
    private System.Threading.Timer? _flushTimer;

    // Sort state — one comparer per column enum value, created once
    private readonly GridSortState<SymbolColumnEnum> _sortState;
    private static readonly Dictionary<SymbolColumnEnum, SymbolColumnComparer> _comparers = new()
    {
        [SymbolColumnEnum.Id] = new(SymbolColumnEnum.Id),
        [SymbolColumnEnum.Symbol] = new(SymbolColumnEnum.Symbol),
        [SymbolColumnEnum.Volume] = new(SymbolColumnEnum.Volume),
        [SymbolColumnEnum.Distance] = new(SymbolColumnEnum.Distance),
    };

    public SymbolService(ApplicationStateService stateService)
    {
        _stateService = stateService;

        // Restore persisted sort state
        _stateService.RestoreGridSortState(GridName, out var sortColumn, out var sortDirection);
        _sortState = !string.IsNullOrEmpty(sortColumn)
            ? new GridSortState<SymbolColumnEnum>()
            : new GridSortState<SymbolColumnEnum>(SymbolColumnEnum.Symbol);
        _sortState.Restore(sortColumn, sortDirection);
    }

    public event Action? SymbolsChanged;
    public event Action? SelectedSymbolChanged;

    /// <summary>
    /// Subscribe to the scanner messages. Without this the symbol list is built once, before
    /// ThreadLoadData has read the symbols, and never refreshed — leaving the panel empty.
    /// </summary>
    public void Start()
    {
        WeakReferenceMessenger.Default.Register<SymbolsHaveChangedMessage>(this, (_, _) => Reload());
        WeakReferenceMessenger.Default.Register<ExchangeSwitchedMessage>(this, (_, _) =>
        {
            SetSelectedSymbol(null);
            Reload();
        });
        WeakReferenceMessenger.Default.Register<ZonesCalculatedForSymbolMessage>(this,
            (_, message) => InvalidateDistance(message.Symbol));
        WeakReferenceMessenger.Default.Register<ConfigurationChangedMessage>(this, (_, _) => Reload());

        Reload();

        _flushTimer = new System.Threading.Timer(_ =>
        {
            if (GlobalData.ApplicationIsClosing)
                return;
            try
            {
                FlushInvalidatedDistances();
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "SymbolService flush");
            }
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public void Dispose()
    {
        _flushTimer?.Dispose();
        _flushTimer = null;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        GC.SuppressFinalize(this);
    }

    public CryptoSymbol? SelectedSymbol { get; private set; }

    public void SetSelectedSymbol(CryptoSymbol? symbol)
    {
        if (SelectedSymbol != symbol)
        {
            SelectedSymbol = symbol;
            SelectedSymbolChanged?.Invoke();
        }
    }

    public GridSortState<SymbolColumnEnum> SortState => _sortState;

    public IReadOnlyList<SymbolViewModel> Symbols
    {
        get
        {
            lock (_lock)
                return _symbols.ToList();
        }
    }

    public void SetFilter(string filter)
    {
        _filter = filter;
        Reload();
    }

    /// <summary>
    /// Toggle sort on a column (click header). Matches Avalonia UserControlWithGrid.OnDataGridSorting.
    /// </summary>
    public void Sort(SymbolColumnEnum column)
    {
        _sortState.ToggleSort(column);
        ApplySort();
        _stateService.SaveGridSortState(GridName, _sortState.SortColumnName, _sortState.SortDirection);
        SymbolsChanged?.Invoke();
    }

    /// <summary>
    /// Full reload: rebuilds the symbol list, reusing existing ViewModels where possible.
    /// </summary>
    public void Reload()
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null)
        {
            lock (_lock)
            {
                _symbols = [];
                _symbolsByName.Clear();
            }
            SymbolsChanged?.Invoke();
            return;
        }

        var newList = new List<SymbolViewModel>();
        var newLookup = new Dictionary<string, SymbolViewModel>(StringComparer.OrdinalIgnoreCase);

        foreach (var symbol in exchange.SymbolListName.Values)
        {
            if (!symbol.QuoteData!.FetchCandles || symbol.Status != 1 || symbol.IsBarometerSymbol())
                continue;

            if (!string.IsNullOrEmpty(_filter) &&
                !symbol.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                continue;

            SymbolViewModel vm;
            lock (_lock)
            {
                if (!_symbolsByName.TryGetValue(symbol.Name, out vm!))
                    vm = new SymbolViewModel(symbol);
            }

            newList.Add(vm);
            newLookup[symbol.Name] = vm;
        }

        lock (_lock)
        {
            _symbols = newList;
            _symbolsByName = newLookup;
        }

        ApplySort();
        SymbolsChanged?.Invoke();
    }

    /// <summary>
    /// Invalidate volume on all symbols (called on timer tick).
    /// </summary>
    public void InvalidateVolumes()
    {
        List<SymbolViewModel> snapshot;
        lock (_lock)
            snapshot = _symbols.ToList();

        foreach (var vm in snapshot)
            vm.InvalidateVolume();

        SymbolsChanged?.Invoke();
    }

    /// <summary>
    /// Invalidate distance on all symbols (called on timer tick).
    /// </summary>
    public void InvalidateDistances()
    {
        List<SymbolViewModel> snapshot;
        lock (_lock)
            snapshot = _symbols.ToList();

        foreach (var vm in snapshot)
            vm.InvalidateDistance();

        SymbolsChanged?.Invoke();
    }

    /// <summary>
    /// Invalidate distance for a single symbol (after zone calculation).
    /// <para>
    /// Raising SymbolsChanged straight away repaints the whole symbol grid, and a zone sweep sends
    /// one of these per symbol — hundreds of repaints in a burst, each of which the web view turns
    /// into a diff of its own. The event is therefore coalesced: the flag is set here and the
    /// notification follows on the next tick of the timer below, so a sweep costs one repaint
    /// instead of one per symbol.
    /// </para>
    /// </summary>
    public void InvalidateDistance(CryptoSymbol symbol)
    {
        lock (_lock)
        {
            if (_symbolsByName.TryGetValue(symbol.Name, out var vm))
                vm.InvalidateDistance();
        }
        _distancesInvalidated = true;
    }

    /// <summary>
    /// Raise the coalesced SymbolsChanged of <see cref="InvalidateDistance"/>, if anything came in
    /// since the previous tick.
    /// </summary>
    private void FlushInvalidatedDistances()
    {
        if (!_distancesInvalidated)
            return;
        _distancesInvalidated = false;
        SymbolsChanged?.Invoke();
    }

    private void ApplySort()
    {
        if (_sortState.SortColumn is not { } col)
            return;

        if (!_comparers.TryGetValue(col, out var comparer))
            return;

        lock (_lock)
        {
            if (_sortState.IsAscending)
                _symbols.Sort(comparer);
            else
                _symbols.Sort((a, b) => comparer.Compare(b, a));
        }
    }
}

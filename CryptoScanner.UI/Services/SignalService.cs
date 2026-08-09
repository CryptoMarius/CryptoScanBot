using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.Signal;
using CryptoScanner.UI.ViewModels;

using Dapper;

using System.ComponentModel;

namespace CryptoScanner.UI.Services;

public class SignalService : IDisposable
{
    private const string GridName = "Signal";
    private readonly object _lock = new();
    private readonly ApplicationStateService _stateService;
    private List<SignalViewModel> _signals = [];
    private string _currentFilter = string.Empty;

    // The pump used to live in Signals.razor, which meant it only ran while that tab was open:
    // queued signals piled up and expired ones were never removed. It belongs to the singleton
    // service so it keeps running regardless of the active tab (same as the Avalonia timers).
    private System.Threading.Timer? _pumpTimer;
    private bool _disposed;

    public GridSortState<SignalColumnEnum> SortState { get; }

    public event Action? SignalsChanged;

    public SignalService(ApplicationStateService stateService)
    {
        _stateService = stateService;

        _stateService.RestoreGridSortState(GridName, out var sortColumn, out var sortDirection);
        SortState = !string.IsNullOrEmpty(sortColumn)
            ? new GridSortState<SignalColumnEnum>()
            : new GridSortState<SignalColumnEnum>(SignalColumnEnum.Date, ListSortDirection.Descending);
        SortState.Restore(sortColumn, sortDirection);
    }

    public IReadOnlyList<SignalViewModel> Signals
    {
        get
        {
            lock (_lock)
                return _signals.ToList();
        }
    }

    public void Start()
    {
        GlobalData.AnalyzeSignalCreated += OnSignalCreated;

        WeakReferenceMessenger.Default.Register<SignalDeleteAllMessage>(this, (_, _) => ClearGrid());
        WeakReferenceMessenger.Default.Register<ExchangeSwitchedMessage>(this, (_, _) => OnExchangeSwitched());
        WeakReferenceMessenger.Default.Register<ConfigurationChangedMessage>(this, (_, _) => SignalsChanged?.Invoke());

        LoadFromDatabase(_currentFilter);

        _pumpTimer = new System.Threading.Timer(_ =>
        {
            if (_disposed || GlobalData.ApplicationIsClosing)
                return;
            try
            {
                ProcessPendingSignals();
                RemoveExpired();
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "SignalService pump");
            }
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public void LoadFromDatabase(string filter = "")
    {
        try
        {
            var signals = GlobalData.LoadSignals(filter);
            lock (_lock)
            {
                _signals.Clear();
                _signals.AddRange(signals.Select(s => new SignalViewModel(s)));
            }
            ApplySort();
            SignalsChanged?.Invoke();
        }
        catch
        {
            // DB not available yet (scanner not fully initialized)
        }
    }

    /// <summary>
    /// Apply the symbol filter of the left hand panel, same as Avalonia wires
    /// MainWindowViewModel.FilterTextChanged to the signal grid.
    /// </summary>
    public void SetFilter(string filter)
    {
        _currentFilter = filter ?? string.Empty;
        LoadFromDatabase(_currentFilter);
    }

    public void Sort(SignalColumnEnum column)
    {
        SortState.ToggleSort(column);
        ApplySort();
        _stateService.SaveGridSortState(GridName, SortState.SortColumnName, SortState.SortDirection);
        SignalsChanged?.Invoke();
    }

    public void ProcessPendingSignals()
    {
        // Drain the shared GlobalData.SignalQueue (filled by SignalNotification) instead of a
        // private queue, so the queue does not grow unbounded and the emulator/SignalR see the
        // same state the Avalonia host produces.
        if (GlobalData.SignalQueue.Count == 0)
            return;

        bool changed = false;
        if (Monitor.TryEnter(GlobalData.SignalQueue))
        {
            try
            {
                lock (_lock)
                {
                    while (GlobalData.SignalQueue.Count > 0)
                    {
                        CryptoSignal signal = GlobalData.SignalQueue.Dequeue();
                        if (signal == null)
                            continue;

                        var symbol = signal.Symbol;
                        if (!string.IsNullOrWhiteSpace(_currentFilter) &&
                            !symbol.Name.Contains(_currentFilter, StringComparison.OrdinalIgnoreCase))
                            continue;

                        _signals.Insert(0, new SignalViewModel(signal));
                        changed = true;
                    }
                }
            }
            finally
            {
                Monitor.Exit(GlobalData.SignalQueue);
            }
        }

        if (changed)
        {
            ApplySort();
            SignalsChanged?.Invoke();
        }
    }

    public void RemoveExpired()
    {
        bool changed;
        var now = GlobalData.Clock.UtcNow;
        lock (_lock)
        {
            int removed = _signals.RemoveAll(s => s.Object.ExpirationDate < now);
            changed = removed > 0;
        }
        if (changed)
            SignalsChanged?.Invoke();
    }

    /// <summary>
    /// Remove every signal from the database and from memory, identical to the Avalonia
    /// CommandSignalDeleteAll (which did far more than clearing the grid).
    /// </summary>
    public void DeleteAll()
    {
        try
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();

            using var transaction = databaseThread.BeginTransaction();
            databaseThread.Connection.Execute("delete from Signal", transaction);
            transaction.Commit();

            if (GlobalData.ActiveExchange != null)
            {
                foreach (CryptoSymbol symbol in GlobalData.ActiveExchange.SymbolListId.Values)
                {
                    foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
                    {
                        symbolInterval.SignalList.Clear();
                    }
                }
            }

            GlobalData.SendMvvmMessage(new SignalDeleteAllMessage());
            GlobalData.AddTextToLogTab("Manually deleted all signals from the database");
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"Error deleting signals: {error.Message}");
        }
    }

    private void ClearGrid()
    {
        if (Monitor.TryEnter(GlobalData.SignalQueue))
        {
            try { GlobalData.SignalQueue.Clear(); }
            finally { Monitor.Exit(GlobalData.SignalQueue); }
        }

        lock (_lock)
            _signals.Clear();
        SignalsChanged?.Invoke();
    }

    private void OnExchangeSwitched()
    {
        // Queued signals may belong to the previous exchange
        ClearGrid();
        LoadFromDatabase(_currentFilter);
    }

    private void OnSignalCreated(CryptoSignal signal)
    {
        SignalNotification.HandleCreatedSignal(signal);
    }

    private void ApplySort()
    {
        if (SortState.SortColumn is not { } col)
            return;

        lock (_lock)
        {
            var comparer = new SignalViewModelComparer(col);
            if (SortState.IsAscending)
                _signals.Sort(comparer);
            else
                _signals.Sort((a, b) => comparer.Compare(b, a));
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _pumpTimer?.Dispose();
        _pumpTimer = null;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        GlobalData.AnalyzeSignalCreated -= OnSignalCreated;
        GC.SuppressFinalize(this);
    }
}

internal class SignalViewModelComparer(SignalColumnEnum sortColumn) : IComparer<SignalViewModel>
{
    public int Compare(SignalViewModel? x, SignalViewModel? y)
    {
        if (x == null || y == null)
            return 0;

        var a = x.Object;
        var b = y.Object;

        int result = sortColumn switch
        {
            SignalColumnEnum.Id => a.Id.CompareTo(b.Id),
            SignalColumnEnum.Date => a.CloseDate.CompareTo(b.CloseDate),
            SignalColumnEnum.Exchange => string.Compare(a.Exchange?.Name, b.Exchange?.Name, StringComparison.OrdinalIgnoreCase),
            SignalColumnEnum.Symbol => string.Compare(a.Symbol?.Name, b.Symbol?.Name, StringComparison.OrdinalIgnoreCase),
            SignalColumnEnum.Side => a.Side.CompareTo(b.Side),
            SignalColumnEnum.Interval => (a.Interval?.Duration ?? 0).CompareTo(b.Interval?.Duration ?? 0),
            SignalColumnEnum.Strategy => string.Compare(a.StrategyText, b.StrategyText, StringComparison.OrdinalIgnoreCase),
            SignalColumnEnum.EventText => string.Compare(a.EventText, b.EventText, StringComparison.OrdinalIgnoreCase),
            SignalColumnEnum.SignalPrice => a.SignalPrice.CompareTo(b.SignalPrice),
            SignalColumnEnum.PriceChange => a.Last24HoursChange.CompareTo(b.Last24HoursChange),
            SignalColumnEnum.SignalVolume => a.SignalVolume.CompareTo(b.SignalVolume),
            SignalColumnEnum.TrendInterval => a.TrendInterval.CompareTo(b.TrendInterval),
            SignalColumnEnum.TrendPercentagePrimary => a.TrendPercentagePrimary.CompareTo(b.TrendPercentagePrimary),
            SignalColumnEnum.TrendPercentageSecondary => a.TrendPercentageSecondary.CompareTo(b.TrendPercentageSecondary),
            SignalColumnEnum.Last24HoursChange => a.Last24HoursChange.CompareTo(b.Last24HoursChange),
            SignalColumnEnum.LastXDaysEffective => a.LastXDaysEffective.CompareTo(b.LastXDaysEffective),
            SignalColumnEnum.BB => (a.BollingerBandsPercentage ?? 0).CompareTo(b.BollingerBandsPercentage ?? 0),
            SignalColumnEnum.BbLower => (a.BollingerBandsLowerBand ?? 0).CompareTo(b.BollingerBandsLowerBand ?? 0),
            SignalColumnEnum.BbUpper => (a.BollingerBandsUpperBand ?? 0).CompareTo(b.BollingerBandsUpperBand ?? 0),
            SignalColumnEnum.AvgBB => a.AvgBB.CompareTo(b.AvgBB),
            SignalColumnEnum.Rsi => (a.Rsi ?? 0).CompareTo(b.Rsi ?? 0),
            SignalColumnEnum.LuxIndicator5m => (a.LuxIndicator5m ?? 0).CompareTo(b.LuxIndicator5m ?? 0),
            SignalColumnEnum.MacdValue => (a.MacdValue ?? 0).CompareTo(b.MacdValue ?? 0),
            SignalColumnEnum.MacdSignal => (a.MacdSignal ?? 0).CompareTo(b.MacdSignal ?? 0),
            SignalColumnEnum.MacdHistogram => (a.MacdHistogram ?? 0).CompareTo(b.MacdHistogram ?? 0),
            SignalColumnEnum.StochOscillator => (a.StochOscillator ?? 0).CompareTo(b.StochOscillator ?? 0),
            SignalColumnEnum.StochSignal => (a.StochSignal ?? 0).CompareTo(b.StochSignal ?? 0),
            SignalColumnEnum.Sma200 => (a.Sma200 ?? 0).CompareTo(b.Sma200 ?? 0),
            SignalColumnEnum.Sma50 => (a.Sma50 ?? 0).CompareTo(b.Sma50 ?? 0),
            SignalColumnEnum.Sma20 => (a.Sma20 ?? 0).CompareTo(b.Sma20 ?? 0),
            SignalColumnEnum.PSar => (a.PSar ?? 0).CompareTo(b.PSar ?? 0),
            SignalColumnEnum.Trend15m => (a.Trend15m ?? 0).CompareTo(b.Trend15m ?? 0),
            SignalColumnEnum.Trend30m => (a.Trend30m ?? 0).CompareTo(b.Trend30m ?? 0),
            SignalColumnEnum.Trend1h => (a.Trend1h ?? 0).CompareTo(b.Trend1h ?? 0),
            SignalColumnEnum.Trend4h => (a.Trend4h ?? 0).CompareTo(b.Trend4h ?? 0),
            SignalColumnEnum.Trend1d => (a.Trend1d ?? 0).CompareTo(b.Trend1d ?? 0),
            SignalColumnEnum.Barometer15m => (a.Barometer15m ?? 0).CompareTo(b.Barometer15m ?? 0),
            SignalColumnEnum.Barometer30m => (a.Barometer30m ?? 0).CompareTo(b.Barometer30m ?? 0),
            SignalColumnEnum.Barometer1h => (a.Barometer1h ?? 0).CompareTo(b.Barometer1h ?? 0),
            SignalColumnEnum.Barometer4h => (a.Barometer4h ?? 0).CompareTo(b.Barometer4h ?? 0),
            SignalColumnEnum.Barometer1d => (a.Barometer1d ?? 0).CompareTo(b.Barometer1d ?? 0),
            SignalColumnEnum.MinimumEntry => a.MinEntry.CompareTo(b.MinEntry),
            // PriceMinPerc / PriceMaxPerc / SignalStatus exist as columns in the Avalonia XAML but
            // their viewmodel properties are commented out, so those cells render empty there too.
            _ => 0,
        };

        if (result == 0)
            result = string.Compare(a.Symbol?.Name, b.Symbol?.Name, StringComparison.OrdinalIgnoreCase);
        if (result == 0)
            result = -(a.Interval?.Duration ?? 0).CompareTo(b.Interval?.Duration ?? 0);
        if (result == 0)
            result = string.Compare(a.StrategyText, b.StrategyText, StringComparison.OrdinalIgnoreCase);
        if (result == 0)
            result = a.CloseDate.CompareTo(b.CloseDate);

        return result;
    }
}

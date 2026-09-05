using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.Trader;
using CryptoScanner.UI.ViewModels;

using Dapper;

using System.ComponentModel;

namespace CryptoScanner.UI.Services;

public class PositionService : IDisposable
{
    private const string OpenGridName = GridNames.PositionOpen;
    private const string ClosedGridName = GridNames.PositionClosed;
    private readonly object _lock = new();
    private readonly ApplicationStateService _stateService;
    private List<PositionViewModel> _openPositions = [];
    private List<PositionViewModel> _closedPositions = [];

    // Mirrors the Avalonia PositionOpenGridViewModel 15-second timer that clears the cached
    // display texts (profit, duration, break-even, invested/returned) so they get recalculated.
    private System.Threading.Timer? _refreshTimer;
    private bool _disposed;

    public GridSortState<PositionColumnEnum> OpenSortState { get; }
    public GridSortState<PositionColumnEnum> ClosedSortState { get; }

    // The selection is kept here instead of in the page: a tab switch disposes the page and takes
    // every field on it with it. Rows are matched on the position id, because a reload builds new
    // view models for the same positions.
    public GridSelectionState<PositionViewModel> OpenSelection { get; } = new(vm => vm.Object.Id);
    public GridSelectionState<PositionViewModel> ClosedSelection { get; } = new(vm => vm.Object.Id);

    public event Action? OpenPositionsChanged;
    public event Action? ClosedPositionsChanged;

    public PositionService(ApplicationStateService stateService)
    {
        _stateService = stateService;

        _stateService.RestoreGridSortState(OpenGridName, out var openSortCol, out var openSortDir);
        OpenSortState = !string.IsNullOrEmpty(openSortCol)
            ? new GridSortState<PositionColumnEnum>()
            : new GridSortState<PositionColumnEnum>(PositionColumnEnum.UpdateTime, ListSortDirection.Descending);
        OpenSortState.Restore(openSortCol, openSortDir);

        _stateService.RestoreGridSortState(ClosedGridName, out var closedSortCol, out var closedSortDir);
        ClosedSortState = !string.IsNullOrEmpty(closedSortCol)
            ? new GridSortState<PositionColumnEnum>()
            : new GridSortState<PositionColumnEnum>(PositionColumnEnum.CloseTime, ListSortDirection.Descending);
        ClosedSortState.Restore(closedSortCol, closedSortDir);
    }

    public IReadOnlyList<PositionViewModel> OpenPositions
    {
        get
        {
            lock (_lock)
                return _openPositions.ToList();
        }
    }

    public IReadOnlyList<PositionViewModel> ClosedPositions
    {
        get
        {
            lock (_lock)
                return _closedPositions.ToList();
        }
    }

    public void Start()
    {
        // The MVVM messages are the primary channel (they are what Core and the Avalonia commands
        // broadcast); the GlobalData.* delegates stay wired for the paths that only use those.
        WeakReferenceMessenger.Default.Register<PositionIsCreatedMessage>(this, (_, m) => OnPositionCreated(m.Position));
        WeakReferenceMessenger.Default.Register<PositionIsClosedMessage>(this, (_, m) => OnPositionClosed(m.Position));
        WeakReferenceMessenger.Default.Register<PositionIsDeletedMessage>(this, (_, m) => OnPositionDeleted(m.Position));
        WeakReferenceMessenger.Default.Register<PositionDeleteAllMessage>(this, (_, _) => OnPositionDeletedAll());
        WeakReferenceMessenger.Default.Register<ConfigurationChangedMessage>(this, (_, _) =>
        {
            OpenPositionsChanged?.Invoke();
            ClosedPositionsChanged?.Invoke();
        });
        WeakReferenceMessenger.Default.Register<ExchangeSwitchedMessage>(this, (_, _) => ReloadAll());

        // The exchange and its symbols are loaded asynchronously by ThreadLoadData, well after
        // this service starts. Reload once that finished, otherwise both grids stay empty.
        WeakReferenceMessenger.Default.Register<SymbolsHaveChangedMessage>(this, (_, _) => ReloadAll());

        GlobalData.PositionCreated += OnPositionCreated;
        GlobalData.PositionClosed += OnPositionClosed;
        GlobalData.PositionDeleted += OnPositionDeleted;
        GlobalData.PositionDeletedAll += OnPositionDeletedAll;

        ReloadAll();

        _refreshTimer = new System.Threading.Timer(_ =>
        {
            if (_disposed || GlobalData.ApplicationIsClosing)
                return;
            try
            {
                InvalidateOpenPositions();
            }
            catch
            {
            }
        }, null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
    }

    /// <summary>
    /// Reload both grids from the database.
    /// </summary>
    public void ReloadAll()
    {
        LoadOpenPositions();
        LoadClosedPositions();
    }

    /// <summary>
    /// Force a redraw of the open positions grid so profit, duration, break-even and the
    /// invested/returned totals are recalculated. Mirrors the Avalonia
    /// PositionOpenGridViewModel refresh timer — PositionViewModel computes every cell on
    /// demand here, so a render is all that is needed.
    /// </summary>
    public void InvalidateOpenPositions()
    {
        int count;
        lock (_lock)
            count = _openPositions.Count;

        if (count > 0)
            OpenPositionsChanged?.Invoke();
    }

    /// <summary>
    /// Sort the rows again with the values they have at this moment. The page calls this when its
    /// tab is opened: profit, profit percentage and duration follow the price, so a row that was
    /// sorted into place drifts out of order while the tab is away. Sorting while the tab is on
    /// screen would make the rows jump around under the mouse, hence only on entry.
    /// </summary>
    public void ResortOpen()
    {
        ApplySortOpen();
    }

    public void SortOpen(PositionColumnEnum column)
    {
        OpenSortState.ToggleSort(column);
        ApplySortOpen();
        _stateService.SaveGridSortState(OpenGridName, OpenSortState.SortColumnName, OpenSortState.SortDirection);
        OpenPositionsChanged?.Invoke();
    }

    public void SortClosed(PositionColumnEnum column)
    {
        ClosedSortState.ToggleSort(column);
        ApplySortClosed();
        _stateService.SaveGridSortState(ClosedGridName, ClosedSortState.SortColumnName, ClosedSortState.SortDirection);
        ClosedPositionsChanged?.Invoke();
    }

    private void OnPositionCreated(CryptoPosition position)
    {
        lock (_lock)
        {
            // PositionMonitor announces a new position twice: once as an MVVM message and once
            // through the GlobalData delegate, and this service listens to both. Closing and
            // deleting already look the position up by id; creating did not, so every position
            // ended up in the grid twice.
            if (_openPositions.Any(p => p.Object.Id == position.Id))
                return;

            _openPositions.Add(new PositionViewModel(position));
        }
        ApplySortOpen();
        OpenPositionsChanged?.Invoke();
    }

    private void OnPositionClosed(CryptoPosition position)
    {
        bool openChanged = false;
        lock (_lock)
        {
            var vm = _openPositions.FirstOrDefault(p => p.Object.Id == position.Id);
            if (vm != null)
            {
                _openPositions.Remove(vm);
                OpenSelection.Remove(vm);
                openChanged = true;
            }
            // Closing is announced twice as well (MVVM message plus delegate), so guard the insert
            if (!_closedPositions.Any(p => p.Object.Id == position.Id))
                _closedPositions.Insert(0, new PositionViewModel(position));
        }
        ApplySortClosed();
        if (openChanged)
            OpenPositionsChanged?.Invoke();
        ClosedPositionsChanged?.Invoke();
    }

    private void OnPositionDeleted(CryptoPosition position)
    {
        bool openChanged = false;
        bool closedChanged = false;
        lock (_lock)
        {
            var openVm = _openPositions.FirstOrDefault(p => p.Object.Id == position.Id);
            if (openVm != null)
            {
                _openPositions.Remove(openVm);
                OpenSelection.Remove(openVm);
                openChanged = true;
            }
            var closedVm = _closedPositions.FirstOrDefault(p => p.Object.Id == position.Id);
            if (closedVm != null)
            {
                _closedPositions.Remove(closedVm);
                ClosedSelection.Remove(closedVm);
                closedChanged = true;
            }
        }
        if (openChanged)
            OpenPositionsChanged?.Invoke();
        if (closedChanged)
            ClosedPositionsChanged?.Invoke();
    }

    private void OnPositionDeletedAll()
    {
        lock (_lock)
        {
            _openPositions.Clear();
            _closedPositions.Clear();
            OpenSelection.Clear();
            ClosedSelection.Clear();
        }
        OpenPositionsChanged?.Invoke();
        ClosedPositionsChanged?.Invoke();
    }

    private void LoadOpenPositions()
    {
        try
        {
            if (GlobalData.ActiveExchange == null)
                return;

            List<PositionViewModel> viewModels = [];
            using var database = new CryptoDatabase();
            string sql = "select * from position where exchangeid=@exchangeid and closetime is null and status < 2";
            foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql, new { exchangeid = GlobalData.ActiveExchange.Id }))
            {
                // AddPosition is idempotent on the position id, so reloading does not duplicate
                // the entries ThreadLoadData already put in exchange.Data.PositionList. It returns
                // the position that list holds, which is the live one the trader keeps updating.
                // The row has to follow that instance: bound to the copy read from the database the
                // grid froze on the values of the reload, so a dca filling an hour later - the
                // hourly exchange refresh sends SymbolsHaveChangedMessage and lands here - never
                // showed up in invested, quantity, break-even or parts.
                CryptoPosition livePosition = PositionTools.AddPosition(position);
                if (ReferenceEquals(livePosition, position))
                    PositionTools.LoadPosition(database, position);
                viewModels.Add(new PositionViewModel(livePosition));
            }
            lock (_lock)
            {
                _openPositions = viewModels;
            }
            ApplySortOpen();
            OpenSelection.Rebind(OpenPositions);
            OpenPositionsChanged?.Invoke();
        }
        catch
        {
        }
    }

    private void LoadClosedPositions()
    {
        try
        {
            if (GlobalData.ActiveExchange == null)
                return;

            string sql = "select * from position where exchangeid=@exchangeid and not closetime is null order by id desc";
            if (!GlobalData.IsEmulatorMode)
                sql += " limit 500";
            using var database = new CryptoDatabase();

            List<PositionViewModel> viewModels = [];
            foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql, new { exchangeid = GlobalData.ActiveExchange.Id }))
            {
                if (GlobalData.ExchangeListId.TryGetValue(position.ExchangeId, out Core.Model.CryptoExchange? exchange))
                {
                    position.Exchange = exchange;
                    if (exchange!.SymbolListId.TryGetValue(position.SymbolId, out CryptoSymbol? symbol))
                    {
                        position.Symbol = symbol;
                        if (GlobalData.IntervalListId.TryGetValue((int)position.IntervalId!, out CryptoInterval? interval))
                            position.Interval = interval!;

                        viewModels.Add(new PositionViewModel(position));
                    }
                }
            }
            lock (_lock)
            {
                _closedPositions = viewModels;
            }
            ApplySortClosed();
            ClosedSelection.Rebind(ClosedPositions);
            ClosedPositionsChanged?.Invoke();
        }
        catch
        {
        }
    }

    private void ApplySortOpen()
    {
        if (OpenSortState.SortColumn is not { } col)
            return;

        lock (_lock)
        {
            var comparer = new PositionViewModelComparer(col);
            if (OpenSortState.IsAscending)
                _openPositions.Sort(comparer);
            else
                _openPositions.Sort((a, b) => comparer.Compare(b, a));
        }
    }

    private void ApplySortClosed()
    {
        if (ClosedSortState.SortColumn is not { } col)
            return;

        lock (_lock)
        {
            var comparer = new PositionViewModelComparer(col);
            if (ClosedSortState.IsAscending)
                _closedPositions.Sort(comparer);
            else
                _closedPositions.Sort((a, b) => comparer.Compare(b, a));
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _refreshTimer?.Dispose();
        _refreshTimer = null;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        GlobalData.PositionCreated -= OnPositionCreated;
        GlobalData.PositionClosed -= OnPositionClosed;
        GlobalData.PositionDeleted -= OnPositionDeleted;
        GlobalData.PositionDeletedAll -= OnPositionDeletedAll;
        GC.SuppressFinalize(this);
    }
}

internal class PositionViewModelComparer(PositionColumnEnum sortColumn) : IComparer<PositionViewModel>
{
    public int Compare(PositionViewModel? x, PositionViewModel? y)
    {
        if (x == null || y == null)
            return 0;

        var a = x.Object;
        var b = y.Object;

        int result = sortColumn switch
        {
            PositionColumnEnum.Id => a.Id.CompareTo(b.Id),
            PositionColumnEnum.AltradyId => string.Compare(a.AltradyPositionId, b.AltradyPositionId, StringComparison.OrdinalIgnoreCase),
            PositionColumnEnum.CreateTime => a.CreateTime.CompareTo(b.CreateTime),
            PositionColumnEnum.UpdateTime => (a.UpdateTime ?? DateTime.MinValue).CompareTo(b.UpdateTime ?? DateTime.MinValue),
            PositionColumnEnum.CloseTime => (a.CloseTime ?? DateTime.MinValue).CompareTo(b.CloseTime ?? DateTime.MinValue),
            PositionColumnEnum.Duration => string.Compare(a.DurationText(), b.DurationText(), StringComparison.Ordinal),
            PositionColumnEnum.Exchange => string.Compare(a.Exchange?.Name, b.Exchange?.Name, StringComparison.OrdinalIgnoreCase),
            PositionColumnEnum.Symbol => string.Compare(a.Symbol?.Name, b.Symbol?.Name, StringComparison.OrdinalIgnoreCase),
            PositionColumnEnum.Interval => (a.Interval?.Duration ?? 0).CompareTo(b.Interval?.Duration ?? 0),
            PositionColumnEnum.Side => a.Side.CompareTo(b.Side),
            PositionColumnEnum.Strategy => string.Compare(a.StrategyText, b.StrategyText, StringComparison.OrdinalIgnoreCase),
            PositionColumnEnum.Status => a.Status.CompareTo(b.Status),

            PositionColumnEnum.Invested => a.Invested.CompareTo(b.Invested),
            PositionColumnEnum.Returned => a.Returned.CompareTo(b.Returned),
            PositionColumnEnum.Commission => a.Commission.CompareTo(b.Commission),
            PositionColumnEnum.BreakEvenPrice => a.BreakEvenPrice.CompareTo(b.BreakEvenPrice),
            PositionColumnEnum.BreakEvenPercent => a.CurrentBreakEvenPercentage().CompareTo(b.CurrentBreakEvenPercentage()),
            PositionColumnEnum.Quantity => a.Quantity.CompareTo(b.Quantity),
            PositionColumnEnum.Open => (a.Invested - a.Returned - a.Commission).CompareTo(b.Invested - b.Returned - b.Commission),
            PositionColumnEnum.CurrentProfit => a.CurrentProfit().CompareTo(b.CurrentProfit()),
            PositionColumnEnum.CurrentProfitPercentage => a.CurrentProfitPercentage().CompareTo(b.CurrentProfitPercentage()),
            PositionColumnEnum.Parts => string.Compare(a.PartCountText(), b.PartCountText(), StringComparison.Ordinal),
            PositionColumnEnum.EntryPrice => (a.EntryPrice ?? 0).CompareTo(b.EntryPrice ?? 0),
            PositionColumnEnum.ProfitPrice => (a.ProfitPrice ?? 0).CompareTo(b.ProfitPrice ?? 0),
            PositionColumnEnum.FundingRate => a.Symbol.FundingRate.CompareTo(b.Symbol.FundingRate),
            PositionColumnEnum.RemainingDust => a.RemainingDust.CompareTo(b.RemainingDust),

            PositionColumnEnum.SignalDate => a.SignalEventTime.CompareTo(b.SignalEventTime),
            PositionColumnEnum.SignalPrice => a.SignalPrice.CompareTo(b.SignalPrice),
            PositionColumnEnum.SignalVolume => a.SignalVolume.CompareTo(b.SignalVolume),

            PositionColumnEnum.TrendPercentagePrimary => a.TrendPercentagePrimary.CompareTo(b.TrendPercentagePrimary),
            PositionColumnEnum.TrendPercentageSecondary => a.TrendPercentageSecondary.CompareTo(b.TrendPercentageSecondary),
            PositionColumnEnum.Last24HoursChange => a.Last24HoursChange.CompareTo(b.Last24HoursChange),

            PositionColumnEnum.BB => (a.BollingerBandsPercentage ?? 0).CompareTo(b.BollingerBandsPercentage ?? 0),
            PositionColumnEnum.Rsi => (a.Rsi ?? 0).CompareTo(b.Rsi ?? 0),
            PositionColumnEnum.MinimumEntry => a.MinEntry.CompareTo(b.MinEntry),

            _ => 0,
        };

        if (result == 0)
            result = string.Compare(a.Symbol?.Name, b.Symbol?.Name, StringComparison.OrdinalIgnoreCase);
        if (result == 0)
            result = a.CreateTime.CompareTo(b.CreateTime);

        return result;
    }
}

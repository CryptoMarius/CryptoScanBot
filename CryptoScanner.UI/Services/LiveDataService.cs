using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal.Indicators;
using CryptoScanner.Core.Trader;
using CryptoScanner.UI.ViewModels;

using System.ComponentModel;

namespace CryptoScanner.UI.Services;

public class LiveDataService : IDisposable
{
    private const string GridName = GridNames.LiveData;

    // The Avalonia grid is bounded by the lifetime of the window; here the list would grow
    // forever, so cap it the same way the log grid is capped.
    private const int MaxLiveDataRows = 5000;

    private readonly object _lock = new();
    private readonly ApplicationStateService _stateService;
    private List<LiveDataViewModel> _liveData = [];

    // Pump lives in the service, not in LiveData.razor: GlobalData.LiveDataQueue used to grow
    // unbounded whenever the Live Data tab was closed.
    private System.Threading.Timer? _pumpTimer;
    private bool _disposed;

    public GridSortState<LiveDataColumnEnum> SortState { get; }

    public event Action? LiveDataChanged;

    public LiveDataService(ApplicationStateService stateService)
    {
        _stateService = stateService;

        _stateService.RestoreGridSortState(GridName, out var sortColumn, out var sortDirection);
        SortState = !string.IsNullOrEmpty(sortColumn)
            ? new GridSortState<LiveDataColumnEnum>()
            : new GridSortState<LiveDataColumnEnum>(LiveDataColumnEnum.Date, ListSortDirection.Descending);
        SortState.Restore(sortColumn, sortDirection);
    }

    public IReadOnlyList<LiveDataViewModel> LiveData
    {
        get
        {
            lock (_lock)
                return _liveData.ToList();
        }
    }

    public void Start()
    {
        WeakReferenceMessenger.Default.Register<ConfigurationChangedMessage>(this, (_, _) => LiveDataChanged?.Invoke());
        WeakReferenceMessenger.Default.Register<ExchangeSwitchedMessage>(this, (_, _) => Clear());

        _pumpTimer = new System.Threading.Timer(_ =>
        {
            if (_disposed || GlobalData.ApplicationIsClosing)
                return;
            try
            {
                ProcessPendingLiveData();
            }
            catch
            {
            }
        }, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
    }

    public void Sort(LiveDataColumnEnum column)
    {
        SortState.ToggleSort(column);
        ApplySort();
        _stateService.SaveGridSortState(GridName, SortState.SortColumnName, SortState.SortDirection);
        LiveDataChanged?.Invoke();
    }

    public void ProcessPendingLiveData()
    {
        bool changed = false;

        if (GlobalData.LiveDataQueue.Count > 0)
        {
            if (Monitor.TryEnter(GlobalData.LiveDataQueue))
            {
                try
                {
                    lock (_lock)
                    {
                        while (GlobalData.LiveDataQueue.Count > 0)
                        {
                            CryptoLiveData liveData = GlobalData.LiveDataQueue.Dequeue();
                            if (liveData != null)
                            {
                                if (!(TradingConfig.Signals[CryptoTradeSide.Long].InBlackList(liveData.Symbol.Name) == MatchBlackAndWhiteList.Present ||
                                    TradingConfig.Signals[CryptoTradeSide.Short].InBlackList(liveData.Symbol.Name) == MatchBlackAndWhiteList.Present))
                                {
                                    _liveData.Insert(0, new LiveDataViewModel(liveData));
                                    changed = true;
                                }
                            }
                        }

                        if (_liveData.Count > MaxLiveDataRows)
                            _liveData.RemoveRange(MaxLiveDataRows, _liveData.Count - MaxLiveDataRows);
                    }
                }
                finally
                {
                    Monitor.Exit(GlobalData.LiveDataQueue);
                }
            }
        }

        if (changed)
        {
            ApplySort();
            LiveDataChanged?.Invoke();
        }
    }

    public void Clear()
    {
        lock (_lock)
            _liveData.Clear();
        LiveDataChanged?.Invoke();
    }

    private void ApplySort()
    {
        if (SortState.SortColumn is not { } col)
            return;

        lock (_lock)
        {
            var comparer = new LiveDataViewModelComparer(col);
            if (SortState.IsAscending)
                _liveData.Sort(comparer);
            else
                _liveData.Sort((a, b) => comparer.Compare(b, a));
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _pumpTimer?.Dispose();
        _pumpTimer = null;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        GC.SuppressFinalize(this);
    }
}

internal class LiveDataViewModelComparer(LiveDataColumnEnum sortColumn) : IComparer<LiveDataViewModel>
{
    public int Compare(LiveDataViewModel? x, LiveDataViewModel? y)
    {
        if (x == null || y == null)
            return 0;

        var a = x.Object;
        var b = y.Object;

        int result = sortColumn switch
        {
            LiveDataColumnEnum.Date => a.Candle.Date.CompareTo(b.Candle.Date),
            LiveDataColumnEnum.Exchange => string.Compare(a.Symbol.Exchange.Name, b.Symbol.Exchange.Name, StringComparison.OrdinalIgnoreCase),
            LiveDataColumnEnum.Symbol => string.Compare(a.Symbol.Name, b.Symbol.Name, StringComparison.OrdinalIgnoreCase),
            LiveDataColumnEnum.Volume => a.Symbol.Volume.CompareTo(b.Symbol.Volume),
            LiveDataColumnEnum.Interval => a.Interval.Duration.CompareTo(b.Interval.Duration),
            LiveDataColumnEnum.Price => a.Candle.Close.CompareTo(b.Candle.Close),
            LiveDataColumnEnum.BB => (a.CandleData?.BollingerBandsPercentage ?? 0).CompareTo(b.CandleData?.BollingerBandsPercentage ?? 0),
            LiveDataColumnEnum.BbLower => (a.CandleData?.BollingerBandsLowerBand ?? 0).CompareTo(b.CandleData?.BollingerBandsLowerBand ?? 0),
            LiveDataColumnEnum.BbUpper => (a.CandleData?.BollingerBandsUpperBand ?? 0).CompareTo(b.CandleData?.BollingerBandsUpperBand ?? 0),
            LiveDataColumnEnum.RangeIndex => (BandRangeOf(a)?.Index ?? 0).CompareTo(BandRangeOf(b)?.Index ?? 0),
            LiveDataColumnEnum.RangeCount => (BandRangeOf(a)?.MeasurementCount ?? 0).CompareTo(BandRangeOf(b)?.MeasurementCount ?? 0),
            LiveDataColumnEnum.Rsi => (a.CandleData?.Rsi ?? 0).CompareTo(b.CandleData?.Rsi ?? 0),
            LiveDataColumnEnum.LuxIndicator5m => (a.CandleData?.Lux5mValue ?? 0).CompareTo(b.CandleData?.Lux5mValue ?? 0),
            LiveDataColumnEnum.MacdValue => (a.CandleData?.MacdValue ?? 0).CompareTo(b.CandleData?.MacdValue ?? 0),
            LiveDataColumnEnum.MacdSignal => (a.CandleData?.MacdSignal ?? 0).CompareTo(b.CandleData?.MacdSignal ?? 0),
            LiveDataColumnEnum.MacdHistogram => (a.CandleData?.MacdHistogram ?? 0).CompareTo(b.CandleData?.MacdHistogram ?? 0),
            LiveDataColumnEnum.StochOscillator => (a.CandleData?.StochOscillator ?? 0).CompareTo(b.CandleData?.StochOscillator ?? 0),
            LiveDataColumnEnum.StochSignal => (a.CandleData?.StochSignal ?? 0).CompareTo(b.CandleData?.StochSignal ?? 0),
            LiveDataColumnEnum.Sma200 => (a.CandleData?.Sma200 ?? 0).CompareTo(b.CandleData?.Sma200 ?? 0),
            LiveDataColumnEnum.Sma50 => (a.CandleData?.Sma50 ?? 0).CompareTo(b.CandleData?.Sma50 ?? 0),
            LiveDataColumnEnum.Sma20 => (a.CandleData?.Sma20 ?? 0).CompareTo(b.CandleData?.Sma20 ?? 0),
            LiveDataColumnEnum.PSar => (a.CandleData?.PSar ?? 0).CompareTo(b.CandleData?.PSar ?? 0),
            LiveDataColumnEnum.FundingRate => a.Symbol.FundingRate.CompareTo(b.Symbol.FundingRate),
            _ => 0,
        };

        if (result == 0)
            result = string.Compare(a.Symbol.Name, b.Symbol.Name, StringComparison.OrdinalIgnoreCase);
        if (result == 0)
            result = a.Candle.Date.CompareTo(b.Candle.Date);

        return result;
    }

    private static BandRangeTracker? BandRangeOf(CryptoLiveData liveData)
        => liveData.Symbol.GetSymbolInterval(liveData.Interval.IntervalPeriod).BandRange;
}

using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.Signal;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.UI.Services;

public class DashboardService : IDisposable
{
    private readonly ApplicationStateService _stateService;
    private bool _disposed;

    public DashboardService(ApplicationStateService stateService)
    {
        _stateService = stateService;
    }

    public string ExchangeName { get; private set; } = "";

    private string _quoteName = "USDT";

    /// <summary>
    /// Selected barometer quote. Persisted in the application state and forwarded to SignalR,
    /// same as the Avalonia DashBoardInformationViewModel does.
    /// </summary>
    public string QuoteName
    {
        get => _quoteName;
        set
        {
            if (_quoteName == value || string.IsNullOrEmpty(value))
                return;
            _quoteName = value;
            _stateService.BarometerQuote = value;
            if (GlobalData.SignalRService != null)
                GlobalData.SignalRService.SelectedQuote = value;
            RefreshTopSymbolsList();
            UpdateBarometerChart();
            DashboardChanged?.Invoke();
        }
    }

    private string _intervalName = "1h";

    /// <summary>
    /// Selected barometer interval — drives which candle list the barometer graph is drawn from.
    /// </summary>
    public string IntervalName
    {
        get => _intervalName;
        set
        {
            if (_intervalName == value || string.IsNullOrEmpty(value))
                return;
            _intervalName = value;
            _stateService.BarometerInterval = value;
            if (GlobalData.SignalRService != null)
                GlobalData.SignalRService.SelectedInterval = value;
            UpdateBarometerChart();
            DashboardChanged?.Invoke();
        }
    }

    /// <summary>Quotes that actually have symbols; rebuilt whenever the symbol list changes.</summary>
    public List<string> QuoteOptions { get; private set; } = ["USDT"];

    /// <summary>The intervals the Avalonia dashboard offers for the barometer graph.</summary>
    public static IReadOnlyList<string> IntervalOptions { get; } = ["1h", "4h", "1d"];

    public string KlineTickerCount { get; private set; } = "-";
    public string ScannerExecuteCount { get; private set; } = "-";
    public string ScannerSignalCount { get; private set; } = "-";
    public string ScannerPositionCount { get; private set; } = "-";
    public string CandleProgressText { get; private set; } = "";

    /// <summary>"HH:mm" once running, otherwise the raw application status (Avalonia parity).</summary>
    public string ApplicationStatusText { get; private set; } = "";

    /// <summary>Time of the most recently calculated barometer candle.</summary>
    public string BarometerTime { get; private set; } = "";

    public string Barometer1h { get; private set; } = "-";
    public string Barometer4h { get; private set; } = "-";
    public string Barometer1d { get; private set; } = "-";
    public string Barometer1hClass { get; private set; } = "";
    public string Barometer4hClass { get; private set; } = "";
    public string Barometer1dClass { get; private set; } = "";

    // Traffic lights. Avalonia only turns them green when the setting is on AND the scanner is
    // actually running, and the Rulez light follows the PauseTrading state of the exchange.
    public bool ScannerActive => GlobalData.Settings.Signal.Active && IsRunning;
    public bool TraderActive => GlobalData.Settings.Trading.Active && IsRunning;
    public bool SoundsActive => GlobalData.Settings.Signal.SoundsActive && IsRunning;

    public bool RulezActive
    {
        get
        {
            var exchange = GlobalData.ActiveExchange;
            if (exchange == null)
                return false;
            var pause = exchange.Data.PauseTrading;
            return pause.Calculated.HasValue && !(pause.Until.HasValue && pause.Until > DateTime.UtcNow);
        }
    }

    private static bool IsRunning => GlobalData.ApplicationStatus == CryptoApplicationStatus.Running;

    public List<DashboardSymbolInfo> TopSymbols { get; private set; } = [];

    public List<BarometerPoint> BarometerChartPoints { get; private set; } = [];

    /// <summary>
    /// CSS class for the Rulez dot. Avalonia has three states here (see
    /// DashBoardInformationViewModel): green when trading is not paused, red when it is, and
    /// neutral while there is no active exchange at all.
    /// </summary>
    public string RulezDotClass =>
        GlobalData.ActiveExchange == null ? "neutral" : RulezActive ? "active" : "";

    public event Action? DashboardChanged;

    // The three toggles live here because both the File menu (MainLayout) and the traffic lights
    // (DashboardPanel) drive them. Each one broadcasts StatusesHaveChangedMessage so the menu
    // checkboxes, the traffic lights and any other subscriber stay in sync (same contract as the
    // Avalonia dashboard).
    public void ToggleScanner()
    {
        GlobalData.Settings.Signal.Active = !GlobalData.Settings.Signal.Active;
        GlobalData.SendMvvmMessage(new StatusesHaveChangedMessage());
    }

    public void ToggleTrader()
    {
        GlobalData.Settings.Trading.Active = !GlobalData.Settings.Trading.Active;
        GlobalData.SendMvvmMessage(new StatusesHaveChangedMessage());
    }

    public void ToggleSounds()
    {
        GlobalData.Settings.Signal.SoundsActive = !GlobalData.Settings.Signal.SoundsActive;
        GlobalData.SendMvvmMessage(new StatusesHaveChangedMessage());
    }

    public void Start()
    {
        // Restore the persisted barometer selection before anything reads it
        if (!string.IsNullOrEmpty(_stateService.BarometerQuote))
            _quoteName = _stateService.BarometerQuote;
        if (!string.IsNullOrEmpty(_stateService.BarometerInterval) && IntervalOptions.Contains(_stateService.BarometerInterval))
            _intervalName = _stateService.BarometerInterval;

        WeakReferenceMessenger.Default.Register<SymbolsHaveChangedMessage>(this, (_, _) => OnSymbolsChanged());
        WeakReferenceMessenger.Default.Register<ExchangeSwitchedMessage>(this, (_, _) => OnExchangeSwitched());
        WeakReferenceMessenger.Default.Register<StatusesHaveChangedMessage>(this, (_, _) => DashboardChanged?.Invoke());
        WeakReferenceMessenger.Default.Register<BarometerRefreshMessage>(this, (_, _) => OnBarometerRefreshed());
        WeakReferenceMessenger.Default.Register<ConfigurationChangedMessage>(this, (_, _) => OnSymbolsChanged());

        RefreshExchangeName();
        RefreshQuoteOptions();
        RefreshTopSymbolsList();
    }

    private void OnSymbolsChanged()
    {
        RefreshQuoteOptions();
        RefreshTopSymbolsList();
        UpdateBarometerChart();
        DashboardChanged?.Invoke();
    }

    private void OnExchangeSwitched()
    {
        BarometerChartPoints = [];
        GlobalData.CreatedSignalCount = 0;
        SignalExecute.ResetAnalyseCount();
        ExchangeBase.KLineTicker?.Reset();

        RefreshExchangeName();
        RefreshQuoteOptions();

        // Switch to the default quote of the new exchange if it is available
        string? defaultQuote = ExchangeBase.ExchangeOptions.DefaultQuote;
        if (!string.IsNullOrEmpty(defaultQuote) && QuoteOptions.Contains(defaultQuote))
            QuoteName = defaultQuote;

        RefreshTopSymbolsList();
        DashboardChanged?.Invoke();
    }

    private void OnBarometerRefreshed()
    {
        if (_disposed)
            return;
        bool changed = UpdateBarometers();
        changed |= UpdateBarometerChart();
        changed |= UpdateBarometerTime();
        if (changed)
            DashboardChanged?.Invoke();
    }

    public void Poll()
    {
        if (_disposed)
            return;

        bool changed = false;

        changed |= UpdateTickers();
        changed |= UpdateBarometers();
        changed |= UpdateCryptoPrices();
        changed |= UpdateExchangeName();
        changed |= UpdateApplicationStatus();
        // Refresh the chart on every tick instead of only in PollSlow. PollSlow runs once at
        // startup (when no candles are loaded yet) and then only once a minute, which is why the
        // loading animation kept spinning for a minute or two after the candles were already in.
        // This reads the last N candles from a list that is already in memory, so it is cheap.
        changed |= UpdateBarometerChart();
        changed |= UpdateBarometerTime();

        var progress = GlobalData.CandleProgressText ?? "";
        if (progress != CandleProgressText)
        {
            CandleProgressText = progress;
            changed = true;
        }

        if (changed)
            DashboardChanged?.Invoke();
    }

    public void PollSlow()
    {
        if (_disposed)
            return;

        bool changed = UpdateBarometers();
        changed |= UpdateCryptoPrices();

        if (changed)
            DashboardChanged?.Invoke();
    }

    private bool UpdateApplicationStatus()
    {
        // Avalonia puts the clock on this row, but the clock now sits in the barometer chart. A
        // second copy next to the sound light said nothing; the light itself is about the sounds.
        // While the application is not running the status ("Starting", "Stopping") still wins,
        // because that is the only place it shows.
        string text = IsRunning
            ? "Sounds"
            : GlobalData.ApplicationStatus.ToString();

        if (text != ApplicationStatusText)
        {
            ApplicationStatusText = text;
            return true;
        }
        return false;
    }

    private bool UpdateExchangeName()
    {
        var name = GlobalData.ActiveExchange?.Name ?? "";
        if (name != ExchangeName)
        {
            ExchangeName = name;
            RefreshQuoteOptions();
            RefreshTopSymbolsList();
            return true;
        }
        return false;
    }

    private void RefreshExchangeName()
    {
        ExchangeName = GlobalData.ActiveExchange?.Name ?? "";
    }

    /// <summary>
    /// Rebuild the quote dropdown from the quotes that fetch candles and actually have symbols.
    /// Called again on every SymbolsHaveChangedMessage because at startup the symbols are not
    /// loaded yet and the list would otherwise be stuck on the "USDT" fallback.
    /// </summary>
    public void RefreshQuoteOptions()
    {
        List<string> quotes = [];
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
                quotes.Add(quoteData.Name);
        }
        if (quotes.Count == 0)
            quotes.Add("USDT");
        QuoteOptions = quotes;

        if (!quotes.Contains(_quoteName))
        {
            _quoteName = quotes[0];
            _stateService.BarometerQuote = _quoteName;
            if (GlobalData.SignalRService != null)
                GlobalData.SignalRService.SelectedQuote = _quoteName;
        }
    }

    private bool UpdateTickers()
    {
        bool changed = false;

        var klineCount = ExchangeBase.KLineTicker?.Count().ToString("N0") ?? "-";
        if (klineCount != KlineTickerCount)
        {
            KlineTickerCount = klineCount;
            changed = true;
        }

        var analyzeCount = SignalExecute.AnalyseCount.ToString("N0");
        if (analyzeCount != ScannerExecuteCount)
        {
            ScannerExecuteCount = analyzeCount;
            changed = true;
        }

        var signalCount = GlobalData.CreatedSignalCount.ToString("N0");
        if (signalCount != ScannerSignalCount)
        {
            ScannerSignalCount = signalCount;
            changed = true;
        }

        var posCount = GetPositionCountText();
        if (posCount != ScannerPositionCount)
        {
            ScannerPositionCount = posCount;
            changed = true;
        }

        return changed;
    }

    private static string GetPositionCountText()
    {
        if (!GlobalData.Settings.Trading.Active)
            return "";

        var exchange = GlobalData.ActiveExchange;
        if (exchange == null)
            return "-";

        int positionCount = 0;
        if (exchange.Data.PositionList.Count != 0)
        {
            foreach (var position in exchange.Data.PositionList.Values)
            {
                positionCount++;
            }
        }

        return $"({GlobalData.Settings.Trading.SlotsMaximalLong}/{GlobalData.Settings.Trading.SlotsMaximalShort}) {positionCount}";
    }

    private bool UpdateBarometers()
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null)
            return false;

        var quote = QuoteName;
        bool changed = false;

        changed |= UpdateSingleBarometer(exchange, quote, CryptoIntervalPeriod.interval1h,
            ref _barometer1h, ref _barometer1hClass, v => { Barometer1h = v; }, c => { Barometer1hClass = c; });
        changed |= UpdateSingleBarometer(exchange, quote, CryptoIntervalPeriod.interval4h,
            ref _barometer4h, ref _barometer4hClass, v => { Barometer4h = v; }, c => { Barometer4hClass = c; });
        changed |= UpdateSingleBarometer(exchange, quote, CryptoIntervalPeriod.interval1d,
            ref _barometer1d, ref _barometer1dClass, v => { Barometer1d = v; }, c => { Barometer1dClass = c; });

        return changed;
    }

    private string _barometer1h = "-", _barometer4h = "-", _barometer1d = "-";
    private string _barometer1hClass = "", _barometer4hClass = "", _barometer1dClass = "";

    private static bool UpdateSingleBarometer(Exchange exchange, string quote,
        CryptoIntervalPeriod period, ref string currentValue, ref string currentClass,
        Action<string> setValue, Action<string> setClass)
    {
        var barometer = exchange.Data.GetBarometer(quote, period);
        if (barometer?.PriceBarometer == null)
            return false;

        var val = barometer.PriceBarometer.Value;
        var text = val.ToString("N2") + "%";
        var css = val > 0 ? "text-green" : val < 0 ? "text-red" : "";

        if (text != currentValue || css != currentClass)
        {
            currentValue = text;
            currentClass = css;
            setValue(text);
            setClass(css);
            return true;
        }
        return false;
    }

    private bool UpdateCryptoPrices()
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null)
            return false;

        // Rebuilt here rather than only when the exchange name changes: that happens once at
        // startup, often before the symbol list has finished loading, and every coin that could
        // not be resolved at that moment stayed missing for the rest of the session.
        RefreshTopSymbolsList();
        if (TopSymbols.Count == 0)
            return false;

        bool changed = false;
        foreach (var info in TopSymbols)
        {
            if (info.Symbol == null)
                continue;

            decimal? price = info.Symbol.LastPrice ?? LastCandleClose(info.Symbol);

            if (price == null)
            {
                // No price yet (or ever): the row drops out and the next coin moves up
                if (info.HasPrice)
                {
                    info.HasPrice = false;
                    changed = true;
                }
                continue;
            }

            if (!info.HasPrice)
            {
                info.HasPrice = true;
                changed = true;
            }

            var priceText = price.Value.ToString(info.Symbol.PriceDisplayFormat ?? "N2");
            if (priceText != info.PriceText)
            {
                var css = price > info.LastKnownPrice ? "text-green"
                        : price < info.LastKnownPrice ? "text-red" : info.ColorClass;
                info.LastKnownPrice = price.Value;
                info.PriceText = priceText;
                info.ColorClass = css;
                changed = true;
            }

            var vol = info.Symbol.Volume;
            var volText = vol > 0 ? vol.ToString("N0") : "-";
            if (volText != info.VolumeText)
            {
                info.VolumeText = volText;
                changed = true;
            }
        }
        return changed;
    }

    /// <summary>
    /// Newest close price from whichever interval has candles. Only the 1 minute list used to be
    /// consulted, so a coin whose 1m candles are not fetched showed no price at all even though
    /// its hourly or daily candles were in memory.
    /// </summary>
    private static decimal? LastCandleClose(CryptoSymbol symbol)
    {
        foreach (var interval in GlobalData.IntervalList)
        {
            var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            if (symbolInterval?.CandleList == null)
                continue;

            lock (symbolInterval.CandleList)
            {
                if (symbolInterval.CandleList.Count > 0)
                    return symbolInterval.CandleList.Values.Last().Close;
            }
        }
        return null;
    }

    private void RefreshTopSymbolsList()
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null)
        {
            if (TopSymbols.Count > 0)
                TopSymbols = [];
            return;
        }

        var quote = QuoteName;
        var names = GlobalData.Settings.ShowSymbolInformation;
        var list = new List<DashboardSymbolInfo>();

        foreach (var baseName in names)
        {
            // Same fallback as Avalonia: if the coin is not listed against the selected quote,
            // show the USDT pair instead of an empty row.
            if (!exchange.SymbolListName.TryGetValue(baseName + quote, out CryptoSymbol? symbol))
                exchange.SymbolListName.TryGetValue(baseName + "USDT", out symbol);

            // A coin the exchange does not list has no price to show. Avalonia leaves the row out
            // altogether; adding it anyway put a "PAXG - -" line on the dashboard.
            if (symbol == null)
                continue;

            // Carry the running price and colour over, so a rebuild does not reset the row
            var existing = TopSymbols.FirstOrDefault(t => t.Symbol == symbol);
            list.Add(existing ?? new DashboardSymbolInfo
            {
                BaseName = baseName,
                Symbol = symbol,
            });
        }

        // Nothing to do when the same symbols came out, otherwise the rows would be replaced on
        // every tick and their price/colour state thrown away.
        if (list.Count == TopSymbols.Count)
        {
            bool equal = true;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Symbol != TopSymbols[i].Symbol)
                {
                    equal = false;
                    break;
                }
            }
            if (equal)
                return;
        }

        TopSymbols = list;
    }

    private bool UpdateBarometerChart()
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null)
            return false;

        string symbolName = Constants.SymbolNameBarometerPrice + QuoteName;
        if (!exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            return false;

        // Use the interval selected in the dropdown (was hardcoded on "1m", which made the
        // interval selection a no-op compared to Avalonia).
        if (!GlobalData.IntervalListPeriodName.TryGetValue(IntervalName, out CryptoInterval? interval))
            return false;

        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        int maxPoints = Constants.BarometerGraphHours * 60;

        var points = new List<BarometerPoint>();
        var candles = symbolInterval.CandleList.GetLastNValues(maxPoints, 1);
        foreach (var candle in candles)
        {
            points.Add(new BarometerPoint
            {
                Time = candle.OpenTime.ToDateTime(),
                Value = candle.Close,
            });
        }

        BarometerChartPoints = points;
        return true;
    }

    private bool UpdateBarometerTime()
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null)
            return false;

        string symbolName = Constants.SymbolNameBarometerPrice + QuoteName;
        if (!exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            return false;
        if (!GlobalData.IntervalListPeriodName.TryGetValue(IntervalName, out CryptoInterval? interval))
            return false;

        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        try
        {
            if (symbolInterval.CandleList.Count > 0)
            {
                CryptoCandle candle = symbolInterval.CandleList.Values.Last();
                string text = (candle.OpenTime + 1).ToDateTime().ToLocalTime().ToString("HH:mm");
                if (text != BarometerTime)
                {
                    BarometerTime = text;
                    return true;
                }
            }
        }
        catch (InvalidOperationException)
        {
        }
        return false;
    }

    public void Dispose()
    {
        _disposed = true;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        GC.SuppressFinalize(this);
    }
}

public class DashboardSymbolInfo
{
    public string BaseName { get; init; } = "";
    public CryptoSymbol? Symbol { get; set; }

    /// <summary>
    /// What the row shows. Avalonia binds the symbol NAME here, so it reads "BTCUSDT" and not just
    /// "BTC" — which also makes the fallback to the USDT pair visible when the coin is not listed
    /// against the selected quote.
    /// </summary>
    public string DisplayName => Symbol?.Name ?? BaseName;

    /// <summary>
    /// False while no price has been seen at all. The dashboard shows a limited number of rows, so
    /// a coin the exchange lists but never quotes (too little volume to be monitored) is left out
    /// and the next coin from the list takes its place instead of wasting a slot.
    /// </summary>
    public bool HasPrice { get; set; }

    public string PriceText { get; set; } = "-";
    public string VolumeText { get; set; } = "-";
    public string ColorClass { get; set; } = "";
    public decimal LastKnownPrice { get; set; }
}

public class BarometerPoint
{
    public DateTime Time { get; set; }
    public decimal Value { get; set; }
}

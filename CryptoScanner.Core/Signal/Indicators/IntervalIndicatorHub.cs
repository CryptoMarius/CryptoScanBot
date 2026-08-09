using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanner.Core.Signal.Indicators;

/// <summary>
/// Incremental indicator state for ONE symbol+interval, built on Skender v3 hubs through an
/// <see cref="IndicatorRegistry"/>: feed each candle once via <see cref="Add"/> and read the latest
/// values via <see cref="BuildCurrent"/>. One instance lives on CryptoSymbolInterval and is fed in
/// ascending candle-open-time order; CryptoSymbolInterval.IndicatorHubLastAdded tracks the last
/// candle (null = warm-up still needed).
///
/// What gets built is a decision of the consumers, not of this class:
///  • a BASE SET that virtually every strategy reads (Bollinger/Sma20, Sma50/100/200, Rsi, Macd,
///    Stoch, PSar) plus the incremental Lux Multi-RSI — always present;
///  • whatever REGISTERED plugins declare through <see cref="IStrategyPlugin.RequiredIndicators"/>
///    (Ema, Wma, Atr, SuperTrend). Nobody declaring them means they are never computed.
///
/// That replaces the old <c>#if DEBUG</c> blocks, which computed Ema50/Wma/Atr14/SuperTrend only in
/// a Debug build because the only strategies reading them (Bbma, SuperTrendBreakout) happened to be
/// registered only in Debug — a coupling spread over two files that nothing enforced.
/// </summary>
public sealed class IntervalIndicatorHub
{
    private readonly IndicatorRegistry _registry;

    private readonly BollingerBandsHub _bb;   // also the source of Sma20 (= BB basis)
    private readonly SmaHub _sma50;
    private readonly SmaHub _sma100;
    private readonly SmaHub _sma200;
    private readonly RsiHub _rsi;
    private readonly MacdHub _macd;
    private readonly StochHub _stoch;
    private readonly ParabolicSarHub _psar;

    // Optional: present only when a registered plugin declared them.
    private readonly EmaHub? _ema50;
    private readonly AtrHub? _atr14;
    private readonly WmaHub? _wma05Low;
    private readonly WmaHub? _wma05High;
    private readonly WmaHub? _wma10Low;
    private readonly WmaHub? _wma10High;
    private readonly SuperTrendHub? _superTrend;

    // Lux Multi-RSI incremental state (mirrors LuxIndicator.CalculateNew)
    private const int LuxMin = 10;
    private const int LuxMax = 20;
    private const int LuxN = LuxMax - LuxMin + 1;
    private readonly double[] _luxNum = new double[LuxN];
    private readonly double[] _luxDen = new double[LuxN];
    private double _luxPrevClose;
    private bool _luxHasPrev;
    private int _luxOversold;
    private int _luxOverbought;

    private readonly List<IIndicatorExtension> _pluginExtensions = [];

    // SMA(200) is the longest lookback; 300 gives comfortable headroom.
    // Keeps Skender's internal cache small so pruning stays O(300) instead of O(100k).
    private const int HubCacheSize = 300;

    // The CryptoData fields BuildCurrent knows how to fill from a declared indicator. Anything a
    // plugin declares outside this list is still built and shared through the registry, it just has
    // no dedicated CryptoData field — the plugin reads it through its own IIndicatorExtension.
    private static readonly IndicatorKey KeyEma50 = IndicatorKey.Ema(50);
    private static readonly IndicatorKey KeyAtr14 = IndicatorKey.Atr(14);
    private static readonly IndicatorKey KeyWma05Low = IndicatorKey.WmaLow(5);
    private static readonly IndicatorKey KeyWma05High = IndicatorKey.WmaHigh(5);
    private static readonly IndicatorKey KeyWma10Low = IndicatorKey.WmaLow(10);
    private static readonly IndicatorKey KeyWma10High = IndicatorKey.WmaHigh(10);
    private static readonly IndicatorKey KeySuperTrend = IndicatorKey.SuperTrend(10, 3.0);

    /// <summary>
    /// The settings generation this hub was built under. A hub is fed incrementally and never
    /// reconfigured, so once the settings change it has to be thrown away and rebuilt — see
    /// <see cref="IndicatorConfiguration"/>.
    /// </summary>
    public int ConfigVersion { get; }

    public IntervalIndicatorHub()
    {
        var settings = GlobalData.Settings.General;
        ConfigVersion = IndicatorConfiguration.Version;
        _registry = new IndicatorRegistry(HubCacheSize);

        // Base set — parameters identical to what the batch path used to compute.
        _bb = _registry.BollingerBands(settings.SettingsBb.Length, settings.SettingsBb.Deviation);
        _sma50 = _registry.Sma(50);
        _sma100 = _registry.Sma(100);
        _sma200 = _registry.Sma(200);
        _rsi = _registry.Rsi(settings.SettingsRsi.Length);
        _macd = _registry.Macd(12, 26, 9);
        _stoch = _registry.Stoch(settings.SettingsStoch.Length, settings.SettingsStoch.SmoothingD, settings.SettingsStoch.SmoothingK);
        _psar = _registry.ParabolicSar(0.02, 0.2);

        // .Distinct(): LoadedPlugins maps each strategy enum to its plugin, so a plugin with N
        // strategies appears N times in .Values.
        var plugins = PluginManager.LoadedPlugins.Values.Distinct().ToList();

        // Declared indicators are built for every REGISTERED plugin, not only for enabled ones.
        // Registration is the build-time decision "does this strategy exist at all"; enabling is a
        // runtime setting the user flips. Tying cheap indicators to that setting is what made a
        // strategy silently stop signalling when it was not ticked in a test or a fresh profile.
        foreach (var plugin in plugins)
        {
            foreach (IndicatorKey key in plugin.RequiredIndicators)
                _registry.GetOrAdd(key);
        }

        _ema50 = _registry.Find<EmaHub>(KeyEma50);
        _atr14 = _registry.Find<AtrHub>(KeyAtr14);
        _wma05Low = _registry.Find<WmaHub>(KeyWma05Low);
        _wma05High = _registry.Find<WmaHub>(KeyWma05High);
        _wma10Low = _registry.Find<WmaHub>(KeyWma10Low);
        _wma10High = _registry.Find<WmaHub>(KeyWma10High);
        _superTrend = _registry.Find<SuperTrendHub>(KeySuperTrend);

        // The heavy plugin kernels (NWE ~99k FLOPs per candle, VBS its VWMA pair) stay gated on an
        // enabled strategy — running them for a disabled plugin is pure waste.
        foreach (var plugin in plugins)
        {
            bool anyEnabled = plugin.Strategies.Any(s =>
                GlobalData.Settings.Signal.Long.Strategy.Contains(s.Name) ||
                GlobalData.Settings.Signal.Short.Strategy.Contains(s.Name));
            if (!anyEnabled)
                continue;

            var ext = plugin.CreateIndicatorExtension();
            if (ext != null)
            {
                ext.Init(_registry);
                _pluginExtensions.Add(ext);
            }
        }
    }

    /// <summary>The indicators actually built for this symbol+interval (diagnostics and tests).</summary>
    public IReadOnlyCollection<IndicatorKey> BuiltIndicators => _registry.Keys;

    /// <summary>Feeds one candle and advances every indicator. Call in ascending candle-open-time order.
    /// Accepts IQuote so the warm-up can feed the boxed CollectCandles window directly.</summary>
    public void Add(IQuote candle)
    {
        _registry.QuoteHub.Add(new Quote(candle.Timestamp, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume));

        // Incremental Lux Multi-RSI: one RMA step per candle instead of replaying 100 candles.
        double close = (double)candle.Close;
        if (_luxHasPrev)
        {
            double diff = close - _luxPrevClose;
            int overbuy = 0, oversell = 0;
            for (int i = 0; i < LuxN; i++)
            {
                double alpha = 1.0 / (LuxMin + i);
                _luxNum[i] = alpha * diff + (1.0 - alpha) * _luxNum[i];
                _luxDen[i] = alpha * Math.Abs(diff) + (1.0 - alpha) * _luxDen[i];
                double rsi = _luxDen[i] == 0.0 ? 50.0 : 50.0 * _luxNum[i] / _luxDen[i] + 50.0;
                if (rsi > 70) overbuy++;
                if (rsi < 30) oversell++;
            }
            _luxOversold = (int)(100.0 * oversell / LuxN);
            _luxOverbought = (int)(100.0 * overbuy / LuxN);
        }
        _luxPrevClose = close;
        _luxHasPrev = true;

        foreach (var ext in _pluginExtensions)
            ext.OnCandleAdded(candle);
    }

    /// <summary>
    /// Reads the latest value of every built indicator into a fresh <see cref="CryptoData"/>.
    /// Fields whose indicator was never requested stay null.
    /// </summary>
    public CryptoData BuildCurrent()
    {
        var data = new CryptoData();

        var bb = _bb.Results;
        if (bb.Count > 0)
        {
            var r = bb[^1];
            data.Sma20 = r.Sma;
            data.BollingerBandsDeviation = 0.5 * (r.UpperBand - r.LowerBand);
            data.BollingerBandsPercentage = 100 * (r.UpperBand / r.LowerBand - 1);
        }

        if (_sma50.Results.Count > 0)
            data.Sma50 = _sma50.Results[^1].Sma;
        if (_sma100.Results.Count > 0)
            data.Sma100 = _sma100.Results[^1].Sma;
        if (_sma200.Results.Count > 0)
            data.Sma200 = _sma200.Results[^1].Sma;
        if (_rsi.Results.Count > 0)
            data.Rsi = _rsi.Results[^1].Rsi;

        if (_macd.Results.Count > 0)
        {
            var r = _macd.Results[^1];
            data.MacdValue = r.Macd;
            data.MacdSignal = r.Signal;
            data.MacdHistogram = r.Histogram;
        }

        if (_stoch.Results.Count > 0)
        {
            var r = _stoch.Results[^1];
            data.StochOscillator = r.Oscillator;
            data.StochSignal = r.Signal;
        }

        if (_psar.Results.Count > 0 && _psar.Results[^1].Sar != null)
            data.PSar = _psar.Results[^1].Sar;

        // Lux Multi-RSI
        int luxValue = 0;
        if (_luxOverbought > 0) luxValue += _luxOverbought;
        if (_luxOversold > 0) luxValue -= _luxOversold;
        data.Lux5mValue = (short)luxValue;

        // Declared by plugins; stay null when nobody asked for them.
        if (_ema50 != null && _ema50.Results.Count > 0)
            data.Ema50 = _ema50.Results[^1].Ema;
        if (_atr14 != null && _atr14.Results.Count > 0)
            data.Atr14 = _atr14.Results[^1].Atr;
        if (_wma05Low != null && _wma05Low.Results.Count > 0)
            data.Wma05Low = _wma05Low.Results[^1].Wma;
        if (_wma05High != null && _wma05High.Results.Count > 0)
            data.Wma05High = _wma05High.Results[^1].Wma;
        if (_wma10Low != null && _wma10Low.Results.Count > 0)
            data.Wma10Low = _wma10Low.Results[^1].Wma;
        if (_wma10High != null && _wma10High.Results.Count > 0)
            data.Wma10High = _wma10High.Results[^1].Wma;
        if (_superTrend != null && _superTrend.Results.Count > 0)
        {
            var st = _superTrend.Results[^1];
            data.SuperTrend = (double?)st.SuperTrend;
            data.SuperTrendUpperBand = (double?)st.UpperBand;
            data.SuperTrendLowerBand = (double?)st.LowerBand;
        }

        foreach (var ext in _pluginExtensions)
            ext.FillData(data);

        return data;
    }
}

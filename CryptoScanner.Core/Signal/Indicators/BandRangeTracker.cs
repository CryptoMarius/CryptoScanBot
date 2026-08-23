using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanner.Core.Signal.Indicators;

/// <summary>
/// Measures whether a symbol has enough room between its Bollinger bands to be worth trading, and
/// how the price behaves after it touches one. Lives per symbol+interval next to
/// <see cref="IntervalIndicatorHub"/> and is fed the same candles.
///
/// <para>Two numbers are collected:</para>
/// <list type="number">
///   <item><b>Band width</b> — 100 * (upper / lower - 1) per candle, kept as a median over the last
///   <see cref="WidthWindow"/> candles. That is the same definition as the stored
///   <see cref="CryptoData.BollingerBandsPercentage"/>, on purpose: one width definition in the
///   scanner. Bollinger's own BandWidth divides by the middle line instead, which runs 0 to 7.5%
///   lower, but the two are a strictly increasing function of each other so they rank symbols
///   identically — measured over 48 symbols the index ranking shifted by at most one place.</item>
///   <item><b>Excursion ratio</b> — after every band touch the measurement follows the price for at
///   most <see cref="MaximumHold"/> candles and records how far it moved in the intended direction
///   (favourable) versus how far it first moved against (adverse). The ratio of the two averages
///   says how much risk has to be carried per unit of reward.</item>
/// </list>
///
/// <para>
/// <see cref="Index"/> multiplies the two. Measured over 48 Binance Futures symbols on 1h candles it
/// is the best single predictor of what a naive mean-reversion entry actually returns; the width
/// alone and the ratio alone are both weaker. Above 3 is where it starts to pay off.
/// </para>
///
/// <para>
/// The windows are deliberately SHORT. A longer look-back gives a steadier number but a worse
/// prediction: the regime moves, so the last few hundred candles describe the symbol of today while
/// a three-month average describes a symbol that no longer exists. Measured on 1h: a 500-candle
/// look-back correlates +0.19 with the next 500 candles, 1000 candles +0.14, 2000 candles +0.06.
/// </para>
///
/// <para>
/// The measurement looks forward — an excursion is only complete once the middle line is reached or
/// the hold expires. That makes this a filter for deciding WHETHER to trade a symbol, never a
/// condition inside the candle you enter on.
/// </para>
///
/// <para>Not thread-safe: one tracker belongs to one CryptoSymbolInterval, exactly like the hub.</para>
/// </summary>
public sealed class BandRangeTracker
{
    /// <summary>Candles kept for the median band width.</summary>
    public const int WidthWindow = 500;

    /// <summary>Completed excursions kept. 25 is roughly what 500 candles produce (~2.7 band
    /// crossings per 100 candles, both sides together).</summary>
    public const int ExcursionWindow = 25;

    /// <summary>Candles one excursion may run before it is closed at whatever it reached by then.</summary>
    public const int MaximumHold = 48;

    /// <summary>Below this many completed excursions the ratio is noise and <see cref="Index"/>
    /// stays null rather than showing a misleading number.</summary>
    public const int MinimumMeasurements = 10;

    /// <summary>Candles below which the median width is not representative either.</summary>
    private const int MinimumWidths = 100;

    /// <summary>Candles replayed when the tracker is built. More than <see cref="WidthWindow"/>,
    /// because the excursions need room to fill their own window as well.</summary>
    public const int BuildWindow = 750;

    /// <summary>Skender cache size for the private Bollinger hub used while building — only the
    /// latest result is ever read, so this only has to clear the band length itself.</summary>
    private const int BuildCacheSize = 100;

    // Band widths, ring buffer.
    private readonly double[] _widths = new double[WidthWindow];
    private int _widthPosition;
    private int _widthCount;

    // Completed excursions, ring buffers. Favourable = movement in the intended direction,
    // adverse = movement against it; both stored as a positive percentage.
    private readonly double[] _favourable = new double[ExcursionWindow];
    private readonly double[] _adverse = new double[ExcursionWindow];
    private int _excursionPosition;
    private int _excursionCount;

    // A measurement that is still running. At most one per side, so measurements never overlap —
    // the same rule the offline calculation used.
    private struct Excursion
    {
        public double Entry;
        public int BarsLeft;
        public double Best;    // percentage, signed in the intended direction
        public double Worst;   // percentage, signed against the intended direction
    }

    private Excursion? _openLong;
    private Excursion? _openShort;

    // Cached median; recomputed only when new candles came in.
    private double? _medianWidth;
    private bool _medianDirty = true;

    /// <summary>The last candle fed, so the caller can spot a gap.</summary>
    public CandleTime? LastAdded { get; private set; }

    /// <summary>Candles fed so far.</summary>
    public int AddCount { get; private set; }

    /// <summary>Completed excursions in the current window. Below
    /// <see cref="MinimumMeasurements"/> the index is not shown.</summary>
    public int MeasurementCount => _excursionCount;

    /// <summary>
    /// Feeds one candle plus the Bollinger bands belonging to it. Call in ascending candle-open-time
    /// order, after the bands for that candle have been calculated.
    /// </summary>
    public void Add(in CryptoCandle candle, double middle, double upper, double lower)
    {
        if (middle <= 0 || upper <= lower || lower <= 0)
            return;

        AddWidth(100.0 * (upper / lower - 1.0));
        LastAdded = candle.OpenTime;
        AddCount++;

        double high = (double)candle.High;
        double low = (double)candle.Low;
        double close = (double)candle.Close;

        // Advance the running measurements FIRST. A measurement started on candle i is only updated
        // from candle i+1 onwards, and the candle that closes it still counts towards its high/low.
        bool closedLong = false;
        bool closedShort = false;

        if (_openLong.HasValue)
        {
            Excursion excursion = _openLong.Value;
            excursion.Best = Math.Max(excursion.Best, 100.0 * (high / excursion.Entry - 1.0));
            excursion.Worst = Math.Min(excursion.Worst, 100.0 * (low / excursion.Entry - 1.0));
            excursion.BarsLeft--;
            if (close >= middle || excursion.BarsLeft <= 0)
            {
                CloseExcursion(excursion);
                _openLong = null;
                closedLong = true;
            }
            else
                _openLong = excursion;
        }

        if (_openShort.HasValue)
        {
            Excursion excursion = _openShort.Value;
            excursion.Best = Math.Min(excursion.Best, 100.0 * (low / excursion.Entry - 1.0));
            excursion.Worst = Math.Max(excursion.Worst, 100.0 * (high / excursion.Entry - 1.0));
            excursion.BarsLeft--;
            if (close <= middle || excursion.BarsLeft <= 0)
            {
                CloseExcursion(excursion);
                _openShort = null;
                closedShort = true;
            }
            else
                _openShort = excursion;
        }

        // Then look for a new touch. Not on a candle that just closed a measurement on that side —
        // the offline calculation resumes at the candle after the close.
        if (_openLong == null && !closedLong && close <= lower)
            _openLong = new Excursion { Entry = close, BarsLeft = MaximumHold };

        if (_openShort == null && !closedShort && close >= upper)
            _openShort = new Excursion { Entry = close, BarsLeft = MaximumHold };
    }

    private void AddWidth(double width)
    {
        _widths[_widthPosition] = width;
        _widthPosition = (_widthPosition + 1) % WidthWindow;
        if (_widthCount < WidthWindow)
            _widthCount++;
        _medianDirty = true;
    }

    private void CloseExcursion(in Excursion excursion)
    {
        _favourable[_excursionPosition] = Math.Abs(excursion.Best);
        _adverse[_excursionPosition] = Math.Abs(excursion.Worst);
        _excursionPosition = (_excursionPosition + 1) % ExcursionWindow;
        if (_excursionCount < ExcursionWindow)
            _excursionCount++;
    }

    /// <summary>Median band width as a percentage, same definition as
    /// <see cref="CryptoData.BollingerBandsPercentage"/>, or null while warming up.</summary>
    public double? MedianWidth
    {
        get
        {
            if (_widthCount < MinimumWidths)
                return null;
            if (_medianDirty)
            {
                double[] copy = new double[_widthCount];
                Array.Copy(_widths, copy, _widthCount);
                Array.Sort(copy);
                _medianWidth = copy.Length % 2 == 1
                    ? copy[copy.Length / 2]
                    : 0.5 * (copy[copy.Length / 2 - 1] + copy[copy.Length / 2]);
                _medianDirty = false;
            }
            return _medianWidth;
        }
    }

    /// <summary>
    /// Average favourable movement divided by average adverse movement over the completed
    /// excursions, or null while there are too few. Below 1 means more risk is carried than reward
    /// is offered; in practice the values run from roughly 0.4 to 0.7.
    /// </summary>
    public double? Ratio
    {
        get
        {
            if (_excursionCount < MinimumMeasurements)
                return null;

            double sumFavourable = 0, sumAdverse = 0;
            for (int i = 0; i < _excursionCount; i++)
            {
                sumFavourable += _favourable[i];
                sumAdverse += _adverse[i];
            }
            if (sumAdverse <= 0)
                return null;
            return sumFavourable / sumAdverse;
        }
    }

    /// <summary>
    /// Median band width times the excursion ratio, or null while there is not enough to say. Use it
    /// as a coarse sort — under 2, between 2 and 3, over 3 — not as a precise number: the difference
    /// between 2.6 and 2.9 is inside the noise.
    /// </summary>
    public double? Index
    {
        get
        {
            double? width = MedianWidth;
            double? ratio = Ratio;
            if (width == null || ratio == null)
                return null;
            return width.Value * ratio.Value;
        }
    }

    /// <summary>
    /// Builds a tracker from the candles already in memory. Uses its own Bollinger hub with the
    /// same settings as <see cref="IntervalIndicatorHub"/>, so the bands are identical to what the
    /// rest of the scanner sees; feeding the tracker from the hub warm-up itself is not an option
    /// because that window is only 260 candles.
    /// <para>
    /// Gaps in the candle list are skipped rather than filled with flat candles: a flat candle would
    /// shrink the deviation and register as neither a touch nor a movement.
    /// </para>
    /// </summary>
    public static BandRangeTracker Build(CryptoSymbolInterval symbolInterval, CandleTime lastOpenTime)
    {
        BandRangeTracker tracker = new();

        var settings = GlobalData.Settings.General.SettingsBb;
        IndicatorRegistry registry = new(BuildCacheSize);
        BollingerBandsHub bands = registry.BollingerBands(settings.Length, settings.Deviation);

        foreach (CryptoCandle candle in CollectBuildCandles(symbolInterval, lastOpenTime))
        {
            registry.QuoteHub.Add(new Quote(candle.Timestamp, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume));

            var results = bands.Results;
            if (results.Count == 0)
                continue;

            var last = results[^1];
            if (last.Sma == null || last.UpperBand == null || last.LowerBand == null)
                continue;

            tracker.Add(candle, last.Sma.Value, last.UpperBand.Value, last.LowerBand.Value);
        }

        return tracker;
    }

    /// <summary>The last <see cref="BuildWindow"/> candles up to and including
    /// <paramref name="lastOpenTime"/>, oldest first.</summary>
    private static List<CryptoCandle> CollectBuildCandles(CryptoSymbolInterval symbolInterval, CandleTime lastOpenTime)
    {
        // This used to be a foreach over the list under lock (symbolInterval.CandleList). That lock
        // protected nothing: CryptoCandleList guards itself with a ReaderWriterLockSlim, so the
        // kline stream adds a candle without ever touching the monitor on the object, and the
        // enumerator then throws "Collection was modified after the enumerator was instantiated".
        // Same defect as the one that aborted BulkCalculateCandles on Okx Futures (20-08-2026).
        // GetLastValuesUpTo does the identical walk under the read lock.
        //
        // Which is where it went wrong: that walk starts at the OLDEST candle and copies everything
        // it passes, so asking for 750 cost whatever the store happened to hold. This runs once per
        // indicator warm-up, and a warm-up runs once per pipeline tick - see ZoneCandleWindows for
        // why those stores had grown to hundreds of thousands of candles. GetLastValues steps
        // backwards from lastOpenTime instead and gives the same answer for a fixed price.
        return symbolInterval.CandleList.GetLastValues(lastOpenTime, BuildWindow, symbolInterval.Interval.Duration);
    }
}

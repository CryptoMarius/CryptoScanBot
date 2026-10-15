using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Collections.Concurrent;

namespace CryptoScanner.Core.Barometer;

/// <summary>
/// One measured value of <see cref="MarketState"/>: the score and the parts it was made of, so a log
/// line or a screen can say why.
/// </summary>
/// <param name="Score">The market state, -100 (everything down) .. +100 (everything up).</param>
/// <param name="BtcPart">The BTC part of the score, -100..+100.</param>
/// <param name="Breadth">Share of the coins whose last 1h close is above their own 1h EMA50, 0..100.</param>
/// <param name="Market72h">Average % change of the coins over the last 72 hours.</param>
/// <param name="SymbolCount">How many coins the breadth and the 72h change were measured over.</param>
public sealed record MarketStateValue(double Score, double BtcPart, double Breadth, double Market72h, int SymbolCount);


/// <summary>
/// The state of the whole market of a quote as one number from -100 to +100, so its course can be
/// followed rather than switched on and off. Measured on Session2 (13/14-10-2026, study
/// market-regime): the long profit of dbr, failedbreakout, candlepattern and mac came from entries
/// at a score of +60 and higher, while longs between 0 and +60 lost money in three of the four.
/// <para>
/// The score is the average of three parts, each clamped to -100..+100:
/// - BTC on the chosen interval (4h by default): its distance to the EMA200 (5% counts as full) and
///   the slope of the EMA50 over the last 24 hours (1.5% counts as full), averaged;
/// - the breadth: the share of the coins above their own 1h EMA50, (share - 50) x 2;
/// - the market over 72 hours: the average change of all coins, 6% counts as full.
/// Mixing in a second, faster BTC interval was measured as well (4h twice and 1h once): slightly
/// more long profit on paper, slightly worse at telling where the market goes. Not worth it.
/// </para>
/// <para>
/// Which candles count: the 1h candles that closed at or before T-1, the same "previous minute"
/// convention as <see cref="MarketRsi"/> (the 1h candle that closes at T is not reliably there for
/// every coin yet). The value is calculated once per whole hour per quote and interval and cached;
/// a coin whose newest hour had not arrived yet makes the next minute try again.
/// </para>
/// </summary>
public static class MarketState
{
    public const int EmaLong = 200;
    public const int EmaShort = 50;
    public const int BreadthEma = 50;
    public const int MarketHours = 72;

    // What counts as a full part (100): see the class comment
    public const double BtcDistanceFull = 5.0;
    public const double BtcSlopeFull = 1.5;
    public const double Market72hFull = 6.0;

    /// <summary>Fewer coins than this and there is no market state, the same floor the barometer uses.</summary>
    public const int MinimumSymbols = 5;

    /// <summary>
    /// The 1h closes read per coin: enough for the EMA50 of the breadth to have settled, and for the
    /// close of 72 hours ago.
    /// </summary>
    public const int HistoryHours = 200;

    /// <summary>
    /// The BTC candles read for the trend: the EMA200 needs 200 to start and settles with every
    /// candle after that, so take what the scanner keeps (500 per interval).
    /// </summary>
    public const int BtcHistoryCandles = 500;

    private sealed class QuoteCache
    {
        public readonly object Gate = new();
        public CandleTime HourOpen = CandleTime.MinValue;
        public CandleTime BuiltAtMinute = CandleTime.MinValue;
        public bool Incomplete;
        public MarketStateValue? Value;
    }

    private static readonly ConcurrentDictionary<(string Quote, CryptoIntervalPeriod Interval), QuoteCache> Caches = new();


    /// <summary>
    /// Forget every cached value. The emulator calls this at the start of each run: two runs can
    /// replay the same hours with a different set of coins.
    /// </summary>
    public static void Reset() => Caches.Clear();


    /// <summary>
    /// The market state for a signal or entry at <paramref name="momentTime"/>. Null when there are
    /// fewer than <see cref="MinimumSymbols"/> coins with enough candles, or no BTC trend.
    /// </summary>
    public static MarketStateValue? Calculate(CryptoQuoteData quoteData, CandleTime momentTime, CryptoIntervalPeriod btcInterval)
    {
        CandleTime priceTime = momentTime - 1u;
        CandleTime hourStart = priceTime.AlignToIntervalMinutes(60);
        CandleTime hourOpen = hourStart - 60u;

        QuoteCache cache = Caches.GetOrAdd((quoteData.Name, btcInterval), _ => new QuoteCache());
        lock (cache.Gate)
        {
            if (cache.HourOpen == hourOpen && (!cache.Incomplete || cache.BuiltAtMinute == priceTime))
                return cache.Value;

            double? btcPart = CalculateBtcPart(quoteData, hourStart, btcInterval);
            (double breadth, double market72h, int count, bool incomplete) = CalculateCoins(quoteData, hourOpen);

            cache.HourOpen = hourOpen;
            cache.BuiltAtMinute = priceTime;
            cache.Incomplete = incomplete;
            cache.Value = count >= MinimumSymbols && btcPart.HasValue
                ? new MarketStateValue(Combine(btcPart.Value, breadth, market72h), btcPart.Value, breadth, market72h, count)
                : null;
            return cache.Value;
        }
    }


    /// <summary>The score from its three parts (see the class comment).</summary>
    public static double Combine(double btcPart, double breadth, double market72h)
    {
        double breadthPart = Clamp((breadth - 50.0) * 2.0);
        double marketPart = Clamp(market72h / Market72hFull * 100.0);
        return (Clamp(btcPart) + breadthPart + marketPart) / 3.0;
    }


    private static double Clamp(double value) => Math.Clamp(value, -100.0, 100.0);


    /// <summary>
    /// The BTC part from the candles of <paramref name="interval"/> that closed at or before
    /// <paramref name="hourStart"/>. Null when BTC is not there or has too few candles for the EMA200.
    /// </summary>
    private static double? CalculateBtcPart(CryptoQuoteData quoteData, CandleTime hourStart, CryptoIntervalPeriod interval)
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null || !exchange.TryGetSymbolByPair("BTC" + quoteData.Name, out CryptoSymbol? btc))
            return null;

        CryptoSymbolInterval symbolInterval = btc.GetSymbolInterval(interval);
        uint duration = symbolInterval.Interval.Duration;
        // The newest candle of this interval that had closed at hourStart
        CandleTime lastOpen = hourStart.AlignToIntervalMinutes(duration) - duration;
        List<double> closes = symbolInterval.CandleList.GetLastValues(lastOpen, BtcHistoryCandles, duration)
            .Select(c => (double)c.Close).ToList();

        // The slope of the EMA50 over the last 24 hours, in candles of this interval
        int slopeCandles = Math.Max(1, (int)(24 * 60 / duration));
        if (closes.Count < EmaLong + slopeCandles)
            return null;

        double[] emaLong = Ema(closes, EmaLong);
        double[] emaShort = Ema(closes, EmaShort);
        int last = closes.Count - 1;
        double distance = (closes[last] / emaLong[last] - 1.0) * 100.0;
        double slope = (emaShort[last] / emaShort[last - slopeCandles] - 1.0) * 100.0;
        return Clamp((distance / BtcDistanceFull + slope / BtcSlopeFull) / 2.0 * 100.0);
    }


    /// <summary>
    /// Breadth and the 72h change over every usable coin, from the 1h candles up to and including
    /// the one that opened at <paramref name="lastHourOpen"/>. Same coin selection as the barometer.
    /// </summary>
    private static (double Breadth, double Market72h, int Count, bool Incomplete) CalculateCoins(CryptoQuoteData quoteData, CandleTime lastHourOpen)
    {
        int above = 0, count = 0, changeCount = 0;
        double changeSum = 0;
        bool incomplete = false;
        List<CryptoSymbol> symbolList = quoteData.SymbolList;
        List<double> closes = new(HistoryHours);
        for (int i = 0; i < symbolList.Count; i++)
        {
            CryptoSymbol symbol = symbolList[i];
            if (symbol.QuoteData == null || !symbol.QuoteData.FetchCandles || symbol.IsBarometerSymbol() || !symbol.EnoughVolume())
                continue;

            CryptoCandleList candles = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1h).CandleList;
            if (!candles.TryGetValue(lastHourOpen, out CryptoCandle lastCandle))
            {
                // Live a candle can arrive late: worth another try next minute
                if (candles.Count != 0)
                    incomplete = true;
                continue;
            }

            closes.Clear();
            foreach (CryptoCandle candle in candles.GetLastValues(lastHourOpen, HistoryHours, 60))
                closes.Add((double)candle.Close);
            if (closes.Count < BreadthEma)
                continue;

            double[] ema = Ema(closes, BreadthEma);
            count++;
            if ((double)lastCandle.Close > ema[^1])
                above++;

            if (candles.TryGetValue(lastHourOpen - (uint)(60 * MarketHours), out CryptoCandle oldCandle) && oldCandle.Close > 0)
            {
                changeSum += ((double)lastCandle.Close / (double)oldCandle.Close - 1.0) * 100.0;
                changeCount++;
            }
        }
        double breadth = count == 0 ? 0 : 100.0 * above / count;
        double market72h = changeCount == 0 ? 0 : changeSum / changeCount;
        return (breadth, market72h, count, incomplete);
    }


    /// <summary>EMA seeded with the simple average of the first <paramref name="length"/> values.</summary>
    internal static double[] Ema(IReadOnlyList<double> values, int length)
    {
        double[] result = new double[values.Count];
        if (values.Count < length)
            return result;
        double sum = 0;
        for (int i = 0; i < length; i++)
            sum += values[i];
        double previous = sum / length;
        result[length - 1] = previous;
        double k = 2.0 / (length + 1.0);
        for (int i = length; i < values.Count; i++)
        {
            previous = (values[i] - previous) * k + previous;
            result[i] = previous;
        }
        return result;
    }
}

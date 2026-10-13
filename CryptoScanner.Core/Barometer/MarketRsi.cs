using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Collections.Concurrent;

namespace CryptoScanner.Core.Barometer;

/// <summary>
/// The market RSI: the standard RSI 14 (Wilder, as the charts and the indicators of the scanner
/// calculate it) of every coin of a quote on its own 1h candles, averaged over the coins. Unlike the
/// price barometer this is not a difference against an hour ago; each coin keeps its own RSI and only
/// the result is averaged.
/// <para>
/// Which candles count. A signal at time T is evaluated while the candles of the other coins for the
/// minute ending at T are still being processed (in parallel in the emulator, by separate streams
/// live), so the 1h candle that closes at T is not reliably there for every coin. The calculation
/// therefore takes the 1h candles that closed at or before T-1 and, as the last value, the close of
/// the 1m candle that ended at T-1 - the same "previous minute" the barometer reads. For a signal at
/// 16:00 that is the hours up to 15:00 plus the price of 15:59.
/// </para>
/// <para>
/// Cost. The RSI averages of the last 14 whole hours are computed once per hour per quote; every other
/// call in that hour only adds the last step from one 1m candle per coin, and a call in the same
/// minute returns the cached result. Nothing is computed unless a side has the filter switched on.
/// </para>
/// </summary>
public static class MarketRsi
{
    public const int RsiLength = 14;

    /// <summary>
    /// Whole-hour closes per coin: 15 closes give the 14 moves of the RSI 14. With exactly that
    /// history the RSI is the sum of the rises over the sum of all moves of those 14 hours; it can
    /// differ a few points from a chart that carries the running average over a longer history.
    /// </summary>
    public const int HistoryHours = RsiLength + 1;

    /// <summary>Fewer coins than this and there is no market RSI, the same floor the barometer uses.</summary>
    public const int MinimumSymbols = 5;

    private sealed class CoinState
    {
        public required CryptoSymbol Symbol;
        public double AverageGain;
        public double AverageLoss;
        public double LastClose;
    }

    private sealed class QuoteCache
    {
        public readonly object Gate = new();
        public CandleTime HourOpen = CandleTime.MinValue;
        public List<CoinState> Coins = [];
        // Coins whose newest whole hour had not come in yet when the hour was built (live, a candle
        // can arrive late). Tried again on every following minute until it is there.
        public List<CryptoSymbol> Waiting = [];
        public CandleTime PriceMinute = CandleTime.MinValue;
        public double? Value;
        public int SymbolCount;
    }

    private static readonly ConcurrentDictionary<string, QuoteCache> Caches = new();


    /// <summary>
    /// Forget every cached value. The emulator calls this at the start of each run: two runs can
    /// replay the same hours with a different set of coins.
    /// </summary>
    public static void Reset() => Caches.Clear();


    /// <summary>
    /// The market RSI for a signal or entry at <paramref name="momentTime"/> (the close of the signal
    /// candle, or the close of the last 1m candle in the position monitor). Null when fewer than
    /// <see cref="MinimumSymbols"/> coins have enough candles.
    /// </summary>
    public static double? Calculate(CryptoQuoteData quoteData, CandleTime momentTime, out int symbolCount)
    {
        // The price of the minute before: the 1m candle that opened at T-2 and closed at T-1
        CandleTime priceTime = momentTime - 1u;
        CandleTime priceMinute = momentTime - 2u;
        CandleTime hourStart = priceTime.AlignToIntervalMinutes(60);
        CandleTime hourOpen = hourStart - 60u;

        QuoteCache cache = Caches.GetOrAdd(quoteData.Name, _ => new QuoteCache());
        lock (cache.Gate)
        {
            if (cache.PriceMinute == priceMinute && cache.HourOpen == hourOpen)
            {
                symbolCount = cache.SymbolCount;
                return cache.Value;
            }

            if (cache.HourOpen != hourOpen)
            {
                (cache.Coins, cache.Waiting) = BuildHourStates(quoteData, hourOpen);
                cache.HourOpen = hourOpen;
            }
            else if (cache.Waiting.Count != 0)
            {
                double[] buffer = new double[HistoryHours];
                for (int i = cache.Waiting.Count - 1; i >= 0; i--)
                {
                    if (TryBuildState(cache.Waiting[i], hourOpen, buffer, out CoinState? state, out bool wait))
                        cache.Coins.Add(state!);
                    if (!wait)
                        cache.Waiting.RemoveAt(i);
                }
            }

            // The last step: the price at T-1 against the close of the last whole hour. When T-1 is
            // itself a whole hour there is no partial hour and the hourly value stands.
            bool partial = priceTime > hourStart;
            double sum = 0;
            int count = 0;
            foreach (CoinState coin in cache.Coins)
            {
                double gain = coin.AverageGain;
                double loss = coin.AverageLoss;
                if (partial)
                {
                    CryptoSymbolInterval interval1m = coin.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                    if (interval1m.CandleList.TryGetValue(priceMinute, out CryptoCandle candle))
                    {
                        double change = (double)candle.Close - coin.LastClose;
                        gain = (gain * (RsiLength - 1) + Math.Max(change, 0)) / RsiLength;
                        loss = (loss * (RsiLength - 1) + Math.Max(-change, 0)) / RsiLength;
                    }
                }
                sum += loss == 0 ? 100.0 : 100.0 - 100.0 / (1.0 + gain / loss);
                count++;
            }

            cache.PriceMinute = priceMinute;
            cache.SymbolCount = count;
            cache.Value = count >= MinimumSymbols ? sum / count : null;
            symbolCount = count;
            return cache.Value;
        }
    }


    /// <summary>
    /// The RSI averages of every usable coin up to and including the 1h candle that opened at
    /// <paramref name="lastHourOpen"/>, plus the coins that only miss that newest hour so far. Same
    /// coin selection as the price barometer.
    /// </summary>
    private static (List<CoinState> States, List<CryptoSymbol> Waiting) BuildHourStates(CryptoQuoteData quoteData, CandleTime lastHourOpen)
    {
        List<CoinState> states = [];
        List<CryptoSymbol> waiting = [];
        List<CryptoSymbol> symbolList = quoteData.SymbolList;
        double[] buffer = new double[HistoryHours];
        for (int i = 0; i < symbolList.Count; i++)
        {
            CryptoSymbol symbol = symbolList[i];
            if (symbol.QuoteData == null || !symbol.QuoteData.FetchCandles || symbol.IsBarometerSymbol() || !symbol.EnoughVolume())
                continue;

            if (TryBuildState(symbol, lastHourOpen, buffer, out CoinState? state, out bool wait))
                states.Add(state!);
            else if (wait)
                waiting.Add(symbol);
        }
        return (states, waiting);
    }


    /// <summary>
    /// The RSI averages of one coin. <paramref name="wait"/> is true when only the newest hour is
    /// missing: worth trying again a minute later. Too little history is final for this hour.
    /// </summary>
    private static bool TryBuildState(CryptoSymbol symbol, CandleTime lastHourOpen, double[] closes,
        out CoinState? state, out bool wait)
    {
        state = null;
        wait = false;
        CryptoCandleList candles = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1h).CandleList;
        int found = 0;
        CandleTime time = lastHourOpen - (uint)(60 * (HistoryHours - 1));
        for (int h = 0; h < HistoryHours; h++, time += 60u)
        {
            if (candles.TryGetValue(time, out CryptoCandle candle))
                closes[found++] = (double)candle.Close;
        }
        // The newest hour first: when only that one is missing, it is worth waiting a minute
        if (!candles.TryGetValue(lastHourOpen, out _))
        {
            wait = true;
            return false;
        }
        // And every hour before it
        if (found < HistoryHours)
            return false;

        // The plain average of the 14 moves (the start of the standard RSI); with a longer history
        // the running average would continue from here, each new move counting for 1/14
        double gain = 0, loss = 0;
        for (int k = 1; k <= RsiLength; k++)
        {
            double change = closes[k] - closes[k - 1];
            gain += Math.Max(change, 0);
            loss += Math.Max(-change, 0);
        }
        gain /= RsiLength;
        loss /= RsiLength;
        for (int k = RsiLength + 1; k < found; k++)
        {
            double change = closes[k] - closes[k - 1];
            gain = (gain * (RsiLength - 1) + Math.Max(change, 0)) / RsiLength;
            loss = (loss * (RsiLength - 1) + Math.Max(-change, 0)) / RsiLength;
        }

        state = new CoinState { Symbol = symbol, AverageGain = gain, AverageLoss = loss, LastClose = closes[found - 1] };
        return true;
    }
}

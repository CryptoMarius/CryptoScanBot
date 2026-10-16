using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Collections.Concurrent;

namespace CryptoScanner.Core.Barometer;

/// <summary>
/// How far the whole market of a quote moved between two moments: the average % change of every
/// coin from the last whole hour before the first moment to the last whole hour before the second.
/// Used by the market exit (close a position when the market turned against it since the entry).
/// <para>
/// Measured on Session2 (14-10-2026, study market-regime): positions during which the market moved
/// 3% against them lost money in every strategy (dbr long -8.778 over 876 positions, failedbreakout
/// long -14.114, candlepattern long -12.403), while all the other positions earned.
/// </para>
/// <para>
/// Whole hours, with the same "previous minute" convention as <see cref="MarketState"/>: the 1h
/// candle that closes at T is not reliably there for every coin yet. Same coin selection as the
/// barometer. The results are cached per quote for the current hour, so every open position that
/// asks within that hour shares the work.
/// </para>
/// </summary>
public static class MarketMove
{
    /// <summary>Fewer coins than this and there is no market move, the same floor the barometer uses.</summary>
    public const int MinimumSymbols = 5;

    private sealed class QuoteCache
    {
        public readonly object Gate = new();
        public CandleTime NowHourOpen = CandleTime.MinValue;
        public Dictionary<CandleTime, double?> SinceHour = [];
    }

    private static readonly ConcurrentDictionary<string, QuoteCache> Caches = new();


    /// <summary>
    /// Forget every cached value. The emulator calls this at the start of each run: two runs can
    /// replay the same hours with a different set of coins.
    /// </summary>
    public static void Reset() => Caches.Clear();


    /// <summary>The open time of the last whole hour that had closed at the minute before <paramref name="moment"/>.</summary>
    public static CandleTime LastHourOpen(CandleTime moment) => (moment - 1u).AlignToIntervalMinutes(60) - 60u;


    /// <summary>
    /// The average % change of the coins of the quote from <paramref name="fromMoment"/> to
    /// <paramref name="toMoment"/>, both taken as the last whole hour before them. Zero when both
    /// fall in the same hour; null when fewer than <see cref="MinimumSymbols"/> coins have both hours.
    /// </summary>
    public static double? Since(CryptoQuoteData quoteData, CandleTime fromMoment, CandleTime toMoment)
    {
        CandleTime fromHour = LastHourOpen(fromMoment);
        CandleTime nowHour = LastHourOpen(toMoment);
        if (fromHour >= nowHour)
            return 0;

        QuoteCache cache = Caches.GetOrAdd(quoteData.Name, _ => new QuoteCache());
        lock (cache.Gate)
        {
            if (cache.NowHourOpen != nowHour)
            {
                cache.NowHourOpen = nowHour;
                cache.SinceHour.Clear();
            }
            if (cache.SinceHour.TryGetValue(fromHour, out double? cached))
                return cached;

            double sum = 0;
            int count = 0;
            List<CryptoSymbol> symbolList = quoteData.SymbolList;
            for (int i = 0; i < symbolList.Count; i++)
            {
                CryptoSymbol symbol = symbolList[i];
                if (symbol.QuoteData == null || !symbol.QuoteData.FetchCandles || symbol.IsBarometerSymbol() || !symbol.EnoughVolume())
                    continue;

                CryptoCandleList candles = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1h).CandleList;
                if (candles.TryGetValue(fromHour, out CryptoCandle from) && from.Close > 0
                    && candles.TryGetValue(nowHour, out CryptoCandle now))
                {
                    sum += ((double)now.Close / (double)from.Close - 1.0) * 100.0;
                    count++;
                }
            }

            double? value = count >= MinimumSymbols ? sum / count : null;
            cache.SinceHour[fromHour] = value;
            return value;
        }
    }


    /// <summary>
    /// The % change of BTC of the quote between the same two whole hours as <see cref="Since"/>, for
    /// the market exit that watches BTC instead of all coins. Null when BTC or its candles are missing.
    /// </summary>
    public static double? BtcSince(CryptoQuoteData quoteData, CandleTime fromMoment, CandleTime toMoment)
    {
        CandleTime fromHour = LastHourOpen(fromMoment);
        CandleTime nowHour = LastHourOpen(toMoment);
        if (fromHour >= nowHour)
            return 0;
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null || !exchange.TryGetSymbolByPair("BTC" + quoteData.Name, out CryptoSymbol? btc))
            return null;
        CryptoCandleList candles = btc.GetSymbolInterval(CryptoIntervalPeriod.interval1h).CandleList;
        if (candles.TryGetValue(fromHour, out CryptoCandle from) && from.Close > 0 && candles.TryGetValue(nowHour, out CryptoCandle now))
            return ((double)now.Close / (double)from.Close - 1.0) * 100.0;
        return null;
    }


    /// <summary>
    /// The market move seen from one side: positive when the market went the way of the position,
    /// negative when it went against it.
    /// </summary>
    public static double ForSide(double marketMove, CryptoTradeSide side)
        => side == CryptoTradeSide.Long ? marketMove : -marketMove;
}

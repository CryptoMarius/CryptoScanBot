using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Collections.Concurrent;

namespace CryptoScanner.Analyzers.FvgLadder.Signal;

/// <summary>
/// Rule 1.1: a new position is only started while the last closed day candle closed above the
/// EMA200 of the day closes. (Rule 1.2, "the EMA200 at or above yesterday's", follows from 1.1 and
/// is left out: with the close above the EMA, the EMA can only have gone up.)
/// <para>
/// The EMA is kept per symbol and carried forward one day at a time, because the candle list only
/// holds a limited number of day candles (the emulator warms up about 270 and prunes back to that).
/// It starts on the first day candle that is there. In the first months of a run it can therefore differ from an EMA over the whole
/// history; the weight of the start falls under 3% after a year.
/// </para>
/// </summary>
public static class FvgLadderTrend
{
    public const int EmaLength = 200;
    private const uint Day = 1440;

    private sealed class State
    {
        public CandleTime LastDay;
        public decimal Ema;
        public decimal Close;
        // What the log was last told, so a flip of the trend is reported once and not every minute
        public bool? Reported;
    }

    private static readonly ConcurrentDictionary<CryptoSymbol, State> States = new();


    /// <summary>
    /// Whether the last day candle that closed at or before <paramref name="now"/> closed above the
    /// EMA200. False when there is no day candle yet.
    /// </summary>
    public static bool IsUp(CryptoSymbol symbol, CandleTime now, out decimal close, out decimal ema)
    {
        close = 0m;
        ema = 0m;
        CryptoCandleList candles = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1d).CandleList;

        // The open time of the last day candle that has closed by now
        CandleTime lastDay = new(now.Minutes / Day * Day - Day);
        if (!candles.TryGetValue(lastDay, out CryptoCandle lastCandle))
            return false;

        State? state = States.GetValueOrDefault(symbol);
        lock (symbol)
        {
            if (state == null || state.LastDay > lastDay || !CarryForward(candles, state, lastDay))
            {
                // Start again on the day candles that are there
                List<decimal> closes = [];
                CandleTime day = lastDay;
                while (candles.TryGetValue(day, out CryptoCandle candle))
                {
                    closes.Add(candle.Close);
                    if (day.Minutes < Day)
                        break;
                    day -= Day;
                }
                closes.Reverse();
                state = new State { LastDay = lastDay, Ema = FvgLadderRules.Ema(closes, EmaLength), Close = lastCandle.Close };
                States[symbol] = state;
            }

            close = state.Close;
            ema = state.Ema;

            // Without this a run in a falling market shows nothing at all for months, and that looks
            // exactly like a strategy that does not work
            bool up = close > ema;
            if (state.Reported != up)
            {
                state.Reported = up;
                GlobalData.AddTextToLogTab($"{symbol.Name} fvgladder trend {(up ? "UP" : "DOWN")}: day close {close} "
                    + $"{(up ? "above" : "not above")} EMA{EmaLength} {ema:N2}, new positions {(up ? "allowed" : "paused")}");
            }
        }
        return close > ema;
    }


    /// <summary>
    /// Move the EMA on to <paramref name="lastDay"/>, one day candle at a time. False when a day
    /// candle in between is missing, the caller then starts again.
    /// </summary>
    private static bool CarryForward(CryptoCandleList candles, State state, CandleTime lastDay)
    {
        if (state.LastDay == lastDay)
            return true;

        decimal ema = state.Ema;
        decimal close = state.Close;
        for (CandleTime day = state.LastDay + Day; day <= lastDay; day += Day)
        {
            if (!candles.TryGetValue(day, out CryptoCandle candle))
                return false;
            ema = FvgLadderRules.NextEma(ema, candle.Close, EmaLength);
            close = candle.Close;
        }
        state.LastDay = lastDay;
        state.Ema = ema;
        state.Close = close;
        return true;
    }


    /// <summary>Forget every symbol, so a next emulator run starts on its own candles.</summary>
    public static void Reset() => States.Clear();
}

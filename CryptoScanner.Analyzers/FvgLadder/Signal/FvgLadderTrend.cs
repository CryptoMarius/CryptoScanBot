using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Collections.Concurrent;

namespace CryptoScanner.Analyzers.FvgLadder.Signal;

/// <summary>
/// Rule 1: a new position is only started while the trend is up: the last closed day candle closed
/// above the EMA200 of the day closes. (Rule 1.2, "the EMA200 at or above yesterday's", follows from
/// that and is left out: with the close above the EMA, the EMA can only have gone up.) The Kalman
/// filter that could be chosen instead was removed on 05-11-2026.
/// <para>
/// The EMA is kept per symbol and carried forward one closed candle at a time, because the candle list
/// only holds a limited number of candles (the emulator warms up about 270 per interval and prunes
/// back to that). They start on the first candle that is there, so in the first part of a run they
/// can differ a little from a calculation over the whole history.
/// </para>
/// </summary>
public static class FvgLadderTrend
{
    public const int EmaLength = 200;

    private sealed class State
    {
        public uint Duration;
        public CandleTime LastCandle;
        public decimal Close;
        public decimal Ema;
        // What the log was last told, so a flip of the trend is reported once and not every minute
        public bool? Reported;
    }

    private static readonly ConcurrentDictionary<CryptoSymbol, State> States = new();


    /// <summary>
    /// Whether the trend is up at <paramref name="now"/>, measured on the last candle that closed at
    /// or before it. False when there is no such candle yet. <paramref name="text"/> says why, for the log.
    /// </summary>
    public static bool IsUp(CryptoSymbol symbol, CandleTime now, FvgLadderSettings settings, out string text)
    {
        CryptoInterval interval = TrendInterval();
        uint duration = interval.Duration;
        CryptoCandleList candles = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;

        // The open time of the last candle of the interval that has closed by now
        CandleTime last = new(now.Minutes / duration * duration - duration);
        if (!candles.TryGetValue(last, out CryptoCandle lastCandle))
        {
            text = $"no closed {interval.Name} candle for the trend yet";
            return false;
        }

        bool up;
        lock (symbol)
        {
            State? state = States.GetValueOrDefault(symbol);
            if (state == null || state.Duration != duration || state.LastCandle > last
                || !CarryForward(candles, state, last))
            {
                // Start again on the candles that are there, oldest first
                List<decimal> closes = [];
                CandleTime time = last;
                while (candles.TryGetValue(time, out CryptoCandle candle))
                {
                    closes.Add(candle.Close);
                    if (time.Minutes < duration)
                        break;
                    time -= duration;
                }
                closes.Reverse();
                state = new State
                {
                    Duration = duration,
                    LastCandle = last,
                    Close = lastCandle.Close,
                    Ema = FvgLadderRules.Ema(closes, EmaLength),
                    Reported = state?.Reported,
                };
                States[symbol] = state;
            }

            up = state.Close > state.Ema;
            text = $"day close {state.Close} {(up ? "above" : "not above")} EMA{EmaLength} {state.Ema:N2}";

            // Without this a run in a falling market shows nothing at all for months, and that looks
            // exactly like a strategy that does not work
            if (state.Reported != up)
            {
                state.Reported = up;
                GlobalData.AddTextToLogTab($"{symbol.Name} fvgladder trend {(up ? "UP" : "DOWN")}: {text}, "
                    + $"new positions {(up ? "allowed" : "paused")}");
            }
        }
        return up;
    }


    /// <summary>The interval the trend is measured on: 1d for the EMA.</summary>
    private static CryptoInterval TrendInterval()
    {
        string name = "1d";
        if (GlobalData.IntervalListPeriodName.TryGetValue(name, out CryptoInterval? interval))
            return interval;
        return GlobalData.IntervalListPeriodName["1h"];
    }


    /// <summary>
    /// Move the trend on to <paramref name="last"/>, one closed candle at a time. False when a candle
    /// in between is missing, the caller then starts again.
    /// </summary>
    private static bool CarryForward(CryptoCandleList candles, State state, CandleTime last)
    {
        if (state.LastCandle == last)
            return true;

        decimal ema = state.Ema;
        decimal close = state.Close;
        for (CandleTime time = state.LastCandle + state.Duration; time <= last; time += state.Duration)
        {
            if (!candles.TryGetValue(time, out CryptoCandle candle))
                return false;
            ema = FvgLadderRules.NextEma(ema, candle.Close, EmaLength);
            close = candle.Close;
        }
        state.LastCandle = last;
        state.Ema = ema;
        state.Close = close;
        return true;
    }


    /// <summary>Forget every symbol, so a next emulator run starts on its own candles.</summary>
    public static void Reset() => States.Clear();
}

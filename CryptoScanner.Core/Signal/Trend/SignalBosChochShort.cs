using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal.Trend;

/// <summary>
/// Fires a Short signal at the moment the pullback after a bearish CHoCH is broken downward
/// (= the actual step-in moment). A bearish CHoCH is a Lower Low that flips the BOS/CHoCH
/// trend from Bullish to Bearish.
///
/// Uses TrendBos which reacts faster than Dow Theory (single structural break is sufficient).
///
/// Flow (all four conditions must be true on the same bar for IsSignal to return true):
///   1. The last recorded structural event is a bearish CHoCH that has not fired yet.
///   2. The CHoCH is fresh enough (MaxEventAgeCandles from now).
///   3. A ZigZag High pivot has formed *after* the CHoCH candle — that is the pullback.
///   4. The current candle closes below that pullback High — confirming the downward resumption.
///
/// Earlier implementations fired on step 1 already and used AllowStepIn to wait for 3+4.
/// That caused signal statistics (entry price, time-in-trade, win rate) to be measured from
/// the CHoCH candle instead of the actual entry, skewing results. Moving the step-3+4 checks
/// into IsSignal means SignalPrice equals the breakout candle close, so statistics reflect
/// the real entry.
/// </summary>
public class SignalBosChochShort : SignalCreateBase
{
    // Maximum age (in candles) of the CHoCH event that can still trigger a signal when the
    // pullback break finally happens. Covers the typical CHoCH → pullback → break sequence
    // plus some slack; anything older is treated as stale (e.g. after a bot restart).
    private const int MaxEventAgeCandles = 35;

    // After this signal fires, the trader has this many candles to actually open a position
    // before we give up (for example when no slot is free).
    private const int GiveUpCandles = 10;


    public override bool IsSignal()
    {
        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval10m)
            return false;

        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;

        CryptoTrendData data = SymbolInterval.TrendBos;

        // A bearish CHoCH event must be present on the swing sequence.
        if (data.LastStructureEvent != CryptoStructureEvent.ChoCh ||
            data.LastStructureEventTime == null ||
            data.LastStructureEventPrice == null ||
            data.Trend != CryptoTrendIndicator.Bearish)
        {
            ExtraText = "no CHoCH";
            return false;
        }

        // Don't fire twice on the same event.
        if (data.LastFiredStructureEventTime != null &&
            data.LastFiredStructureEventTime >= data.LastStructureEventTime)
        {
            ExtraText = "CHoCH already fired";
            return false;
        }

        // Reject ancient CHoCHs (e.g. when the bot has just started and the last CHoCH
        // is already many candles old with its pullback long gone).
        CandleTime cutoff = CandleLast.Candle.OpenTime - MaxEventAgeCandles * Interval.Duration;
        if (data.LastStructureEventTime < cutoff)
        {
            ExtraText = "CHoCH too old";
            return false;
        }

        // A pullback pivot (ZigZag High) must have formed after the CHoCH candle.
        if (data.LastPivotType != 'H' || data.LastPivotTime <= data.LastStructureEventTime)
        {
            ExtraText = "waiting for pullback pivot (ZigZag High)";
            return false;
        }

        // The current candle must close below the pullback High — this is the breakout that
        // confirms the downward resumption and becomes the actual step-in moment.
        if (CandleLast.Candle.Close >= data.LastPivotValue)
        {
            ExtraText = $"waiting for break below pullback high {data.LastPivotValue:N8}";
            return false;
        }

        ExtraText = $"CHoCH Short break @ {CandleLast.Candle.Close:N8} (pullback H {data.LastPivotValue:N8})";
        data.LastFiredStructureEventTime = data.LastStructureEventTime;
        data.LastTrend = data.Trend;
        return true;
    }


    /// <summary>
    /// Give up when the BOS/CHoCH structure has reverted to Bullish, or when the trader fails
    /// to pick up the signal within GiveUpCandles bars after it fired.
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        // Structure has already broken back up — setup is invalidated
        if (SymbolInterval.TrendBos.Trend == CryptoTrendIndicator.Bullish)
        {
            ExtraText = "BOS/CHoCH structure reverted to bullish";
            return true;
        }

        // Time limit exceeded
        if (CandleTime.FromDateTime(signal.CloseDate).Minutes + GiveUpCandles * Interval.Duration < CandleLast.Candle.OpenTime.Minutes)
        {
            ExtraText = $"give up after {GiveUpCandles} candles";
            return true;
        }

        return false;
    }
}

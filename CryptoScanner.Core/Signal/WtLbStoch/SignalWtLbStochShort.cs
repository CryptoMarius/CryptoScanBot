#if DEBUG
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Indicators;

namespace CryptoScanner.Core.Signal.WtLbStoch;

/// <summary>
/// Short variant — mirror of <see cref="SignalWtLbStochLong"/>.
///
/// Setup (in evaluation order — cheapest first):
///   1. BB width inside the Stobb-configured range.
///   2. Optional trend filter: close &lt; SMA200.
///   3. Stoch %K must have crossed down through StochCenterLevel within the last
///      StochCrossLookback candles.
///   4. WT1 crosses down through WtCrossShortLevel (e.g. +60) between the previous and
///      the current candle — the recovery from the deep overbought extreme.
///   5. Qualifier: WT1 must have been uninterruptedly above 0 for at least
///      WtConsecutiveBarsBelowAboveZero bars ending at the bar before the cross.
/// </summary>
public class SignalWtLbStochShort : SignalWtLbStochBase
{
    public override bool IsSignal()
    {
        ExtraText = "";

        // 1. BB width — reuse Stobb thresholds.
        if (!CandleLast.CheckBollingerBandsWidth(
                GlobalData.Settings.Signal.Stobb.BBMinPercentage,
                GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        var settings = GlobalData.Settings.Signal.WtLbStoch;

        // 2. Trend filter (optional, O(1)).
        if (settings.RequireTrendFilter)
        {
            decimal close = CandleLast.Candle.Close;
            decimal sma200 = (decimal)CandleLast.CandleData!.Sma200!.Value;
            if (close >= sma200)
            {
                ExtraText = $"close {close} not below sma200 {sma200:N4}";
                return false;
            }
        }

        // 3. Stoch %K cross DOWN through StochCenterLevel within last StochCrossLookback candles.
        double centerLevel = (double)settings.StochCenterLevel;
        bool stochCrossed = false;
        int stochCrossOffset = -1;
        MyData? data = CandleLast;
        for (int k = 0; k < settings.StochCrossLookback; k++)
        {
            if (!GetPrevCandle(data, out MyData? prev) || prev == null)
                break;
            if (data?.CandleData?.StochOscillator is double curK
                && prev.CandleData?.StochOscillator is double prevK
                && prevK >= centerLevel && curK < centerLevel)
            {
                stochCrossed = true;
                stochCrossOffset = k;
                break;
            }
            data = prev;
        }
        if (!stochCrossed)
        {
            ExtraText = $"stoch %K did not cross down through {centerLevel:N0} in last {settings.StochCrossLookback}";
            return false;
        }

        // 4. WT1 indicator (full history) — expensive, only reached after cheap gates passed.
        var indicator = new WaveTrendIndicator(settings.ChannelLength, settings.AverageLength);
        var results = indicator.Calculate(SymbolInterval.CandleList);

        if (results.Count < 2)
        {
            ExtraText = "not enough wt history";
            return false;
        }

        var curr = results[^1];
        var prev1 = results[^2];

        if (curr.Wt1 == null || prev1.Wt1 == null)
        {
            ExtraText = "wt values not yet available";
            return false;
        }

        double crossLevel = (double)settings.WtCrossShortLevel;
        if (!(prev1.Wt1.Value >= crossLevel && curr.Wt1.Value < crossLevel))
        {
            ExtraText = $"no wt1 cross down through {crossLevel:N1}";
            return false;
        }

        // 5. Qualifier — wt1 uninterruptedly above 0 for at least N bars, ending at prev1.
        int consecutive = 0;
        for (int i = results.Count - 2; i >= 0; i--)
        {
            if (results[i].Wt1 is double v && v > 0)
                consecutive++;
            else
                break;
        }
        if (consecutive < settings.WtConsecutiveBarsBelowAboveZero)
        {
            ExtraText = $"only {consecutive} consecutive bars above 0 (need {settings.WtConsecutiveBarsBelowAboveZero})";
            return false;
        }

        ExtraText = $"wt1 cross down {crossLevel:N1} after {consecutive} bars above 0, " +
                    $"stoch %K cross {centerLevel:N0} {stochCrossOffset} candle(s) ago";
        return true;
    }
}
#endif

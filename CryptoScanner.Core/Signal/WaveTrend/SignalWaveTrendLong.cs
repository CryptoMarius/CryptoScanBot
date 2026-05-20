using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Indicators;

#if DEBUG
namespace CryptoScanner.Core.Signal.WaveTrend;

/// <summary>
/// Long variant of the WaveTrend [LazyBear] strategy (WT_LB).
///
/// Setup (in evaluation order — cheapest first):
///   1. Bollinger band width inside the configured range.
///   2. Optional trend filter: close > SMA200 (substitute for LazyBear's EMA200).
///   3. WT1 crosses up through the oversold level between the previous and the current candle.
///   4. Excursion check over the last
///      <see cref="Settings.Strategy.SettingsSignalStrategyWaveTrend.LookbackBars"/> bars
///      ending at the candle before the cross:
///        a) area below the OS line ≥
///           <see cref="Settings.Strategy.SettingsSignalStrategyWaveTrend.MinAreaInZone"/>
///           — filters out WT1 wiggling around the OS line.
///        b) min(WT1) ≤ OsLevel −
///           <see cref="Settings.Strategy.SettingsSignalStrategyWaveTrend.DeepLevelOffset"/>
///           — guarantees the excursion actually reached an extreme.
/// </summary>
public class SignalWaveTrendLong : SignalWaveTrendBase
{
    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        var settings = GlobalData.Settings.Signal.WaveTrend;

        // 1. Trend filter — runs in O(1), so evaluate first and bail out before computing WT.
        if (settings.RequireTrendFilter)
        {
            decimal close = CandleLast.Candle.Close;
            decimal sma200 = (decimal)CandleLast.CandleData!.Sma200!.Value;
            if (close <= sma200)
            {
                ExtraText = $"close {close} not above sma200 {sma200:N4}";
                return false;
            }
        }

        // 2. Compute WT over the full candle history.
        var indicator = new WaveTrendIndicator(settings.ChannelLength, settings.AverageLength);
        var results = indicator.Calculate(SymbolInterval.CandleList);

        if (results.Count < 2)
        {
            ExtraText = "not enough wt history";
            return false;
        }

        var curr = results[^1];
        var prev = results[^2];

        if (curr.Wt1 == null || prev.Wt1 == null)
        {
            ExtraText = "wt values not yet available";
            return false;
        }

        double osLevel = (double)settings.OsLevel;

        // 3. Cross-up through OS level: prev was at or below OS, current is above.
        if (!(prev.Wt1.Value <= osLevel && curr.Wt1.Value > osLevel))
        {
            ExtraText = "no wt1 cross up over os level";
            return false;
        }

        // 4. Excursion measured over the last LookbackBars ending at `prev`:
        //    area  = Σ max(0, osLevel − wt1) — how much oversold "mass" was accumulated.
        //    minWt = deepest WT1 reached       — was the excursion actually extreme?
        double area = 0.0;
        double minWt = double.PositiveInfinity;
        int from = Math.Max(0, results.Count - 1 - settings.LookbackBars);
        for (int i = from; i <= results.Count - 2; i++)
        {
            if (results[i].Wt1 is not double v) continue;
            if (v < osLevel) area += osLevel - v;
            if (v < minWt) minWt = v;
        }

        double minArea = (double)settings.MinAreaInZone;
        if (area < minArea)
        {
            ExtraText = $"wt1 os-area {area:N1} below required {minArea:N1}";
            return false;
        }

        double deepLevel = osLevel - (double)settings.DeepLevelOffset;
        if (minWt > deepLevel)
        {
            ExtraText = $"wt1 only reached {minWt:N1}, needs ≤ {deepLevel:N1}";
            return false;
        }

        ExtraText = $"wt cross-up over os (area {area:N1}, min {minWt:N1})";
        return true;
    }
}
#endif

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Indicators;

#if DEBUG
namespace CryptoScanner.Core.Signal.WaveTrend;

/// <summary>
/// Short variant — mirror of <see cref="SignalWaveTrendLong"/>.
///
/// Setup (in evaluation order — cheapest first):
///   1. Bollinger band width inside the configured range.
///   2. Optional trend filter: close &lt; SMA200.
///   3. WT1 crosses down through the overbought level between the previous and the current candle.
///   4. Excursion check over the last
///      <see cref="Settings.Strategy.SettingsSignalStrategyWaveTrend.LookbackBars"/> bars
///      ending at the candle before the cross:
///        a) area above the OB line ≥
///           <see cref="Settings.Strategy.SettingsSignalStrategyWaveTrend.MinAreaInZone"/>
///           — filters out WT1 wiggling around the OB line.
///        b) max(WT1) ≥ ObLevel +
///           <see cref="Settings.Strategy.SettingsSignalStrategyWaveTrend.DeepLevelOffset"/>
///           — guarantees the excursion actually reached an extreme.
/// </summary>
public class SignalWaveTrendShort : SignalWaveTrendBase
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

        // 1. Trend filter — cheap, evaluate first.
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

        double obLevel = (double)settings.ObLevel;

        // 3. Cross-down through OB level: prev was at or above OB, current is below.
        if (!(prev.Wt1.Value >= obLevel && curr.Wt1.Value < obLevel))
        {
            ExtraText = "no wt1 cross down over ob level";
            return false;
        }

        // 4. Excursion measured over the last LookbackBars ending at `prev`:
        //    area  = Σ max(0, wt1 − obLevel) — how much overbought "mass" was accumulated.
        //    maxWt = highest WT1 reached      — was the excursion actually extreme?
        double area = 0.0;
        double maxWt = double.NegativeInfinity;
        int from = Math.Max(0, results.Count - 1 - settings.LookbackBars);
        for (int i = from; i <= results.Count - 2; i++)
        {
            if (results[i].Wt1 is not double v) continue;
            if (v > obLevel) area += v - obLevel;
            if (v > maxWt) maxWt = v;
        }

        double minArea = (double)settings.MinAreaInZone;
        if (area < minArea)
        {
            ExtraText = $"wt1 ob-area {area:N1} below required {minArea:N1}";
            return false;
        }

        double deepLevel = obLevel + (double)settings.DeepLevelOffset;
        if (maxWt < deepLevel)
        {
            ExtraText = $"wt1 only reached {maxWt:N1}, needs ≥ {deepLevel:N1}";
            return false;
        }

        ExtraText = $"wt cross-down over ob (area {area:N1}, max {maxWt:N1})";
        return true;
    }
}
#endif

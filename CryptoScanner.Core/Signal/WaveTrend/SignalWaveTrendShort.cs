using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Indicators;

#if DEBUG
namespace CryptoScanner.Core.Signal.WaveTrend;

/// <summary>
/// Short variant — mirror of <see cref="SignalWaveTrendLong"/>.
///
/// Setup (in evaluation order — cheapest first):
///   1. Optional trend filter: close &lt; SMA200.
///   2. WT1 crosses down through the overbought level between the previous and the current candle.
///   3. WT1 must have remained at-or-above the overbought level for at least
///      <see cref="Settings.Strategy.SettingsSignalStrategyWaveTrend.MinBarsInZone"/>
///      consecutive bars ending at the candle before the cross.
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

        // 4. Dwell — count consecutive bars ending at `prev` where wt1 stayed at-or-above OB.
        int dwell = 0;
        for (int i = results.Count - 2; i >= 0; i--)
        {
            if (results[i].Wt1 is double v && v >= obLevel)
                dwell++;
            else
                break;
        }

        if (dwell < settings.MinBarsInZone)
        {
            //ExtraText = $"wt1 only {dwell} consecutive bars in ob zone (need {settings.MinBarsInZone})";
            return false;
        }

        ExtraText = $"wt cross-down over ob after {dwell} bars in zone";
        return true;
    }
}
#endif

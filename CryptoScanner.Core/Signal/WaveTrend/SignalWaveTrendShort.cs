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
///   3. WT1 crosses down through +RecoveryLevel (e.g. +50) between the previous and the
///      current candle — the recovery from the overbought extreme.
///   4. Qualifier: within the last
///      <see cref="Settings.Strategy.SettingsSignalStrategyWaveTrend.LookbackBars"/> bars
///      ending at the candle before the cross, WT1 must have been above
///      <see cref="Settings.Strategy.SettingsSignalStrategyWaveTrend.ObLevel"/> on at least
///      <see cref="Settings.Strategy.SettingsSignalStrategyWaveTrend.MinBarsBeyondOsOb"/>
///      bars. Filters out shallow excursions that never reached the deep OB zone.
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

        if (CandleLast.StochOversold())
        {
            ExtraText = "stoch already oversold";
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
        double recoveryLevel = (double)settings.ObRecoveryLevel;

        // 3. Recovery cross — WT1 crosses down through +RecoveryLevel.
        if (!(prev.Wt1.Value >= recoveryLevel && curr.Wt1.Value < recoveryLevel))
        {
            ExtraText = $"no wt1 cross down over {recoveryLevel:N1}";
            return false;
        }

        // 4. Qualifier — count bars within the lookback (ending at prev) where WT1 was
        //    genuinely beyond the OB level. Need not be consecutive.
        int barsBeyond = 0;
        int from = Math.Max(0, results.Count - 1 - settings.LookbackBars);
        for (int i = from; i <= results.Count - 2; i++)
        {
            if (results[i].Wt1 is double v)
            {
                if (v > obLevel)
                    barsBeyond++;
                if (v < obLevel)
                    break;
            }
        }

        if (barsBeyond < settings.MinBarsBeyondOsOb)
        {
            ExtraText = $"only {barsBeyond} bars above {obLevel:N1} (need {settings.MinBarsBeyondOsOb})";
            return false;
        }

        ExtraText = $"wt recovery cross over {recoveryLevel:N1} after {barsBeyond} bars above {obLevel:N1}";
        return true;
    }
}
#endif

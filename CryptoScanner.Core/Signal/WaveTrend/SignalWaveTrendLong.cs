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
///   3. WT1 crosses up through −RecoveryLevel (e.g. −50) between the previous and the
///      current candle — the recovery from the oversold extreme.
///   4. Qualifier: within the last
///      <see cref="Settings.Strategy.SettingsSignalStrategyWaveTrend.LookbackBars"/> bars
///      ending at the candle before the cross, WT1 must have been below
///      <see cref="Settings.Strategy.SettingsSignalStrategyWaveTrend.OsLevel"/> on at least
///      <see cref="Settings.Strategy.SettingsSignalStrategyWaveTrend.MinBarsBeyondOsOb"/>
///      bars. Filters out shallow excursions that never reached the deep OS zone.
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

        if (CandleLast.StochOverbought())
        {
            ExtraText = "stoch already overbought";
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
        double recoveryLevel = (double)settings.OsRecoveryLevel;

        // 3. Recovery cross — WT1 crosses up through −RecoveryLevel.
        if (!(prev.Wt1.Value <= recoveryLevel && curr.Wt1.Value > recoveryLevel))
        {
            ExtraText = $"no wt1 cross up over {recoveryLevel:N1}";
            return false;
        }

        // 4. Qualifier — count bars within the lookback (ending at prev) where WT1 was
        //    genuinely beyond the OS level. Need not be consecutive.
        int barsBeyond = 0;
        int from = Math.Max(0, results.Count - 1 - settings.LookbackBars);
        for (int i = from; i <= results.Count - 2; i++)
        {
            if (results[i].Wt1 is double v)
            {
                if (v < osLevel)
                    barsBeyond++;
                if (v > osLevel)
                    break;
            }
        }

        if (barsBeyond < settings.MinBarsBeyondOsOb)
        {
            ExtraText = $"only {barsBeyond} bars below {osLevel:N1} (need {settings.MinBarsBeyondOsOb})";
            return false;
        }

        ExtraText = $"wt recovery cross over {recoveryLevel:N1} after {barsBeyond} bars below {osLevel:N1}";
        return true;
    }
}
#endif

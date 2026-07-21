using CryptoScanner.Analyzers.Stobb;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Nwe.Signal;

/// <summary>
/// Long signal: NWE lower crosses BB lower upward (from outside in).
/// BB lower must still be falling. At least one of the last 5 bars must have
/// closed below BB lower.
///
/// Entry conditions (bars[^1] = current, bars[^2] = prev, bars[^3] = prev2):
///   - prev.NweLower &lt;= prev.BbLower         : NWE lower was outside (below) BB lower
///   - current.NweLower > current.BbLower     : NWE lower crossed inside (above) BB lower
///   - current.BbLower &lt; prev.BbLower        : BB lower still pointing down
///   - current.BbLower &lt; prev.BbLower &lt; prev2.BbLower : BB lower falling for 2 bars
///   - Any of last 5 bars: close &lt; BbLower
/// </summary>
public class SignalNweBbLong : SignalNweBbBase
{

    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(StobbPlugin.Settings.BBMinPercentage, 100))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }


        // NWE lower was outside (below) BB lower
        if (CandleLast.Candle.Close >= (decimal)CandleLast.CandleData.Sma20!)
        {
            ExtraText = $"Candle already above sma20";
            return false;
        }

        if (!TryBuildHistory(out var bars))
        {
            ExtraText = "insufficient history for NWE/BB";
            return false;
        }

        var current = bars[^1];
        var prev = bars[^2];
        var prev2 = bars[^3];

        // NWE lower was outside (below) BB lower
        if (prev.NweLower > prev.BbLower)
        {
            ExtraText = $"NWE lower was not below BB lower ({prev.NweLower:N6} > {prev.BbLower:N6})";
            return false;
        }

        // NWE lower has now crossed inside (above) BB lower
        if (current.NweLower <= current.BbLower)
        {
            ExtraText = $"NWE lower has not crossed above BB lower ({current.NweLower:N6} <= {current.BbLower:N6})";
            return false;
        }

        // BB lower must be falling for at least 2 bars
        if (current.BbLower >= prev.BbLower || prev.BbLower >= prev2.BbLower)
        {
            ExtraText = "BB lower not falling for 2 bars";
            return false;
        }


        // At least one of the last 5 bars must have closed below BB lower
        bool hadExtension = false;
        int lookbackStart = Math.Max(0, bars.Length - 6); // exclude current bar (^1)
        for (int i = lookbackStart; i < bars.Length - 1; i++)
        {
            if (bars[i].Close < bars[i].BbLower)
            {
                hadExtension = true;
                break;
            }
        }

        if (!hadExtension)
        {
            ExtraText = "no candle in last 5 bars closed below BB lower";
            return false;
        }

        ExtraText = $"nwe.bb ↑ nwe={current.NweLower:N6} bb={current.BbLower:N6}";
        return true;
    }

    public override bool GiveUp(CryptoSignal signal)
    {
        if (CandleTime.FromDateTime(signal.CloseDate).Minutes + 3 * Interval.Duration < CandleLast.Candle.OpenTime.Minutes)
        {
            ExtraText = "give up after 3 candles";
            return true;
        }
        return false;
    }
}

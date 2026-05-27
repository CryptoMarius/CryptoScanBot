using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Nwe;

/// <summary>
/// Short signal: NWE upper crosses BB upper downward (from outside in).
/// BB upper must still be rising. At least one of the last 10 bars must have
/// closed above BB upper.
///
/// Entry conditions (bars[^1] = current, bars[^2] = prev, bars[^3] = prev2):
///   - prev.NweUpper >= prev.BbUpper         : NWE upper was outside (above) BB upper
///   - current.NweUpper &lt; current.BbUpper   : NWE upper crossed inside (below) BB upper
///   - current.BbUpper > prev.BbUpper        : BB upper still pointing up
///   - current.BbUpper > prev.BbUpper > prev2.BbUpper : BB upper rising for 2 bars
///   - Any of last 10 bars: close > BbUpper
/// </summary>
public class SignalNweBbShort : SignalNweBbBase
{

    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
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

        // NWE upper was outside (above) BB upper
        if (prev.NweUpper < prev.BbUpper)
        {
            ExtraText = $"NWE upper was not above BB upper ({prev.NweUpper:N6} < {prev.BbUpper:N6})";
            return false;
        }

        // NWE upper has now crossed inside (below) BB upper
        if (current.NweUpper >= current.BbUpper)
        {
            ExtraText = $"NWE upper has not crossed below BB upper ({current.NweUpper:N6} >= {current.BbUpper:N6})";
            return false;
        }

        // BB upper must be rising for at least 2 bars
        if (current.BbUpper <= prev.BbUpper || prev.BbUpper <= prev2.BbUpper)
        {
            ExtraText = "BB upper not rising for 2 bars";
            return false;
        }

        // At least one of the last 10 bars must have closed above BB upper
        bool hadExtension = false;
        int lookbackStart = Math.Max(0, bars.Length - 6); // exclude current bar (^1)
        for (int i = lookbackStart; i < bars.Length - 1; i++)
        {
            if (bars[i].Close > bars[i].BbUpper)
            {
                hadExtension = true;
                break;
            }
        }

        if (!hadExtension)
        {
            ExtraText = "no candle in last 10 bars closed above BB upper";
            return false;
        }

        ExtraText = $"nwe.bb ↓ nwe={current.NweUpper:N6} bb={current.BbUpper:N6}";
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

#if DEBUG
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Squeeze;

// Squeeze.Fade (long): counter-trend reversal. After volatility has been compressed
// (recent squeeze in the lookback window), price wicks below the lower BB at a Stoch
// oversold extreme — and then Stoch %K crosses back up through %D. We fade the
// false breakout, expecting the released energy to push back the other way.
public class SignalSqueezeFadeLong : SignalSqueezeBase
{
    public override bool IsSignal()
    {
        ExtraText = "";
        var settings = GlobalData.Settings.Signal.Squeeze;

        // ---- Cheap, candle-local checks first ----

        // BB width sanity (drop completely flat bands or runaway-wide ones).
        if (!CandleLast.CheckBollingerBandsWidth(settings.BBMinPercentage, settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width out of range {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // Price wicks/closes below the lower BB (Stobb-style trigger).
        if (!CandleLast.IsBelowBollingerBands(settings.UseLowHigh))
        {
            ExtraText = "not below bb.lower";
            return false;
        }

        // Stoch in oversold zone (both %K and %D below threshold).
        if (!CandleLast.StochOversold())
        {
            ExtraText = "stoch not oversold";
            return false;
        }

        // Current candle: %K above %D (cross-up has already happened on or before this bar).
        if (CandleLast.CandleData!.StochOscillator <= CandleLast.CandleData!.StochSignal)
        {
            ExtraText = "stoch %K not above %D";
            return false;
        }

        // ---- Slightly more expensive: prev candle for the cross direction ----
        if (!GetPrevCandle(CandleLast, out MyData? prev) || prev == null)
            return false;
        // Cross must have actually flipped on this bar (was K<=D, now K>D).
        if (prev.CandleData!.StochOscillator > prev.CandleData!.StochSignal)
        {
            ExtraText = "no fresh stoch bull cross";
            return false;
        }

        // ---- Walk back through history: was there a squeeze recently? ----
        ScanSqueeze(settings.FadeSqueezeLookback, out int squeezeCount, out int lastSqueezeOffset);
        if (lastSqueezeOffset < 0 || squeezeCount == 0)
        {
            ExtraText = $"no recent squeeze in last {settings.FadeSqueezeLookback} candles";
            return false;
        }

        ExtraText = $"squeeze {squeezeCount}/{settings.FadeSqueezeLookback} candles, last {lastSqueezeOffset} ago";

        // ---- Trend filter last (most expensive) ----
        if (settings.CheckTrendPrimaryDirection && !CheckTrendPrimary(settings.TrendPrimaryDirectionCount))
            return false;
        if (settings.CheckTrendSecondaryDirection && !CheckTrendSecondary(settings.TrendSecondaryDirectionCount))
            return false;

        return true;
    }
}
#endif

#if DEBUG
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Squeeze;

// Squeeze.Fade (short): counter-trend reversal. After volatility has been compressed
// (recent squeeze in the lookback window), price wicks above the upper BB at a Stoch
// overbought extreme — and then Stoch %K crosses back down through %D. We fade the
// false breakout, expecting the released energy to push back the other way.
public class SignalSqueezeFadeShort : SignalSqueezeBase
{
    public override bool IsSignal()
    {
        ExtraText = "";
        var settings = GlobalData.Settings.Signal.Squeeze;

        // ---- Cheap, candle-local checks first ----
        if (!CandleLast.CheckBollingerBandsWidth(settings.BBMinPercentage, settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width out of range {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        if (!CandleLast.IsAboveBollingerBands(settings.UseLowHigh))
        {
            ExtraText = "not above bb.upper";
            return false;
        }

        if (!CandleLast.StochOverbought())
        {
            ExtraText = "stoch not overbought";
            return false;
        }

        // Current candle: %K below %D (cross-down has already happened).
        if (CandleLast.CandleData!.StochOscillator >= CandleLast.CandleData!.StochSignal)
        {
            ExtraText = "stoch %K not below %D";
            return false;
        }

        // ---- prev candle for cross direction ----
        if (!GetPrevCandle(CandleLast, out MyData? prev) || prev == null)
            return false;
        if (prev.CandleData!.StochOscillator < prev.CandleData!.StochSignal)
        {
            ExtraText = "no fresh stoch bear cross";
            return false;
        }

        // ---- Recent squeeze in window ----
        ScanSqueeze(settings.FadeSqueezeLookback, out int squeezeCount, out int lastSqueezeOffset);
        if (lastSqueezeOffset < 0 || squeezeCount == 0)
        {
            ExtraText = $"no recent squeeze in last {settings.FadeSqueezeLookback} candles";
            return false;
        }

        ExtraText = $"squeeze {squeezeCount}/{settings.FadeSqueezeLookback} candles, last {lastSqueezeOffset} ago";

        // ---- Trend filter last ----
        if (settings.CheckTrendPrimaryDirection && !CheckTrendPrimary(settings.TrendPrimaryDirectionCount))
            return false;
        if (settings.CheckTrendSecondaryDirection && !CheckTrendSecondary(settings.TrendSecondaryDirectionCount))
            return false;

        return true;
    }
}
#endif

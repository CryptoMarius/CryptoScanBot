#if DEBUG
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Squeeze;

// Squeeze.Brk (short): classic TTM Squeeze breakout to the downside. Bands were
// compressed for at least N candles, the squeeze has released, and momentum kicks
// down via a fresh Stoch %K-under-%D cross.
public class SignalSqueezeBrkShort : SignalSqueezeBase
{
    public override bool IsSignal()
    {
        ExtraText = "";
        var settings = GlobalData.Settings.Signal.Squeeze;

        // ---- Cheap checks first ----
        if (!CandleLast.CheckBollingerBandsWidth(settings.BBMinPercentage, settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width out of range {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        if (CandleLast.IsKeltnerSqueeze())
        {
            ExtraText = "still in squeeze (no release yet)";
            return false;
        }

        if (CandleLast.CandleData!.StochOscillator >= CandleLast.CandleData!.StochSignal)
        {
            ExtraText = "stoch %K not below %D";
            return false;
        }

        // ---- prev candle for fresh cross ----
        if (!GetPrevCandle(CandleLast, out MyData? prev) || prev == null)
            return false;
        if (prev.CandleData!.StochOscillator < prev.CandleData!.StochSignal)
        {
            ExtraText = "no fresh stoch bear cross";
            return false;
        }

        // ---- Walk back: prior squeeze must have built up ----
        ScanSqueeze(settings.BrkReleaseLookback, out int squeezeCount, out int lastSqueezeOffset);
        if (squeezeCount < settings.BrkReleaseMinCandles)
        {
            ExtraText = $"only {squeezeCount} squeeze candles in last {settings.BrkReleaseLookback} (need {settings.BrkReleaseMinCandles})";
            return false;
        }

        ExtraText = $"squeeze {squeezeCount}/{settings.BrkReleaseLookback} candles, released {lastSqueezeOffset} ago";

        // ---- Trend filter last ----
        if (settings.CheckTrendPrimaryDirection && !CheckTrendPrimary(settings.TrendPrimaryDirectionCount))
            return false;
        if (settings.CheckTrendSecondaryDirection && !CheckTrendSecondary(settings.TrendSecondaryDirectionCount))
            return false;

        return true;
    }
}
#endif

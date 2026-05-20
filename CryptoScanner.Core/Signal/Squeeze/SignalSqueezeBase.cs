#if DEBUG
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Squeeze;

// Base class for the TTM Squeeze family (squeeze.fade + squeeze.brk).
// Holds shared indicator availability + a small scanner that walks back N candles
// to count squeeze candles and report how recently a squeeze was active.
public class SignalSqueezeBase : SignalCreateBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Sma20 == null
           || data.CandleData.StochSignal == null
           || data.CandleData.StochOscillator == null
           || data.CandleData.BollingerBandsDeviation == null
           || data.CandleData.KeltnerUpperBand == null
           || data.CandleData.KeltnerLowerBand == null
           )
            return false;

        return true;
    }


    // Walk back at most `lookback` candles (including CandleLast at offset 0) and report
    // how many of them were a squeeze, and the offset of the most recent squeeze candle
    // (0 = current candle is a squeeze, -1 = no squeeze found in the window).
    //
    // Cheap: per candle this is 4 numeric compares + a dictionary lookup for prev.
    // No trend / structure work is done here — keep this before any expensive filter.
    protected void ScanSqueeze(int lookback, out int squeezeCount, out int lastSqueezeOffset)
    {
        squeezeCount = 0;
        lastSqueezeOffset = -1;

        MyData? candle = CandleLast;
        for (int j = 0; j < lookback && candle != null; j++)
        {
            if (!IndicatorsOkay(candle))
                break;

            if (candle.IsKeltnerSqueeze())
            {
                squeezeCount++;
                if (lastSqueezeOffset < 0)
                    lastSqueezeOffset = j;
            }

            if (!GetPrevCandle(candle, out candle))
                break;
        }
    }
}
#endif

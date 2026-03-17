using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Experiment;

#if DEBUG
/// <summary>
/// BBMA Magic Extreme – Long (buy) signal.
///
/// A Magic Extreme occurs when BOTH the WMA5 and WMA10 of candle lows have pierced below
/// the lower Bollinger Band (20, 2σ), while the candle itself also wicked below the band
/// but closed back above it.  This is stricter than the regular BBMA Extreme, which only
/// requires WMA5 to be outside the band.

public class SignalBbmaMagicExtreme : SignalBbmaBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
            || data.Candle.OpenTime == 0
            || data.CandleData == null
            || data.CandleData.Wma05Low == null
            || data.CandleData.Wma10Low == null
            || data.CandleData.Wma05High == null
            || data.CandleData.Wma10High == null
            || data.CandleData.BollingerBandsDeviation == null)
            return false;

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // Bollinger Bands width guard (reuse Stobb settings)
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        if (SignalSide == Enums.CryptoTradeSide.Long)
        {
            if (CandleLast.CandleData.Wma05Low < CandleLast.CandleData.BollingerBandsLowerBand
                && CandleLast.CandleData.Wma10Low < CandleLast.CandleData.BollingerBandsLowerBand)
                return true;
        }
        else
        {
            if (CandleLast.CandleData.Wma05High > CandleLast.CandleData.BollingerBandsUpperBand
                && CandleLast.CandleData.Wma10High > CandleLast.CandleData.BollingerBandsUpperBand)
                return true;
        }

        return false;
    }
}
#endif

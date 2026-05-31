using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Sbm;

namespace CryptoScanner.Core.Signal.Stobb;

public class SignalStobbBase : SignalSbmBase
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
           )
            return false;

        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        if (!base.GiveUp(signal))
            return false;

        switch (SignalSide)
        {
            case Enums.CryptoTradeSide.Long:
                if (CandleLast?.Candle.Close > (decimal?)CandleLast?.CandleData?.Sma20)
                {
                    ExtraText = "Close above sma20";
                    return true;
                }
                break;
            case Enums.CryptoTradeSide.Short:
                if (CandleLast!.Candle.Close < (decimal?)CandleLast!.CandleData?.Sma20)
                {
                    ExtraText = "Close below sma20";
                    return true;
                }
                break;
        }

        ExtraText = "";
        return false;
    }
}

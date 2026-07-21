using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Stobb.Signal;

public class SignalStobbBase : SignalCreateBase
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
        if (base.GiveUp(signal))
            return true;

        switch (SignalSide)
        {
            case CryptoTradeSide.Long:
                if (CandleLast?.Candle.Close > (decimal?)CandleLast?.CandleData?.Sma20)
                {
                    ExtraText = "Close above sma20";
                    return true;
                }
                break;
            case CryptoTradeSide.Short:
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

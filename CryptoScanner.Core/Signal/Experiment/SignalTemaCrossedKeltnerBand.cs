using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Experiment;

public class SignalTemaCrossedKeltnerBand : SignalCreateBase
{
    public SignalTemaCrossedKeltnerBand(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Ema9 == null
           || candle.CandleData.KeltnerCenterLine == null
           )
            return false;

        return true;
    }



    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 0))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // It looks powerfull, but is it a valuable?
        if (GetPrevCandle(CandleLast, out CryptoCandle? candlePrev))
        {
            if (SignalSide == CryptoTradeSide.Short)
            {
                if (candlePrev!.CandleData!.Tema > candlePrev.CandleData.KeltnerLowerBand
                    && CandleLast.CandleData!.Tema <= CandleLast.CandleData.KeltnerLowerBand)
                {
                    return true;
                }
            }
            else
            {
                if (candlePrev!.CandleData!.Tema < candlePrev.CandleData.KeltnerUpperBand
                    && CandleLast.CandleData!.Tema >= CandleLast.CandleData.KeltnerUpperBand)
                {
                    return true;
                }
            }
        }
        return false;
    }

}

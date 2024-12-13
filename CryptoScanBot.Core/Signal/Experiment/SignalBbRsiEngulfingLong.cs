using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalBbRsiEngulfingLong : SignalCreateBase
{
    public SignalBbRsiEngulfingLong(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(account, symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.BbRsiEngulfing;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
            || (candle.CandleData.Rsi == null)
            || (candle.CandleData.BollingerBandsLowerBand == null)
            )
            return false;

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100)) //GlobalData.Settings.Signal.AnalysisBBMaxPercentage
        {
            ExtraText = "bb.width to small " + CandleLast.CandleData!.BollingerBandsPercentage?.ToString("N2");
            return false;
        }


        if (!GetPrevCandle(CandleLast!, out CryptoCandle? prev))
            return false;


        // Prev below BB
        if (prev!.Close >= (decimal)prev!.CandleData!.BollingerBandsLowerBand!)
        {
            ExtraText = "not below bb.lower";
            return false;
        }

        // Rsi oversold
        if (!prev!.IsRsiOversold(0))
        {
            ExtraText = "rsi not oversold";
            return false;
        }

        // Candle last closes above the high of the previous
        if (CandleLast.Close <= prev!.High)
        {
            ExtraText = "not engulfing";
            return false;
        }

        //if (HadStorsiInThelastXCandles(SignalSide, 0, 25) == null)
        //{
        //    ExtraText = "no previous storsi found";
        //    return false;
        //}

        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        // Langer dan 3 candles willen we niet wachten
        if (CandleLast.OpenTime - signal.EventTime > 3 * Interval.Duration)
        {
            ExtraText = "Ophouden na 3 candles";
            return true;
        }

        ExtraText = "";
        return false;
    }

}

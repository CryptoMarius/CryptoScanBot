using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalBbRsiEngulfingShort : SignalCreateBase
{
    public SignalBbRsiEngulfingShort(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(account, symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
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


        // Prev above BB
        //if (!prev!.IsAboveBollingerBands(GlobalData.Settings.Signal.Stobb.UseLowHigh))
        if (prev!.Close <= (decimal)prev!.CandleData!.BollingerBandsUpperBand!)
        {
            ExtraText = "not below bb.upper";
            return false;
        }

        // Rsi oversold
        if (!prev!.IsRsiOverbought(0))
        {
            ExtraText = "rsi not overbought";
            return false;
        }

        // Candle last closes above the high of the previous
        if (CandleLast.Close >= prev!.Low)
        {
            ExtraText = "not engulfing";
            return false;
        }
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

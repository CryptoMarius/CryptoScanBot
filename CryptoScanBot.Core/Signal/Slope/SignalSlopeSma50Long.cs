using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Slope;

#if EXTRASTRATEGIESSLOPESMA
public class SignalSlopeSma50Long : SignalCreateBase
{
    public SignalSlopeSma50Long(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.SlopeSma50;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.SlopeSma50 == null)
           || (candle.CandleData.Rsi == null)
           )
            return false;

        return true;
    }


    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        //if (!BarometerHelper.CheckValidBarometer(Symbol.QuoteData, CryptoIntervalPeriod.interval1h, (0.5m, decimal.MaxValue), out string reaction))
        //{
        //    response = reaction;
        //    return false;
        //}


        if (HadStobbInThelastXCandles(SignalSide, 10, 60) == null)
        {
            response = "Geen voorgaande STOBB of SBM";
            return false;
        }

        response = "";
        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        if (CandleLast.CandleData.SlopeSma50 < 0)
            return false;

        if (!GetPrevCandle(CandleLast, out CryptoCandle prevCandle))
            return false;

        if (prevCandle.CandleData.SlopeSma50 > 0)
            return false;

        return true;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        if (CandleLast.IsBelowBollingerBands(GlobalData.Settings.Signal.Sbm.UseLowHigh))
        {
            ExtraText = "beneden de bb";
            return false;
        }

        //if (CandleLast.CandleData.Sma50 >= CandleLast.CandleData.Ema50)
        //    return false;


        if (CandleLast.CandleData.Rsi < 55)
        {
            ExtraText = $"De RSI niet herstellend {CandleLast.CandleData.Rsi:N8} {CandleLast.CandleData.Rsi:N8} (last.2)";
            return false;
        }
        if (CandleLast.CandleData.StochOscillator < 60)
        {
            ExtraText = string.Format("De Stoch.K is niet hoog genoeg {0:N8}", CandleLast.CandleData.StochOscillator);
            return false;
        }
        if (CandleLast.CandleData.StochSignal < 60)
        {
            ExtraText = string.Format("De Stoch.D is niet hoog genoeg {0:N8}", CandleLast.CandleData.StochSignal);
            return false;
        }

        //ExtraText = "Alles lijkt goed";
        return true;
    }



    public override bool GiveUp(CryptoSignal signal)
    {
        ExtraText = "";


        // Langer dan 60 candles willen we niet wachten (is 60 niet heel erg lang?)
        if ((CandleLast.OpenTime - signal.EventTime) > 10 * Interval.Duration)
        {
            ExtraText = "Ophouden na 10 candles";
            return true;
        }

        ExtraText = "";
        return false;
    }


}
#endif
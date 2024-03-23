using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Signal;

#if EXTRASTRATEGIESSLOPESMA
public class SignalSlopeSma20Long : SignalCreateBase
{
    public SignalSlopeSma20Long(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.SlopeSma20;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.SlopeSma20 == null)
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

        if (CandleLast.CandleData.SlopeSma20 < 0)
            return false;

        if (!GetPrevCandle(CandleLast, out CryptoCandle prevCandle))
            return false;

        if (prevCandle.CandleData.SlopeSma20 > 0)
            return false;

        return true;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Na de initiele melding hebben we 3 candles de tijd om in te stappen, maar alleen indien de MACD verbetering laat zien.

        // Er een candle onder de bb opent of sluit
        if (CandleLast.IsBelowBollingerBands(GlobalData.Settings.Signal.Sbm.UseLowHigh))
        {
            ExtraText = "beneden de bb";
            return false;
        }

        // De markt is nog niet echt positief
        // (maar missen we meldingen hierdoor denk het wel!?)
        //if (CandleLast.CandleData.Sma20 >= CandleLast.CandleData.Ema20)
        //    return false;

        //if (!GetPrevCandle(CandleLast, out CryptoCandle candlePrev))
        //    return false;

        //// Herstel? Verbeterd de RSI
        //if ((CandleLast.CandleData.Rsi < candlePrev.CandleData.Rsi))
        //{
        //    ExtraText = string.Format("De RSI niet herstellend {0:N8} {1:N8} (last.1)", candlePrev.CandleData.Rsi, CandleLast.CandleData.Rsi);
        //    return false;
        //}

        if ((CandleLast.CandleData.Rsi < 55))
        {
            ExtraText = string.Format("De RSI niet herstellend {0:N8} {1:N8} (last.2)", CandleLast.CandleData.Rsi, CandleLast.CandleData.Rsi);
            return false;
        }
        if ((CandleLast.CandleData.StochOscillator < 60))
        {
            ExtraText = string.Format("De Stoch.K is niet hoog genoeg {0:N8}", CandleLast.CandleData.StochOscillator);
            return false;
        }
        if ((CandleLast.CandleData.StochSignal < 60))
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
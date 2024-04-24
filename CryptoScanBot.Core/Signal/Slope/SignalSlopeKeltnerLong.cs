using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal;

namespace CryptoScanBot.Signal.Slope;

#if EXTRASTRATEGIESSLOPEKELTNER
public class SignalSlopeKeltnerLong : SignalCreateBase
{
    public SignalSlopeKeltnerLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.SlopeKeltner;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.KeltnerCenterLineSlope == null)
           )
            return false;

        return true;
    }

    public bool WasKeltnerNegativeInTheLast(int candleCount = 10)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        CryptoCandle candle = CandleLast;
        while (candleCount >= 0)
        {
            if (CandleLast.CandleData.KeltnerCenterLineSlope > 0)
                return false;

            if (!GetPrevCandle(candle, out candle))
                return false;
            candleCount--;
        }
        return true;
    }

    /// <summary>
    /// Is de ... oplopend in de laatste x candles
    /// 2e parameter geeft aan hoeveel afwijkend mogen zijn
    /// </summary>
    public bool CheckKeltnerSlopeInTheLastPeriod(int candleCount, int allowedWrongCount, out string response)
    {
        // We gaan van rechts naar links (van de nieuwste candle richting verleden)
        bool first = true;
        int wrongCount = 0;
        CryptoCandle last = CandleLast;

        // En van de candles daarvoor mag er een (of meer) afwijken
        while (candleCount > 0)
        {
            if (!GetPrevCandle(last, out CryptoCandle prev))
            {
                response = "No prev candle";
                return false;
            }

            if (last.CandleData.KeltnerCenterLineSlope > 0)
            {
                wrongCount++;
                if (first || wrongCount > allowedWrongCount)
                {
                    response = $"No negative keltner period ({first} {wrongCount}/{allowedWrongCount})";
                    return false;
                }
            }

            last = prev;
            candleCount--;
            first = false;
        }

        response = "";
        return true;
    }

    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        //if (!BarometerHelper.CheckValidBarometer(Symbol.QuoteData, CryptoIntervalPeriod.interval1h, (0.5m, decimal.MaxValue), out string reaction))
        //{
        //    response = reaction;
        //    return false;
        //}

        if (!CheckKeltnerSlopeInTheLastPeriod(10, 5, out response))
        {
            //response = "No negative keltner period";
            return false;
        }

        if (HadStobbInThelastXCandles(SignalSide, 5, 30) == null)
        {
            response = "No previous STOBB/SBM";
            return false;
        }


        response = "";
        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        if (CandleLast.CandleData.KeltnerCenterLineSlope < 0)
            return false;

        if (!GetPrevCandle(CandleLast, out CryptoCandle prevCandle))
            return false;

        if (prevCandle.CandleData.KeltnerCenterLineSlope > 0)
            return false;

        return true;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Na de initiele melding hebben we 3 candles de tijd om in te stappen, maar alleen indien de MACD verbetering laat zien.

        // Er een candle onder de bb opent of sluit
        //if (CandleLast.IsBelowBollingerBands(GlobalData.Settings.Signal.Sbm.UseLowHigh))
        //{
        //    ExtraText = "beneden de bb";
        //    return false;
        //}

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

        //if ((CandleLast.CandleData.Rsi < 55))
        //{
        //    ExtraText = string.Format("De RSI niet herstellend {0:N8} {1:N8} (last.2)", CandleLast.CandleData.Rsi, CandleLast.CandleData.Rsi);
        //    return false;
        //}
        //if ((CandleLast.CandleData.StochOscillator < 60))
        //{
        //    ExtraText = string.Format("De Stoch.K is niet hoog genoeg {0:N8}", CandleLast.CandleData.StochOscillator);
        //    return false;
        //}
        //if ((CandleLast.CandleData.StochSignal < 60))
        //{
        //    ExtraText = string.Format("De Stoch.D is niet hoog genoeg {0:N8}", CandleLast.CandleData.StochSignal);
        //    return false;
        //}

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
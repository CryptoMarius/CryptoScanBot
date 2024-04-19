using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Signal.Slope;

#if EXTRASTRATEGIESSLOPEEMA
public class SignalSlopeEma50Short : SignalCreateBase
{
    public SignalSlopeEma50Short(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.SlopeEma50;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.SlopeEma50 == null)
           || (candle.CandleData.Rsi == null)
           )
            return false;

        return true;
    }


    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        //if (!BarometerHelper.CheckValidBarometer(Symbol.QuoteData, CryptoIntervalPeriod.interval1h, (decimal.MinValue, -0.5m), out string reaction))
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

        if (CandleLast.CandleData.SlopeEma50 > 0)
            return false;

        if (!GetPrevCandle(CandleLast, out CryptoCandle prevCandle))
            return false;

        if (prevCandle.CandleData.SlopeEma50 < 0)
            return false;

        //decimal perc = 100m * (Symbol.LastPrice.Value - lastSignal.Price) / lastSignal.Price;
        //GlobalData.AddTextToLogTab($"{Symbol.Name} Slope EMA50 negative perc={perc:N2} last signal={lastSignal.Price:N8} lastprice={Symbol.LastPrice.Value:N8}");
        //if (perc <= 2.0m)
        //{
        //    return false;
        //}

        //ExtraText = perc.ToString("N2");
        return true;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Na de initiele melding hebben we 3 candles de tijd om in te stappen, maar alleen indien de MACD verbetering laat zien.

        // Er een candle onder de bb opent of sluit
        //if (CandleLast.IsAboveBollingerBands(GlobalData.Settings.Signal.Sbm.UseLowHigh))
        //{
        //    ExtraText = "Boven de bb";
        //    return false;
        //}

        // De markt is nog niet echt negatief
        //if (CandleLast.CandleData.Ema50 <= CandleLast.CandleData.Ema50)
        //    return false;


        if ((CandleLast.CandleData.Rsi > 45))
        {
            ExtraText = string.Format("De RSI niet herstellend {0:N8} {1:N8} (last.2)", CandleLast.CandleData.Rsi, CandleLast.CandleData.Rsi);
            return false;
        }
        if ((CandleLast.CandleData.StochOscillator > 40))
        {
            ExtraText = string.Format("De Stoch.K is niet laag genoeg {0:N8}", CandleLast.CandleData.StochOscillator);
            return false;
        }
        if ((CandleLast.CandleData.StochSignal > 40))
        {
            ExtraText = string.Format("De Stoch.D is niet laag genoeg {0:N8}", CandleLast.CandleData.StochSignal);
            return false;
        }

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
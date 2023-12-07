using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

#if EXTRASTRATEGIES
public class SignalSlopeSma50TurningPositive : SignalCreateBase
{
    public SignalSlopeSma50TurningPositive(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.SlopeSma50;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.Sma20 == null)
           || (candle.CandleData.Sma50 == null)
           || (candle.CandleData.Sma200 == null)
           || (candle.CandleData.SlopeSma20 == null)
           || (candle.CandleData.SlopeSma50 == null)
           )
            return false;

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        if (CandleLast.CandleData.SlopeSma50 < 0)
            return false;

        if (CandleLast.CandleData.Sma50 > CandleLast.CandleData.Sma200)
            return false;

        if (!GetPrevCandle(CandleLast, out CryptoCandle prevCandle))
            return false;

        if (!IndicatorsOkay(prevCandle))
            return false;

        if (prevCandle.CandleData.SlopeSma50 > 0)
            return false;

        if (!BarometersOkay((0.5m, decimal.MaxValue)))
        {
            ExtraText = "barometer te laag";
            return false;
        }

        // Is er een mogelijke LL in de voorgaande 60 candles?
        DateTime boundary = CandleLast.Date.AddSeconds(- Interval.Duration * 60);
        if (!GlobalData.IsStobSignalAvailableInTheLast(CryptoTradeSide.Long, boundary))
        {
            ExtraText = "Geen voorgaande STOB of SBM";
            return false;
        }

        return true;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        if (CandleLast.IsBelowBollingerBands(GlobalData.Settings.Signal.SbmUseLowHigh))
        {
            ExtraText = "beneden de bb";
            return false;
        }

        if (CandleLast.CandleData.Sma50 >= CandleLast.CandleData.Ema50)
            return false;


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

        // De markt is nog niet echt positief
        // (maar missen we meldingen hierdoor denk het wel!?)
        if (CandleLast.CandleData.Sma50 > CandleLast.CandleData.Ema50)
        {
            ExtraText = "SMA50 > EMA50";
            return true;
        }

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
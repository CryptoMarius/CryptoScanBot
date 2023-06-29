using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalBullishEngulfing : SignalCreateBase
{


    public SignalBullishEngulfing(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        ReplaceSignal = false;
        SignalMode = CryptoOrderSide.Buy;
        SignalStrategy = CryptoSignalStrategy.BullishEngulfing;
    }

    /// <summary>
    /// Is het een signaal?
    /// </summary>
    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
            || (candle.CandleData.Sma200 == null)
            || (candle.CandleData.Sma50 == null)
            || (candle.CandleData.BollingerBandsLowerBand == null)
            || (candle.CandleData.PSar == null)
            || (candle.CandleData.StochSignal == null)
            || (candle.CandleData.StochOscillator == null)
            )
            return false;

        return true;
    }


    public override string DisplayText()
    {
        return string.Format("ma200={0:N3} ma50={1:N3} ma20={2:N3} psar={3:N8} macd.h={4:N8}",
            CandleLast.CandleData.Sma200,
            CandleLast.CandleData.Sma50,
            CandleLast.CandleData.Sma20,
            CandleLast.CandleData.PSar,
            CandleLast.CandleData.MacdHistogram
        );
    }



    public override bool IsSignal()
    {
        ExtraText = "";

        // Nee, dit werkt ook niet echt.
        // Een beter idee zou zijn om op een bullish engulfing candle te gokken (mhh, waarom niet ;-))

        // The bullish engulfing pattern is a two - candle reversal pattern. The second candle completely
        // ‘engulfs’ the real body of the first one, without regard to the length of the tail shadows.
        //
        // This pattern appears in a downtrend and is a combination of one dark candle followed by a
        // larger hollow candle. On the second day of the pattern, the price opens lower than the
        // previous low, yet buying pressure pushes the price up to a higher level than the previous
        // high, culminating in an obvious win for the buyers.

        // What Does a Bullish Engulfing Pattern Tell You?
        // A bullish engulfing pattern is not to be interpreted as simply a white candlestick, representing
        // upward price movement, following a black candlestick, representing downward price movement. For
        // a bullish engulfing pattern to form, the stock must open at a lower price on Day 2 than it closed
        // at on Day 1.If the price did not gap down, the body of the white candlestick would not have a
        // chance to engulf the body of the previous day’s black candlestick.


        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StobbBBMinPercentage, 100)) //GlobalData.Settings.Signal.AnalysisBBMaxPercentage
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }


        // De laatste moet een groene candle zijn
        if (CandleLast.Close <= CandleLast.Open)
            return false;

        CryptoCandle CandlePrev1;
        if (!Candles.TryGetValue(CandleLast.OpenTime - Interval.Duration, out CandlePrev1))
        {
            ExtraText = "geen candleprev1!";
            return false;
        }

        // De voorlaatste candle moet rood zijn
        // Volgens de crypto Bible: De kleur van de candles doet er bij deze patronen niet toe, zeker de kleur van de eerste body niet
        //if (CandlePrev1.Close >= CandlePrev1.Open)
        //    return false;

        // We willen enkel engulfing in de onderste gedeelte van de bb (anders niet genoeg ruimte?)
        if (CandleLast.Close >= (decimal)CandleLast.CandleData.Sma20)
            return false;


        // De laatste candle moet de vorige volledig overschaduwen
        decimal widthPrev = Math.Abs(CandlePrev1.Open - CandlePrev1.Close);
        decimal widthLast = Math.Abs(CandleLast.Close - CandleLast.Open);
        if (widthPrev * 1.50m > widthLast)
            return false;

        // Er moet in ieder geval strength in de candle zitten (zeker geen platte candles), dus aanname 1/10 van de bb..
        if (widthPrev * 10m < ((decimal)CandleLast.CandleData.BollingerBandsUpperBand - (decimal)CandleLast.CandleData.BollingerBandsLowerBand))
            return false;
        if (widthLast * 10m < ((decimal)CandleLast.CandleData.BollingerBandsUpperBand - (decimal)CandleLast.CandleData.BollingerBandsLowerBand))
            return false;

        return true;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        //return true;
        // Langer dan 60 candles willen we niet wachten (is 60 niet heel erg lang?)
        if (CandleLast.OpenTime - signal.EventTime > 3 * Interval.Duration)
        {
            ExtraText = "Ophouden na 3 candles";
            return true;
        }

        ExtraText = "";
        return false;
    }

}


using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalPriceCrossedSma20 : SignalBase
{
    public SignalPriceCrossedSma20(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = SignalMode.modeLong;
        SignalStrategy = SignalStrategy.priceCrossedSma20;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.Rsi == null)
           || (candle.CandleData.Sma20 == null)
           )
            return false;

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste X%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StobbBBMinPercentage, GlobalData.Settings.Signal.StobbBBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        // Er is een goed opwaarts momentum
        if (CandleLast.CandleData.Rsi <= 60)
            return false;

        if (CandleLast.CandleData.StochSignal <= 70)
            return false;

        if (CandleLast.CandleData.StochOscillator <= 70)
            return false;

        CryptoCandle prevCandle;
        if (!Candles.TryGetValue(CandleLast.OpenTime - Interval.Duration, out prevCandle))
        {
            ExtraText = "geen prev candle! " + CandleLast.DateLocal.ToString();
            return false;
        }

        // De vorige candle mag de ema niet gekruist hebben
        if ((prevCandle.Open > (decimal)prevCandle.CandleData.Sma20) || (prevCandle.Close > (decimal)CandleLast.CandleData.Sma20))
            return false;
        //if (prevCandle.CandleData.Tema > prevCandle.CandleData.Sma20)
        //    return false;


        // De laatste candle moet de ema opwaarts kruisen
        if ((CandleLast.Open > (decimal)CandleLast.CandleData.Sma20) || (CandleLast.Close < (decimal)CandleLast.CandleData.Sma20))
            return false;
        //if (CandleLast.CandleData.Tema < CandleLast.CandleData.Sma20)
        //    return false;

        if (!WasRsiOversoldInTheLast(20))
            return false;


        decimal tickPercentage = 100m * Symbol.PriceTickSize / (decimal)Symbol.LastPrice;
        if (tickPercentage > GlobalData.Settings.Signal.MinimumTickPercentage)
            return false;

        return true;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        if ((CandleLast.CandleData.Rsi < 50))
        {
            ExtraText = string.Format("De RSI is niet > 50 {0:N8} (last)", CandleLast.CandleData.Rsi);
            return false;
        }


        CryptoCandle CandlePrev1;
        if (!Candles.TryGetValue(CandleLast.OpenTime - Interval.Duration, out CandlePrev1))
        {
            ExtraText = "No prev1";
            return false;
        }

        // Herstel? Verbeterd de RSI
        if ((CandleLast.CandleData.Rsi < CandlePrev1.CandleData.Rsi))
        {
            ExtraText = string.Format("De RSI niet herstellend {0:N8} {1:N8} (last)", CandlePrev1.CandleData.Rsi, CandleLast.CandleData.Rsi);
            return false;
        }

        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        ExtraText = "";
        int value = 5;
        //Langer dan 60 candles willen we niet wachten(is 60 niet heel erg lang ?)
        if ((CandleLast.OpenTime - signal.EventTime) / Interval.Duration > value)
        {
            ExtraText = "Ophouden na " + value.ToString() + " candles";
            return true;
        }

        return false;
    }


}

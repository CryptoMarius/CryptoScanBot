using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalPriceCrossedSma50 : SignalBase
{
    public SignalPriceCrossedSma50(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = SignalMode.modeLong;
        SignalStrategy = SignalStrategy.strategyPriceCrossedSma50;
    }

    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StobbBBMinPercentage, GlobalData.Settings.Signal.StobbBBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage.ToString("N2");
            return false;
        }

        if (!Candles.TryGetValue(CandleLast.OpenTime - 1 * Interval.Duration, out CryptoCandle prevCandle))
        {
            ExtraText = "geen prev candle! " + CandleLast.DateLocal.ToString();
            return false;
        }

        if (CandleLast.CandleData.Rsi.Rsi.Value <= 50)
            return false;

        if (CandleLast.CandleData.Stoch.Oscillator.Value <= 50)
            return false;

        // Kruising van de candle
        if (prevCandle.Close > (decimal)prevCandle.CandleData.Sma50.Sma.Value)
            return false;

        if (CandleLast.Close < (decimal)CandleLast.CandleData.Sma50.Sma.Value)
            return false;

        if (CandleLast.CandleData.Sma50.Sma.Value > CandleLast.CandleData.Sma200.Sma.Value)
            return false;


        decimal tickPercentage = 100m * Symbol.PriceTickSize / (decimal)Symbol.LastPrice;
        if (tickPercentage > GlobalData.Settings.Signal.MinimumTickPercentage)
            return false;

        return true;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        return false;
    }


}

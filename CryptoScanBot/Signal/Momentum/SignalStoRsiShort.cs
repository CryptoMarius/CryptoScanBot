using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Signal.Momentum;


// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class SignalStoRsiShort : SignalSbmBaseShort
{
    public SignalStoRsiShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.StoRsi;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Rsi == null
           || candle.CandleData.StochSignal == null
           || candle.CandleData.StochOscillator == null
           || candle.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    public override string DisplayText()
    {
        return string.Format("stoch.oscillator={0:N8} stoch.signal={1:N8}",
            CandleLast.CandleData.StochOscillator,
            CandleLast.CandleData.StochSignal
        );
    }


    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        response = "";
        return true;
    }


    public override bool IsSignal()
    {
        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        //if (!IsInUpperPartOfBollingerBands(2, 2.5m))
        //{
        //    ExtraText = "geen hoge prijs in de laatste x candles";
        //    return false;
        //}

        // Sprake van een overbought situatie
        if (!CandleLast.IsStochOverbought())
        {
            ExtraText = "stoch niet overbought";
            return false;
        }

        // Sprake van een overbought situatie
        if (!CandleLast.IsRsiOverbought())
        {
            ExtraText = "rsi niet overbought";
            return false;
        }


        ExtraText = "";
        return true;
    }


}

using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Momentum;

public class SignalSbmBase(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : SignalCreateBase(symbol, interval, candle)
{
    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Sma20 == null
           || candle.CandleData.Sma50 == null
           || candle.CandleData.Sma200 == null
           || candle.CandleData.PSar == null
           || candle.CandleData.StochSignal == null
           || candle.CandleData.StochOscillator == null
           || candle.CandleData.BollingerBandsDeviation == null
           )
        {
            ExtraText = "indicators not ok!";
            return false;
        }

        return true;
    }
}


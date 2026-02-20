using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Momentum;

public class SignalSbmBase(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : SignalCreateBase(symbol, interval, candle)
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Sma20 == null
           || data.CandleData.Sma50 == null
           || data.CandleData.Sma200 == null
           || data.CandleData.PSar == null
           || data.CandleData.StochSignal == null
           || data.CandleData.StochOscillator == null
           || data.CandleData.BollingerBandsDeviation == null
           )
        {
            ExtraText = "indicators not ok!";
            return false;
        }

        return true;
    }
}


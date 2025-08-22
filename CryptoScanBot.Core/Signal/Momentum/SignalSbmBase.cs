using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

public class SignalSbmBase(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : SignalCreateBase(symbol, interval, candle)
{

    public override string DisplayText()
    {
        return string.Format("ma200={0:N8} ma50={1:N8} ma20={2:N8} psar={3:N8} macd.h={4:N8} bb%={5:N2} rsi=={6:N2}",
            CandleLast.CandleData!.Sma200,
            CandleLast.CandleData.Sma50,
            CandleLast.CandleData.Sma20,
            CandleLast.CandleData.PSar,
            CandleLast.CandleData.MacdHistogram,
            CandleLast.CandleData.BollingerBandsPercentage,
            CandleLast.CandleData.Rsi
        );
    }




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


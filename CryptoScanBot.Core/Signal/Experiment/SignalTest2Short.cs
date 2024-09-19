using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal.Momentum;

namespace CryptoScanBot.Core.Signal.Experiment;

// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class SignalTest2Short: SignalSbmBaseShort
{
    public SignalTest2Short(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.Test2;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Rsi == null
           || candle.CandleData.PSar == null
           || candle.CandleData.Tema == null
           || candle.CandleData.Wma30 == null
           || candle.CandleData.MacdSignal == null
           || candle.CandleData.StochSignal == null
           || candle.CandleData.StochOscillator == null
           || candle.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }

    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        // disable sbm conditions
        response = "";
        return true;
    }

    public override bool IsSignal()
    {
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = "bb.width too small " + CandleLast.CandleData!.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        // psar below price (strage condition)
        if (CandleLast.CandleData!.PSar > (double)CandleLast.Close)
        {
            // need previous
            if (GetPrevCandle(CandleLast!, out CryptoCandle? candlePrev))
            {
                // tema crossing the wma30 upwards
                if (candlePrev!.CandleData!.Tema > candlePrev.CandleData!.Wma30 && CandleLast.CandleData!.Tema < CandleLast.CandleData!.Wma30)
                {
                    // macd blue above macd red and green macd candles
                    if (CandleLast.CandleData!.MacdValue < CandleLast.CandleData!.MacdSignal && CandleLast.CandleData!.MacdHistogram < 0)
                    {
                        // there was a drop in the period before this
                        if (IsInUpperPartOfBollingerBands(45, 5.0m) != null)
                        {
                            return true;
                        }
                    }
                }
            }
        }


        ExtraText = "";
        return false;
    }


}

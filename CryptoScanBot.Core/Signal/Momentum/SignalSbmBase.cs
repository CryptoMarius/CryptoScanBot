using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

public class SignalSbmBase(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : SignalCreateBase(account, symbol, interval, candle)
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


    /// <summary>
    /// Als de ma200 en ma50 elkaar gekruist of geraakt hebben dan is het een nogo
    /// Er geen crosover is geweest van de 200 en 50 in de laatste x candles.
    /// </summary>
    public bool HasCrossed200and50(int candleCount, out int candlesAgo)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        candlesAgo = 0;
        long time = CandleLast.OpenTime;
        //DateTime TimeDebug = CandleTools.GetUnixDate(CandleLast.OpenTime);
        CryptoCandle? prevCandle = null;
        while (candleCount >= 0)
        {
            if (Candles.TryGetValue(time, out CryptoCandle? lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 200 naar boven
                        if (prevCandle.CandleData!.Sma50 < prevCandle.CandleData.Sma200 &&
                                lastCandle.CandleData!.Sma50 >= lastCandle.CandleData.Sma200)
                            return true;
                        // de 50 kruist de 200 naar beneden
                        if (prevCandle.CandleData!.Sma50 > prevCandle.CandleData.Sma200 &&
                                lastCandle.CandleData!.Sma50 <= lastCandle.CandleData.Sma200)
                            return true;
                    }
                }
            }

            candlesAgo++;
            candleCount--;
            prevCandle = lastCandle;
            time -= Interval.Duration;
        }
        return false;
    }


    /// <summary>
    /// Als de ma200 en ma20 elkaar gekruist of geraakt hebben dan is het een nogo
    /// Er geen crosover is geweest van de 200 en 50 in de laatste x candles.
    /// </summary>
    public bool HasCrossed200and20(int candleCount, out int candlesAgo)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        candlesAgo = 0;
        long time = CandleLast.OpenTime;
        //DateTime TimeDebug = CandleTools.GetUnixDate(CandleLast.OpenTime);
        CryptoCandle? prevCandle = null;
        while (candleCount >= 0)
        {
            if (Candles.TryGetValue(time, out CryptoCandle? lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 200 naar boven
                        if (prevCandle.CandleData!.Sma20 < prevCandle.CandleData.Sma200 &&
                                lastCandle.CandleData!.Sma20 >= lastCandle.CandleData.Sma200)
                            return true;
                        // de 50 kruist de 200 naar beneden
                        if (prevCandle.CandleData!.Sma20 > prevCandle.CandleData.Sma200 &&
                                lastCandle.CandleData!.Sma20 <= lastCandle.CandleData.Sma200)
                            return true;
                    }
                }
            }
            candlesAgo++;
            candleCount--;
            prevCandle = lastCandle;
            time -= Interval.Duration;
        }
        return false;
    }


    /// <summary>
    /// Als de ma200 en ma50 elkaar gekruist of geraakt hebben dan is het een nogo
    /// Er geen crosover is geweest van de 200 en 50 in de laatste x candles.
    /// </summary>
    public bool HasCrossed50and20(int candleCount, out int candlesAgo)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        candlesAgo = 0;
        long time = CandleLast.OpenTime;
        //DateTime TimeDebug = CandleTools.GetUnixDate(CandleLast.OpenTime);
        CryptoCandle? prevCandle = null;
        while (candleCount >= 0)
        {
            if (Candles.TryGetValue(time, out CryptoCandle? lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 20 naar boven
                        if (prevCandle.CandleData!.Sma50 < prevCandle.CandleData.Sma20 &&
                                lastCandle.CandleData!.Sma50 >= lastCandle.CandleData.Sma20)
                            return true;

                        // de 50 kruist de 20 naar beneden
                        if (prevCandle.CandleData!.Sma50 > prevCandle.CandleData.Sma20 &&
                                lastCandle.CandleData!.Sma50 <= lastCandle.CandleData.Sma20)
                            return true;
                    }
                }
            }

            candlesAgo++;
            candleCount--;
            prevCandle = lastCandle;
            time -= Interval.Duration;
        }
        return false;
    }


    public bool CheckMaCrossings(out string response)
    {
        if (GlobalData.Settings.Signal.Sbm.Ma200AndMa20Crossing && HasCrossed200and20(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Lookback, out int candlesAgo))
        {
            response = string.Format("ma200 and ma20 gekruist ({0} candles)", candlesAgo);
            //GlobalData.AddTextToLogTab(Symbol.Name + " " + Interval.Name + " " + response);
            return false;
        }
        if (GlobalData.Settings.Signal.Sbm.Ma200AndMa50Crossing && HasCrossed200and50(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Lookback, out candlesAgo))
        {
            response = string.Format("ma200 en ma50 gekruist ({0} candles)", candlesAgo);
            //GlobalData.AddTextToLogTab(Symbol.Name + " " + Interval.Name + " " + response);
            return false;
        }
        if (GlobalData.Settings.Signal.Sbm.Ma50AndMa20Crossing && HasCrossed50and20(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Lookback, out candlesAgo))
        {
            response = string.Format("ma50 and ma20 gekruist ({0} candles)", candlesAgo);
            //GlobalData.AddTextToLogTab(Symbol.Name + " " + Interval.Name + " " + response);
            return false;
        }

        response = "";
        return true;
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


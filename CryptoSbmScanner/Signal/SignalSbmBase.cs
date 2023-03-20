using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;


// De officiele SBM methode van Maurice Orsel

public class SignalSbmBase : SignalBase
{
    public SignalSbmBase(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }

#if DEBUG
    private static string Toelichting(CryptoCandle candle)
    {
        string text = "";

        if (candle == null)
        {
            text += "candle==null";
            return text;
        }

        if (candle.CandleData == null)
        {
            text += "geen candledata";
            return text;
        }

        if (candle.CandleData.PSar == 0)
            text += "geen candledata.psar";
        if (candle.CandleData.Stoch == null)
            text += "geen candledata.stoch";
        if (candle.CandleData.Stoch.Signal == null)
            text += "geen candledata.stoch.signal";
        if (candle.CandleData.Stoch.Oscillator == null)
            text += "geen candledata.stoch.Oscillator";

        if (candle.CandleData.Sma50.Sma == null)
            text += "geen candledata.sma50.sma";
        if (!candle.CandleData.Sma50.Sma.HasValue)
            text += "geen candledata.sma50.sma.value";
        if (candle.CandleData.Sma200.Sma == null)
            text += "geen candledata.sma200.sma";
        if (!candle.CandleData.Sma200.Sma.HasValue)
            text += "geen candledata.sma200.sma.value";

        if (candle.CandleData.BollingerBands == null)
            text += "geen candledata.BollingerBands";
        if (!candle.CandleData.BollingerBands.Sma.HasValue)
            text += "geen candledata.BollingerBands.Sma.Value";

        return text;
    }
#endif


    /// <summary>
    /// Als de ma200 en ma50 elkaar gekruist of geraakt hebben dan is het een nogo
    /// Er geen crosover is geweest van de 200 en 50 in de laatste x candles.
    /// </summary>
    public bool HasCrossed200and50(int candleCount = 30)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        long time = CandleLast.OpenTime;
        //DateTime TimeDebug = CandleTools.GetUnixDate(CandleLast.OpenTime);
        CryptoCandle prevCandle = null;
        while (candleCount >= 0)
        {
            if (Candles.TryGetValue(time, out CryptoCandle lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 200 naar boven
                        if (prevCandle.CandleData.Sma50.Sma.Value < prevCandle.CandleData.Sma200.Sma.Value &&
                                lastCandle.CandleData.Sma50.Sma.Value >= lastCandle.CandleData.Sma200.Sma.Value)
                            return true;
                        // de 50 kruist de 200 naar beneden
                        if (prevCandle.CandleData.Sma50.Sma.Value > prevCandle.CandleData.Sma200.Sma.Value &&
                                lastCandle.CandleData.Sma50.Sma.Value <= lastCandle.CandleData.Sma200.Sma.Value)
                            return true;
                    }
#if DEBUG
                    else
                    {
                        // toelichting geven?
                        GlobalData.AddTextToLogTab(lastCandle.DateLocal.ToString() + " " + Symbol.Name + " " + Interval.Name + " ma200-50, geen candledata! " +
                            Candles.Count + " " + candleCount + " prevcandle= " + Toelichting(prevCandle) + " lastcandle=" + Toelichting(lastCandle));
                    }
#endif
                }
            }
#if DEBUG
            else GlobalData.AddTextToLogTab(lastCandle.DateLocal.ToString() + " " + Symbol.Name + " " + Interval.Name + " ma200-50, geen candle! " + Candles.Count);
#endif

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
    public bool HasCrossed200and20(int candleCount = 30)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        long time = CandleLast.OpenTime;
        //DateTime TimeDebug = CandleTools.GetUnixDate(CandleLast.OpenTime);
        CryptoCandle prevCandle = null;
        while (candleCount >= 0)
        {
            if (Candles.TryGetValue(time, out CryptoCandle lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 200 naar boven
                        if (prevCandle.CandleData.BollingerBands.Sma.Value < prevCandle.CandleData.Sma200.Sma.Value &&
                                lastCandle.CandleData.BollingerBands.Sma.Value >= lastCandle.CandleData.Sma200.Sma.Value)
                            return true;
                        // de 50 kruist de 200 naar beneden
                        if (prevCandle.CandleData.BollingerBands.Sma.Value > prevCandle.CandleData.Sma200.Sma.Value &&
                                lastCandle.CandleData.BollingerBands.Sma.Value <= lastCandle.CandleData.Sma200.Sma.Value)
                            return true;
                    }
#if DEBUG
                    else
                    {
                        // toelichting geven?
                        GlobalData.AddTextToLogTab(lastCandle.DateLocal.ToString() + " " + Symbol.Name + " " + Interval.Name + " ma200-20, geen candledata! " +
                        Candles.Count + " " + candleCount + " prevcandle= " + Toelichting(prevCandle) + " lastcandle=" + Toelichting(lastCandle));
                    }
#endif
                }
            }
#if DEBUG
            else GlobalData.AddTextToLogTab(lastCandle.DateLocal.ToString() + " " + Symbol.Name + " " + Interval.Name + " ma200-20, geen candle! " + Candles.Count);
#endif
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
    public bool HasCrossed50and20(int candleCount = 30)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        long time = CandleLast.OpenTime;
        //DateTime TimeDebug = CandleTools.GetUnixDate(CandleLast.OpenTime);
        CryptoCandle prevCandle = null;
        while (candleCount >= 0)
        {
            if (Candles.TryGetValue(time, out CryptoCandle lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 20 naar boven
                        if (prevCandle.CandleData.Sma50.Sma.Value < prevCandle.CandleData.BollingerBands.Sma.Value &&
                                lastCandle.CandleData.Sma50.Sma.Value >= lastCandle.CandleData.BollingerBands.Sma.Value)
                            return true;

                        // de 50 kruist de 20 naar beneden
                        if (prevCandle.CandleData.Sma50.Sma.Value > prevCandle.CandleData.BollingerBands.Sma.Value &&
                                lastCandle.CandleData.Sma50.Sma.Value <= lastCandle.CandleData.BollingerBands.Sma.Value)
                            return true;
                    }
#if DEBUG
                    else
                    {
                        // toelichting geven?
                        GlobalData.AddTextToLogTab(lastCandle.DateLocal.ToString() + " " + Symbol.Name + " " + Interval.Name + " ma50-20, geen candledata! " +
                        Candles.Count + " " + candleCount + " prevcandle= " + Toelichting(prevCandle) + " lastcandle=" + Toelichting(lastCandle));
                    }
#endif
                }
            }
#if DEBUG
            else GlobalData.AddTextToLogTab(lastCandle.DateLocal.ToString() + " " + Symbol.Name + " " + Interval.Name + " ma50-20, geen candle! " + Candles.Count);
#endif

            candleCount--;
            prevCandle = lastCandle;
            time -= Interval.Duration;
        }
        return false;
    }


    public bool CheckMaCrossings()
    {
        if (GlobalData.Settings.Signal.SbmMa200AndMa20Crossing && HasCrossed200and20(GlobalData.Settings.Signal.SbmMa200AndMa20Lookback))
        {
            ExtraText = "ma200 en ma20 gekruist";
            GlobalData.AddTextToLogTab(Symbol.Name + " " + Interval.Name + " " + ExtraText);
            return true;
        }
        if (GlobalData.Settings.Signal.SbmMa200AndMa50Crossing && HasCrossed200and50(GlobalData.Settings.Signal.SbmMa200AndMa50Lookback))
        {
            ExtraText = "ma200 en ma50 gekruist";
            GlobalData.AddTextToLogTab(Symbol.Name + " " + Interval.Name + " " + ExtraText);
            return true;
        }
        if (GlobalData.Settings.Signal.SbmMa50AndMa20Crossing && HasCrossed50and20(GlobalData.Settings.Signal.SbmMa50AndMa20Lookback))
        {
            ExtraText = "ma50 en ma20 gekruist";
            GlobalData.AddTextToLogTab(Symbol.Name + " " + Interval.Name + " " + ExtraText);
            return true;
        }

        return false;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.PSar == 0
           || candle.CandleData.Stoch == null
           || candle.CandleData.Stoch.Signal == null
           || candle.CandleData.Stoch.Oscillator == null
           || candle.CandleData.Sma50.Sma == null || !candle.CandleData.Sma50.Sma.HasValue
           || candle.CandleData.Sma200.Sma == null || !candle.CandleData.Sma200.Sma.HasValue
           || candle.CandleData.BollingerBands == null || !candle.CandleData.BollingerBands.Sma.HasValue
           )
        {
            ExtraText = "indicators not ok!";
            return false;
        }

        return true;
    }


    public override string DisplayText()
    {
        decimal value = -999m;

        return string.Format("ma200={0:N8} ma50={1:N8} ma20={2:N8} psar={3:N8} macd.h={4:N8} bm={5:N2} bb%={6:N2}",
            CandleLast.CandleData.Sma200.Sma.Value,
            CandleLast.CandleData.Sma50.Sma.Value,
            CandleLast.CandleData.BollingerBands.Sma.Value,
            CandleLast.CandleData.PSar,
            CandleLast.CandleData.Macd.Histogram.Value,
            value,
            CandleLast.CandleData.BollingerBandsPercentage
        );
    }


    public bool IsMacdRecoveryOversold(int candleCount = 2)
    {
        // We stappen in op het moment dat er herstel is, zijn er 2 macd candles roze?
        // Die negatieve waarden van de macd zijn killing voor de gemaakte vergelijking (dat klopt namelijk niet als een van beide positief wordt)
        //decimal value = (decimal)CandleLast.CandleData.Macd.Histogram.Value - (decimal)CandlePrev1.CandleData.Macd.Histogram.Value;
        int iterator = 0;
        CryptoCandle last = CandleLast;
        while (candleCount > 0)
        {
            if (last.CandleData.Macd.Histogram.Value >= 0)
            {
                ExtraText = $"De MACD[{iterator:N0}].Hist is ondertussen groen {iterator:N8}";
                return false;
            }
            iterator--;

            if (!Candles.TryGetValue(last.OpenTime - 1 * Interval.Duration, out CryptoCandle prev))
            {
                ExtraText = string.Format("No MACD[{0:N0}]", iterator);
                return false;
            }
            if (!IndicatorsOkay(prev))
                return false;

            if (last.CandleData.Macd.Histogram.Value <= prev.CandleData.Macd.Histogram.Value)
            {
                ExtraText = string.Format("De MACD[{0:N0}].Hist is niet roze {1:N8} {2:N8} (last)", iterator, prev.CandleData.Macd.Histogram.Value, last.CandleData.Macd.Histogram.Value);
                return false;
            }

            last = prev;
            candleCount--;
        }

        return true;
    }

    public bool IsMacdRecoveryOverbought(int candleCount = 2)
    {
        // Is er herstel ten opzichte van de vorige macd histogram candle?
        int iterator = 0;
        CryptoCandle last = CandleLast;
        while (candleCount > 0)
        {
            if (last.CandleData.Macd.Histogram.Value <= 0)
            {
                ExtraText = $"De MACD[{iterator:N0}].Hist is ondertussen rood {iterator:N8}";
                return false;
            }
            iterator--;

            if (!Candles.TryGetValue(last.OpenTime - 1 * Interval.Duration, out CryptoCandle prev))
            {
                ExtraText = string.Format("No MACD[{0:N0}]", iterator);
                return false;
            }
            if (!IndicatorsOkay(prev))
                return false;

            // Een groene is ook goed (zolang ie maar niet lager wordt)
            if (last.CandleData.Macd.Histogram.Value >= prev.CandleData.Macd.Histogram.Value)
            {
                ExtraText = string.Format("De MACD[{0:N0}].Hist is niet lichtgroen {1:N8} {2:N8} (last)", iterator, prev.CandleData.Macd.Histogram.Value, last.CandleData.Macd.Histogram.Value);
                return false;
            }

            last = prev;
            candleCount--;
        }

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

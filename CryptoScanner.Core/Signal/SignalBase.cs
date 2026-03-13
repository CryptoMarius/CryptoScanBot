using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal;


// Het draait allemaal om de status van het algoritme
// (het algoritme zet die status zelf alsmede delay enz.):
// -None, candle aanbieden voor signaal detectie
// -WarmingUp (voor de indicators)
// -Delaying: Een (optionele) delay
// -TryStepIn: Na een OK van het algoritme om in te stappen

public class MyData
{
    public required CryptoCandle Candle { get; set; }
    public required CryptoData CandleData { get; set; }
}

public class SignalCreateBase
{
    // RegisterAlgorithms.GetAlgorithm
    public required CryptoSymbol Symbol { get; set; }
    public required CryptoInterval Interval { get; set; }
    public required CryptoSymbolInterval SymbolInterval { get; set; }

    // The requested strategy and side
    public required CryptoTradeSide SignalSide { get; set; }
    public required CryptoSignalStrategy SignalStrategy { get; set; }

    // The requested candle and its indicator data (grouped)
    public required MyData CandleLast { get; set; }

    // Prepared indicator data
    public required CryptoIndicatorData IndicatorData { get; set; }
    public required CryptoIndicatorDataList IndicatorDataList { get; set; }

    public string ExtraText = "";

    /// <summary>
    /// Zijn de indicatoren aanwezig
    /// </summary>
    public virtual bool IndicatorsOkay(MyData data) => data.Candle.OpenTime != 0 && data.CandleData != null;

    /// <summary>
    /// Is het een signaal?
    /// </summary>
    public virtual bool IsSignal() => false;


    public virtual bool AdditionalChecks(MyData candle, out string response)
    {
        response = "";
        return true;
    }


    public virtual string DisplayText()
        => $"stoch={CandleLast.CandleData?.StochOscillator:N8} signal={CandleLast.CandleData?.StochSignal:N8}";


    /// <summary>
    /// Ophouden met positie nemen
    /// </summary>
    public virtual bool GiveUp(CryptoSignal signal)
    {
        ExtraText = "";
        return false;
    }


    /// <summary>
    /// Extra controles nadat we het accepteren
    /// </summary>
    public virtual bool AllowStepIn(CryptoSignal signal) => true;


    //// Get the candle and indicator data from the signal interval
    //internal bool TryGetCandle(CandleTime time, out MyData? myData)
    //{
    //    if (SymbolInterval.CandleList.TryGetValue(time, out CryptoCandle? candle) &&
    //        IndicatorData.Data.TryGetValue(time, out CandleIndicatorData? indicator))
    //    {
    //        myData = new()
    //        {
    //            Candle = candle!,
    //            CandleData = indicator!
    //        };
    //        return true;
    //    }
    //    else
    //    {
    //        myData = null;
    //        return false;
    //    }
    //}

    //// Get the candle and indicator data from a DIFFERENT interval
    //internal bool TryGetCandle(CryptoInterval interval, CandleTime time, out MyData? myData)
    //{
    //    var symbolInterval = Symbol.GetSymbolInterval(interval.IntervalPeriod);
    //    if (symbolInterval.CandleList.TryGetValue(time, out CryptoCandle? candle) &&
    //        IndicatorDataList.TryGetValue(interval.IntervalPeriod, out CryptoIndicatorData? indicatorData) &&
    //        indicatorData!.Data.TryGetValue(time, out CandleIndicatorData? indicator))
    //    {
    //        myData = new()
    //        {
    //            Candle = candle!,
    //            CandleData = indicator!
    //        };
    //        return true;
    //    }
    //    else
    //    {
    //        myData = null;
    //        return false;
    //    }
    //}


    // Get the candle and indicator data from the signal interval
    public bool GetPrevCandle(MyData? oldCandle, out MyData? newCandle)
    {
        if (oldCandle == null)
        {
            newCandle = null;
            return false;
        }

        CandleTime targetTime = oldCandle.Candle.OpenTime - Interval.Duration;
        if (!IndicatorData.TryGetCandle(targetTime, out newCandle))
        {
            ExtraText = $"No prev candle or data! {targetTime.ToDateTime().ToLocalTime()}";
            newCandle = null;
            return false;
        }


        if (!IndicatorsOkay(newCandle!))
        {
            ExtraText = $"Prev problem indicators! {targetTime.ToDateTime().ToLocalTime()}";
            return false;
        }

        return true;
    }

    // Get the candle and indicator data from a DIFFERENT interval
    public bool GetPrevCandle(CryptoInterval interval, MyData? oldData, out MyData? newData)
    {
        if (oldData == null)
        {
            newData = null;
            return false;
        }

        CandleTime targetTime = oldData.Candle.OpenTime - interval.Duration;
        if (!IndicatorDataList.TryGetCandle(interval, targetTime, out newData))
        {
            ExtraText = $"No prev candle or data! {targetTime.ToDateTime().ToLocalTime()}";
            newData = null;
            return false;
        }

        if (!IndicatorsOkay(newData!))
        {
            ExtraText = $"Prev problem indicators! {targetTime.ToDateTime().ToLocalTime()}";
            return false;
        }

        return true;
    }


    protected MyData? HadStobbInThelastXCandles(CryptoTradeSide side, int skipCandleCount, int candleCount)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        MyData? candle = CandleLast;
        while (candleCount > 0)
        {
            skipCandleCount--;
            bool isOverSold = candle is not null && candle.IsBelowBollingerBands(false) && candle.StochOversold();
            bool isOverBought = candle is not null && candle.IsAboveBollingerBands(false) && candle.StochOverbought();

            if (side == CryptoTradeSide.Long)
            {
                if (isOverBought) // Een short melding! Nee ongeldig!
                    return null;
                if (skipCandleCount < 0 && isOverSold)
                    return candle;
            }
            else
            {
                if (isOverSold) // Een long melding! Nee ongeldig!
                    return null;
                if (skipCandleCount < 0 && isOverBought)
                    return candle;
            }

            if (!GetPrevCandle(candle, out candle))
                return null;
            candleCount--;
        }

        return null;
    }



    protected MyData? HadStorsiInThelastXCandles(CryptoTradeSide side, int skipCandleCount, int candleCount, int correction = 0)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        MyData? candle = CandleLast;
        while (candleCount > 0)
        {
            skipCandleCount--; // GlobalData.Settings.Signal.StoRsi.AddRsiAmount
            bool isOverSold = candle is not null && candle.RsiOversold(correction) && candle.StochOversold();
            bool isOverBought = candle is not null && candle.RsiOverbought(correction) && candle.StochOverbought();

            if (side == CryptoTradeSide.Long)
            {
                if (isOverBought) // Een short melding! Nee ongeldig!
                    return null;
                if (skipCandleCount < 0 && isOverSold)
                    return candle;
            }
            else
            {
                if (isOverSold) // Een long melding! Nee ongeldig!
                    return null;
                if (skipCandleCount < 0 && isOverBought)
                    return candle;
            }

            if (!GetPrevCandle(candle, out candle))
                return null;
            candleCount--;
        }

        return null;
    }

    protected bool InLowerPartOfBollingerBands(int candleCount, decimal percentage)
    {
        // Was the price near the lower bb?

        MyData? last = CandleLast;
        while (candleCount-- > 0)
        {
            decimal band = (decimal)last!.CandleData?.BollingerBandsLowerBand!;
            band += (decimal)last!.CandleData?.BollingerBandsDeviation! * percentage / 100m;

            decimal value;
            if (GlobalData.Settings.Signal.Sbm.Sbm2UseLowHigh)
                value = last.Candle.Low;
            else
                value = Math.Max(last.Candle.Open, last.Candle.Close);

            if (value <= band)
                return true;

            if (!GetPrevCandle(last, out last))
                return false;
        }

        return false;
    }


    protected bool InUpperPartOfBollingerBands(int candleCount, decimal percentage)
    {
        // Was the price near the upper bb?

        MyData? last = CandleLast;
        while (candleCount > 0)
        {
            decimal band = (decimal)last!.CandleData?.BollingerBandsUpperBand!;
            band -= (decimal)last!.CandleData?.BollingerBandsDeviation! * percentage / 100m;

            decimal value;
            if (GlobalData.Settings.Signal.Sbm.Sbm2UseLowHigh)
                value = last.Candle.High;
            else
                value = Math.Max(last.Candle.Open, last.Candle.Close);

            if (value >= band)
                return true;

            if (!GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }

        return false;
    }


    /// <summary>
    /// Als de ma200 en ma50 elkaar gekruist of geraakt hebben dan is het een nogo
    /// Er geen crosover is geweest van de 200 en 50 in de laatste x candles.
    /// </summary>
    public bool HasCrossed200and50(int candleCount, out int candlesAgo)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        candlesAgo = 0;
        CandleTime time = CandleLast.Candle.OpenTime;
        MyData? prevCandle = null;
        while (candleCount >= 0)
        {
            if (IndicatorData.TryGetCandle(time, out MyData? lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle!) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 200 naar boven
                        if (prevCandle.CandleData!.Sma50 < prevCandle.CandleData.Sma200 &&
                                lastCandle!.CandleData!.Sma50 >= lastCandle.CandleData.Sma200)
                            return true;
                        // de 50 kruist de 200 naar beneden
                        if (prevCandle.CandleData!.Sma50 > prevCandle.CandleData.Sma200 &&
                                lastCandle!.CandleData!.Sma50 <= lastCandle.CandleData.Sma200)
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
        CandleTime time = CandleLast.Candle.OpenTime;
        MyData? prevCandle = null;
        while (candleCount >= 0)
        {
            if (IndicatorData.TryGetCandle(time, out MyData? lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle!) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 200 naar boven
                        if (prevCandle.CandleData!.Sma20 < prevCandle.CandleData.Sma200 &&
                                lastCandle!.CandleData!.Sma20 >= lastCandle.CandleData.Sma200)
                            return true;
                        // de 50 kruist de 200 naar beneden
                        if (prevCandle.CandleData!.Sma20 > prevCandle.CandleData.Sma200 &&
                                lastCandle!.CandleData!.Sma20 <= lastCandle.CandleData.Sma200)
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
        CandleTime time = CandleLast.Candle.OpenTime;
        MyData? prevCandle = null;
        while (candleCount >= 0)
        {
            if (IndicatorData.TryGetCandle(time, out MyData? lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle!) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 20 naar boven
                        if (prevCandle.CandleData!.Sma50 < prevCandle.CandleData.Sma20 &&
                                lastCandle!.CandleData!.Sma50 >= lastCandle.CandleData.Sma20)
                            return true;

                        // de 50 kruist de 20 naar beneden
                        if (prevCandle.CandleData!.Sma50 > prevCandle.CandleData.Sma20 &&
                                lastCandle!.CandleData!.Sma50 <= lastCandle.CandleData.Sma20)
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
            response = string.Format("ma200 and ma20 crossed ({0} candles)", candlesAgo);
            return false;
        }
        if (GlobalData.Settings.Signal.Sbm.Ma200AndMa50Crossing && HasCrossed200and50(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Lookback, out candlesAgo))
        {
            response = string.Format("ma200 and ma50 crossed ({0} candles)", candlesAgo);
            return false;
        }
        if (GlobalData.Settings.Signal.Sbm.Ma50AndMa20Crossing && HasCrossed50and20(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Lookback, out candlesAgo))
        {
            response = string.Format("ma50 and ma20 crossed ({0} candles)", candlesAgo);
            return false;
        }

        response = "";
        return true;
    }

}
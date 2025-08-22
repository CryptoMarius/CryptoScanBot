using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal;


// Het draait allemaal om de status van het algoritme
// (het algoritme zet die status zelf alsmede delay enz.):
// -None, candle aanbieden voor signaal detectie
// -WarmingUp (voor de indicators)
// -Delaying: Een (optionele) delay
// -TryStepIn: Na een OK van het algoritme om in te stappen

public class SignalCreateBase
{
    protected Model.CryptoExchange Exchange;
    protected CryptoSymbol Symbol;
    protected CryptoSymbolInterval SymbolInterval;
    protected CryptoInterval Interval;
    protected CryptoQuoteData QuoteData;
    protected CryptoCandleList Candles;

    public CryptoTradeSide SignalSide;
    public CryptoSignalStrategy SignalStrategy;
    public CryptoCandle CandleLast;
    public string ExtraText = "";

    public SignalCreateBase(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle)
    {
        Symbol = symbol;
        Exchange = symbol.Exchange!;
        Interval = interval;
        QuoteData = symbol.QuoteData!;
        CandleLast = candle;

        SymbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
        Candles = SymbolInterval.CandleList;
    }

    /// <summary>
    /// Zijn de indicatoren aanwezig
    /// </summary>
    public virtual bool IndicatorsOkay(CryptoCandle candle) => true;


    /// <summary>
    /// Is het een signaal?
    /// </summary>
    public virtual bool IsSignal() => false;


    public virtual bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        response = "";
        return true;
    }


    public virtual string DisplayText()
        => $"stoch={CandleLast?.CandleData?.StochOscillator:N8} signal={CandleLast?.CandleData?.StochSignal:N8}";


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


    public bool GetPrevCandle(CryptoCandle? oldCandle, out CryptoCandle? newCandle)
    {
        if (oldCandle == null)
        {
            newCandle = null;
            return false;
        }

        if (!Candles.TryGetValue(oldCandle.OpenTime - Interval.Duration, out newCandle))
        {
            ExtraText = "No prev candle! " + oldCandle.DateLocal.ToString();
            return false;
        }

        if (!IndicatorsOkay(newCandle))
        {
            ExtraText = "Prev problem indicators " + newCandle.DateLocal.ToString();
            return false;
        }

        return true;
    }




    protected CryptoCandle? HadStobbInThelastXCandles(CryptoTradeSide side, int skipCandleCount, int candleCount)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        CryptoCandle? candle = CandleLast;
        while (candleCount > 0)
        {
            skipCandleCount--;
            bool isOverSold = candle is not null && candle.IsBelowBollingerBands(false) && candle.StochOversold();
            bool isOverBought = candle is not null && candle.AboveBollingerBands(false) && candle.StochOverbought();

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



    protected CryptoCandle? HadStorsiInThelastXCandles(CryptoTradeSide side, int skipCandleCount, int candleCount)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        CryptoCandle? candle = CandleLast;
        while (candleCount > 0)
        {
            skipCandleCount--; // GlobalData.Settings.Signal.StoRsi.AddRsiAmount
            bool isOverSold = candle is not null && candle.RsiOversold(0) && candle.StochOversold();
            bool isOverBought = candle is not null && candle.RsiOverbought() && candle.StochOverbought();

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

        CryptoCandle? last = CandleLast;
        while (candleCount-- > 0)
        {
            decimal band = (decimal)last!.CandleData?.BollingerBandsLowerBand!;
            band += (decimal)last!.CandleData?.BollingerBandsDeviation! * percentage / 100m;

            decimal value;
            if (GlobalData.Settings.Signal.Sbm.Sbm2UseLowHigh)
                value = last.Low;
            else
                value = Math.Max(last.Open, last.Close);

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

        CryptoCandle? last = CandleLast;
        while (candleCount-- > 0)
        {
            decimal band = (decimal)last!.CandleData?.BollingerBandsUpperBand!;
            band -= (decimal)last!.CandleData?.BollingerBandsDeviation! * percentage / 100m;

            decimal value;
            if (GlobalData.Settings.Signal.Sbm.Sbm2UseLowHigh)
                value = last.High;
            else
                value = Math.Max(last.Open, last.Close);

            if (value >= band)
                return true;

            if (!GetPrevCandle(last, out last))
                return false;
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
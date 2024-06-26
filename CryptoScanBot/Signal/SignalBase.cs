﻿using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;
using CryptoScanBot.Signal.Momentum;

namespace CryptoScanBot.Signal;


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
    protected SortedList<long, CryptoCandle> Candles;

    public CryptoTradeSide SignalSide;
    public CryptoSignalStrategy SignalStrategy;
    public CryptoCandle CandleLast = null;
    public string ExtraText = "";
    public bool ReplaceSignal = true;

    public SignalCreateBase(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle)
    {
        Symbol = symbol;
        Exchange = symbol.Exchange;
        Interval = interval;
        QuoteData = symbol.QuoteData;
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
    public virtual bool IsSignal()
    {
        return false;
    }


    public virtual bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        response = "";
        return true;
    }


    public virtual string DisplayText()
    {
        return $"stoch={CandleLast.CandleData.StochOscillator.Value:N8} signal={CandleLast.CandleData.StochSignal.Value:N8}";
    }


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
    public virtual bool AllowStepIn(CryptoSignal signal)
    {
        return true;
    }


    public bool GetPrevCandle(CryptoCandle oldCandle, out CryptoCandle newCandle)
    {
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



    /// <summary>
    /// Is de RSI oversold geweest in de laatste x candles
    /// </summary>
    public bool WasRsiOversoldInTheLast(int candleCount = 30)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        CryptoCandle candle = CandleLast;
        while (candleCount >= 0)
        {
            if (candle.IsRsiOversold())
                return true;

            if (!GetPrevCandle(candle, out candle))
                return false;
            candleCount--;
        }
        return false;
    }


    public CryptoCandle HadStobbInThelastXCandles(CryptoTradeSide side, int skipCandleCount, int candleCount)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        CryptoCandle candle = CandleLast;
        while (candleCount > 0)
        {
            skipCandleCount--;
            bool isOverSold = candle.IsBelowBollingerBands(false) && candle.IsStochOversold();
            bool isOverBought = candle.IsAboveBollingerBands(false) && candle.IsStochOverbought();

            if (side == CryptoTradeSide.Long)
            {
                if (isOverBought) // Een short melding! Nee ongeldig!
                    return null;
                if (skipCandleCount <= 0 && isOverSold)
                    return candle;
            }
            else
            {
                if (isOverSold) // Een long melding! Nee ongeldig!
                    return null;
                if (skipCandleCount <= 0 && isOverBought)
                    return candle;
            }

            if (!GetPrevCandle(candle, out candle))
                return null;
            candleCount--;
        }

        return null;
    }


}

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal.Momentum;

namespace CryptoScanBot.Core.Signal;


// Het draait allemaal om de status van het algoritme
// (het algoritme zet die status zelf alsmede delay enz.):
// -None, candle aanbieden voor signaal detectie
// -WarmingUp (voor de indicators)
// -Delaying: Een (optionele) delay
// -TryStepIn: Na een OK van het algoritme om in te stappen

public class SignalCreateBase
{
    protected CryptoAccount Account;
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

    public SignalCreateBase(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle)
    {
        Account = account;
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



    /// <summary>
    /// Is de RSI oversold geweest in de laatste x candles
    /// </summary>
    public bool WasRsiOversoldInTheLast(int candleCount = 30)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        CryptoCandle? candle = CandleLast;
        while (candleCount >= 0)
        {
            if (candle is not null && candle.IsRsiOversold())
                return true;

            if (!GetPrevCandle(candle, out candle))
                return false;
            candleCount--;
        }
        return false;
    }


    /// <summary>
    /// Is de RSI overbought geweest in de laatste x candles
    /// </summary>
    public bool WasRsiOverboughtInTheLast(int candleCount = 30)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        CryptoCandle? candle = CandleLast;
        while (candleCount >= 0)
        {
            if (candle is not null && candle.IsRsiOverbought())
                return true;

            if (!GetPrevCandle(candle, out candle))
                return false;
            candleCount--;
        }
        return false;
    }


    public CryptoCandle? HadStobbInThelastXCandles(CryptoTradeSide side, int skipCandleCount, int candleCount)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        CryptoCandle? candle = CandleLast;
        while (candleCount > 0)
        {
            skipCandleCount--;
            bool isOverSold = candle is not null && candle.IsBelowBollingerBands(false) && candle.IsStochOversold();
            bool isOverBought = candle is not null && candle.IsAboveBollingerBands(false) && candle.IsStochOverbought();

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


    /// <summary>
    /// Is de RSI oplopend in de laatste x candles
    /// 2e parameter geeft aan hoeveel afwijkend mogen zijn
    /// </summary>
    public bool IsRsiIncreasingInTheLast(int candleCount, int allowedDown)
    {
        // We gaan van rechts naar links (van de nieuwste candle richting verleden)
        int down = 0;
        bool first = true;
        CryptoCandle? last = CandleLast;

        // En van de candles daarvoor mag er een (of meer) afwijken
        while (candleCount > 0)
        {
            if (!GetPrevCandle(last!, out CryptoCandle? prev))
                return false;

            if (last?.CandleData?.Rsi <= prev?.CandleData?.Rsi)
            {
                down++;
                if (first || down > allowedDown)
                    return false;
            }

            last = prev;
            candleCount--;
            first = false;
        }

        return true;
    }


    /// <summary>
    /// Is de RSI aflopend in de laatste x candles
    /// 2e parameter geeft aan hoeveel afwijkend mogen zijn
    /// </summary>
    public bool IsRsiDecreasingInTheLast(int candleCount, int allowedDown)
    {
        // We gaan van rechts naar links (van de nieuwste candle richting verleden)
        int down = 0;
        bool first = true;
        CryptoCandle? last = CandleLast;


        // En van de candles daarvoor mag er een (of meer) afwijken
        while (candleCount > 0)
        {
            if (!GetPrevCandle(last, out CryptoCandle? prev))
                return false;

            if (last.CandleData?.Rsi >= prev!.CandleData?.Rsi)
            {
                down++;
                if (first || down > allowedDown)
                    return false;
            }

            last = prev;
            candleCount--;
            first = false;
        }

        return true;
    }

    public CryptoCandle? HadStorsiInThelastXCandles(CryptoTradeSide side, int skipCandleCount, int candleCount)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        CryptoCandle? candle = CandleLast;
        while (candleCount > 0)
        {
            skipCandleCount--;
            bool isOverSold = candle is not null && candle.IsStochOversold(GlobalData.Settings.Signal.StoRsi.AddStochAmount) && CandleLast.IsRsiOversold(GlobalData.Settings.Signal.StoRsi.AddRsiAmount);
            bool isOverBought = candle is not null && candle.IsStochOverbought(GlobalData.Settings.Signal.StoRsi.AddStochAmount) && CandleLast.IsRsiOverbought(GlobalData.Settings.Signal.StoRsi.AddRsiAmount);

            if (side == CryptoTradeSide.Long)
            {
                if (isOverBought) // short signal! Invalid
                    return null;
                if (skipCandleCount < 0 && isOverSold)
                    return candle;
            }
            else
            {
                if (isOverSold) // long signal! Invalid
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

}
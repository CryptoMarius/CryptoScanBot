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
        // Is de prijs onlangs dicht bij de onderste bb geweest?

        CryptoCandle? last = CandleLast;
        while (candleCount > 0)
        {
            decimal? value = (decimal?)last?.CandleData?.BollingerBandsLowerBand;
            value += (decimal?)last?.CandleData?.BollingerBandsDeviation * percentage / 100m;

            if (GlobalData.Settings.Signal.Sbm.Sbm2UseLowHigh)
            {
                if (last?.Low <= value)
                    return true;
            }
            else
            {
                if (last?.Open <= value || last?.Close <= value)
                    return true;
            }

            if (!GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }

        return false;
    }


    protected bool InUpperPartOfBollingerBands(int candleCount, decimal percentage)
    {
        // Is de prijs onlangs dicht bij de bovenste bb geweest?

        CryptoCandle? last = CandleLast;
        while (candleCount > 0)
        {
            decimal value = (decimal)last!.CandleData?.BollingerBandsUpperBand!;
            value -= (decimal)last!.CandleData?.BollingerBandsDeviation! * percentage / 100m;

            if (GlobalData.Settings.Signal.Sbm.Sbm2UseLowHigh)
            {
                if (last.High >= value)
                    return true;
            }
            else
            {
                if (last.Open >= value || last.Close >= value)
                    return true;
            }

            if (!GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }

        return false;
    }


    //public static (bool result, CryptoCandle? candle) CalculateBarometerIndicators(CryptoSymbol symbol, CryptoInterval candleInterval, CryptoCandle candleLast)
    //{
    //    // Calculate the indicators of the barometer
    //    if (!symbol.Exchange.SymbolListName.TryGetValue(Const.Constants.SymbolNameBarometerPrice + symbol.QuoteData.Name, out CryptoSymbol? bmSymbol))
    //        return (false, null);

    //    // Calculate the last candle into the barometer list (the barometer is calculated each minute)
    //    CryptoSymbolInterval symbolInterval = bmSymbol.GetSymbolInterval(CryptoIntervalPeriod.interval1h);
    //    long candleOpenTime = candleLast.OpenTime + candleInterval.Duration - 60;
    //    if (!symbolInterval.CandleList.TryGetValue(candleOpenTime, out CryptoCandle? candle))
    //    {
    //        // 1 minute back because it might nog have been calcuated yet
    //        candleOpenTime -= 60;
    //        if (!symbolInterval.CandleList.TryGetValue(candleOpenTime, out candle))
    //            return (false, null);
    //    }

    //    // Calculate indicators if needed
    //    if (candle.CandleData == null)
    //    {
    //        List<CryptoCandle>? history = CandleIndicatorData.CalculateCandles(bmSymbol, symbolInterval.Interval, candle.OpenTime, out string _);
    //        if (history == null)
    //            return (false, null);
    //        CandleIndicatorData.CalculateIndicators(bmSymbol, symbolInterval.Interval, history);
    //    }

    //    return (true, candle);
    //}


}
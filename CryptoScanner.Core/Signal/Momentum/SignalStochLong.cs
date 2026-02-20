using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Momentum;

public class SignalStochLong : SignalSbmBaseLong
{
    public SignalStochLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }


    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Sma20 == null
           || data.CandleData.StochSignal == null
           || data.CandleData.StochOscillator == null
           || data.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    public override bool AdditionalChecks(MyData data, out string response)
    {
        //if (GlobalData.Settings.Signal.StoRsi.OnlyIfLux5m)
        //{
        //    if (CandleLast.CandleData!.Lux5mValue > -50)
        //    {
        //        response = $"lux 5m not oversold enough ({CandleLast.CandleData!.Lux5mValue}%)";
        //        return false;
        //    }
        //}

        //// Controle op de ma-lijnen
        //if (GlobalData.Settings.Signal.Stobb.IncludeSoftSbm)
        //{
        //    if (!CandleLast!.IsSbmConditionsOversold(false))
        //    {
        //        response = "no sbm conditions";
        //        return false;
        //    }
        //}

        //// Controle op de ma-kruisingen
        //if (GlobalData.Settings.Signal.Stobb.IncludeSbmPercAndCrossing)
        //{
        //    if (!data.IsPercentageSma200AndSma50OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
        //        return false;
        //    if (!data.IsPercentageSma200AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
        //        return false;
        //    if (!data.IsPercentageSma50AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
        //        return false;

        //    if (!CheckMaCrossings(out response))
        //        return false;
        //}

        //// Controle op de RSI
        //if (GlobalData.Settings.Signal.Stobb.IncludeRsi && !CandleLast.RsiOversold())
        //{
        //    response = "rsi not oversold";
        //    return false;
        //}

        //if (GlobalData.Settings.Signal.Stobb.OnlyIfPreviousStobb && HadStobbInThelastXCandlesOversold(SignalSide, 5, 60) == null)
        //{
        //    response = "no previous stobb found";
        //    return false;
        //}

        response = "";
        return true;
    }



    public bool HadStochOscillatorPrettyOversoldInThelastXCandles(int candleCount, int oscValue)
    {
        MyData? candle = CandleLast;
        while (candleCount > 0)
        {
            candleCount--;
            if (!GetPrevCandle(candle, out candle))
                return false;

            if (candle!.CandleData!.StochOscillator < oscValue)
                return true;
        }
        return false;
    }


    //private bool HasACoupleOfStochOversold(CryptoSymbolInterval symbolInterval, CryptoCandle? data, int candleCount, int oscValue, int limit)
    //{
    //    // Is a data of the 5 last candles stoch oversold?
    //    int count = 0;
    //    while (candleCount > 0)
    //    {
    //        if (IndicatorsOkay(data!) && data!.CandleData!.StochOscillator < oscValue)
    //            count++;
    //        if (!GetPrevCandle(symbolInterval, data, out data))
    //            return false;
    //        candleCount--;
    //    }
    //    if (count < limit)
    //        return false;

    //    return true;
    //}


    public override bool IsSignal()
    {
        ExtraText = "";


        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }


        if (!this.RsiIncreasingInTheLast(SymbolInterval, CandleLast, 2, 999))
        {
            ExtraText = "rsi not increasing";
            return false;
        }


        // From oversold to not oversold

        // Crossing of Stoch Oscilator=18
        int oscLimit = 18;
        if (CandleLast.CandleData?.StochOscillator < oscLimit)
        {
            ExtraText = "last stoch oversold";
            return false;
        }
        if (!GetPrevCandle(CandleLast!, out MyData? candlePrev))
            return false;
       
        if (candlePrev!.CandleData?.StochOscillator > oscLimit)
        {
            ExtraText = "prev stoch not oversold";
            return false;
        }


        //// Stoch Oscilator needs to have 3 candles to be < 10 in the last 10 candles)
        //if (!HasACoupleOfStochOversold(SymbolInterval, CandleLast, 10, 10, 3))
        //{
        //    ExtraText = $"stoch osc not oversold < {10}";
        //    return false;
        //}

        double stochSurface = this.StochOversoldSurface(SymbolInterval, CandleLast, 30, GlobalData.Settings.General.SettingsStoch.Oversold);
        if (stochSurface < 5)
        {
            ExtraText = $"stoch osc not oversold < {10}";
            return false;
        }

        if (Interval.IntervalPeriod == CryptoIntervalPeriod.interval1w)
            return false;
        CryptoSymbolInterval higherInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod + 1);

        // To higher interval
        var result = this.CalculateIndicatorsForInterval(SymbolInterval, Symbol, CandleLast, higherInterval);
        if (!result.result)
            return false;

        //// Stoch Oscilator on higher interval needs to have 2 candles to be < 15 in the last 10 candles)
        //if (!HasACoupleOfStochOversold(result.higherInterval, CandleLast, 10, 15, 2))
        //{
        //    ExtraText = $"stoch osc not oversold < {15}";
        //    return false;
        //}


        // does not work in higher interval, this needs extra work..
        //if (!InLowerPartOfBollingerBands(3, 5.0m))
        //{
        //    ExtraText = "not in lower part of bb";
        //    return false;
        //}


        //// storsi condition is too strong..
        //if (!WasRsiOversoldInTheLast(30))
        //{
        //    ExtraText = "no prev rsi oversold";
        //    return false;
        //}

        //if (HadStorsiInThelastXCandles(SignalSide, 0, 40) == null)
        //{
        //    ExtraText = "no prev storsi";
        //    return false;
        //}

        //var x = CalculateBarometerIndicators(Symbol, Interval, CandleLast);
        //if (!x.result)
        //    return false;
        //ExtraText = $"BM: RSI:{x.data!.CandleData!.Rsi:N2} SIG:{x.data!.CandleData!.StochOscillator:N2} HIS:{x.data!.CandleData!.MacdHistogram:N2}";

        double stochSurface2 = this.StochOversoldSurface(result.higherInterval, result.candle!, 30, GlobalData.Settings.General.SettingsStoch.Oversold);
        if (stochSurface2 < 5)
        {
            ExtraText = $"stoch osc not oversold < {5}";
            return false;
        }

        double rsiSurface = this.RsiOversoldSurface(SymbolInterval, CandleLast, 30, GlobalData.Settings.General.SettingsRsi.Oversold);
        double rsiSurface2 = this.RsiOversoldSurface(result.higherInterval, result.candle!, 30, GlobalData.Settings.General.SettingsRsi.Oversold);
        ExtraText = $"sto:{stochSurface:N2}/{stochSurface2:N2} rsi:{rsiSurface:N2}/{rsiSurface2:N2}";

        return true;
    }


}


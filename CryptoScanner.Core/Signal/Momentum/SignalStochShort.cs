using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Momentum;

public class SignalStochShort : SignalSbmBaseShort
{


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
        //    if (CandleLast.CandleData!.Lux5mValue < 50)
        //    {
        //        response = $"lux 5m not overbought enough ({CandleLast.CandleData!.Lux5mValue}%)";
        //        return false;
        //    }
        //}

        //// Controle op de ma-lijnen
        //if (GlobalData.Settings.Signal.Stobb.IncludeSoftSbm)
        //{
        //    if (!CandleLast.IsSbmConditionsOverbought(false))
        //    {
        //        response = "no sbm conditions";
        //        return false;
        //    }
        //}

        //// Controle op de ma-kruisingen
        //if (GlobalData.Settings.Signal.Stobb.IncludeSbmPercAndCrossing)
        //{
        //    if (!data.IsPercentageSma200AndSma50OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
        //        return false;
        //    if (!data.IsPercentageSma200AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
        //        return false;
        //    if (!data.IsPercentageSma50AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
        //        return false;

        //    if (!CheckMaCrossings(out response))
        //        return false;
        //}

        //// Controle op de RSI
        //if (GlobalData.Settings.Signal.Stobb.IncludeRsi && !CandleLast.RsiOverbought())
        //{
        //    response = "rsi not overbought";
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


    public bool HadStochOscillatorPrettyOverboughtInThelastXCandles(int candleCount, int oscValue)
    {
        MyData? candle = CandleLast;
        while (candleCount > 0)
        {
            candleCount--;
            if (!GetPrevCandle(candle, out candle))
                return false;

            if (candle!.CandleData!.StochOscillator > oscValue)
                return true;
        }
        return false;
    }


    //private bool HasACoupleOfStochOverbought(CryptoSymbolInterval symbolInterval, CryptoCandle? data, int candleCount, int oscValue, int limit)
    //{
    //    // Is a data of the 5 last candles stoch oversold?
    //    int count = 0;
    //    while (candleCount > 0)
    //    {
    //        if (IndicatorsOkay(data!) && data!.CandleData!.StochOscillator > oscValue)
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


        if (!this.RsiDecreasingInTheLast(SymbolInterval, CandleLast, 2, 999))
        {
            ExtraText = "rsi not decreasing";
            return false;
        }

        // From overbought to not overbought

        // Crossing of Stoch Oscilator=82
        int oscLimit = 82;
        if (CandleLast.CandleData?.StochOscillator > oscLimit)
        {
            ExtraText = "last stoch overbough";
            return false;
        }
        if (!GetPrevCandle(CandleLast!, out MyData? candlePrev))
            return false;
        if (candlePrev!.CandleData?.StochOscillator < oscLimit)
        {
            ExtraText = "prev stoch not overbough";
            return false;
        }



        double stochSurface = this.StochOverboughtSurface(SymbolInterval, CandleLast, 30, GlobalData.Settings.General.SettingsStoch.Overbought);
        if (stochSurface < 5)
        {
            ExtraText = $"stoch osc not overbough < {90}";
            return false;
        }


        // To higher interval
        if (Interval.IntervalPeriod == CryptoIntervalPeriod.interval1w)
            return false;
        var result = IndicatorDataList.CalculateIndicatorsForInterval(Symbol, Interval, CandleLast.Candle.OpenTime, Interval.IntervalPeriod + 1);
        if (!result.success)
            return false;

        double stochSurface2 = this.StochOverboughtSurface(result.higherInterval, result.candle!, 30, GlobalData.Settings.General.SettingsStoch.Overbought);
        if (stochSurface2 < 5)
        {
            ExtraText = $"stoch osc not overbough < {85}";
            return false;
        }

        double rsiSurface = this.RsiOverboughtSurface(SymbolInterval, CandleLast, 70, GlobalData.Settings.General.SettingsRsi.Overbought);
        double rsiSurface2 = this.RsiOverboughtSurface(result.higherInterval, result.candle!, 70, GlobalData.Settings.General.SettingsRsi.Overbought);
        ExtraText = $"sto:{stochSurface:N2}/{stochSurface2:N2} rsi:{rsiSurface:N2}/{rsiSurface2:N2}";

        return true;
    }


}


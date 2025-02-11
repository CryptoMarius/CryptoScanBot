using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

public class SignalStochLong : SignalSbmBaseLong
{
    public SignalStochLong(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(account, symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.Stoch;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Sma20 == null
           || candle.CandleData.StochSignal == null
           || candle.CandleData.StochOscillator == null
           || candle.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    public override bool AdditionalChecks(CryptoCandle candle, out string response)
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
        //    if (!candle.IsSma200AndSma50OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
        //        return false;
        //    if (!candle.IsSma200AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
        //        return false;
        //    if (!candle.IsSma50AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
        //        return false;

        //    if (!CheckMaCrossings(out response))
        //        return false;
        //}

        //// Controle op de RSI
        //if (GlobalData.Settings.Signal.Stobb.IncludeRsi && !CandleLast.IsRsiOversold())
        //{
        //    response = "rsi not oversold";
        //    return false;
        //}

        //if (GlobalData.Settings.Signal.Stobb.OnlyIfPreviousStobb && HadStobbInThelastXCandles(SignalSide, 5, 60) == null)
        //{
        //    response = "no previous stobb found";
        //    return false;
        //}

        response = "";
        return true;
    }



    public bool HadStochOscillatorPrettyOversoldInThelastXCandles(int candleCount, int oscValue)
    {
        CryptoCandle? candle = CandleLast;
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



    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }


        if (!IsRsiIncreasingInTheLast(2, 999))
        {
            ExtraText = "rsi not increasing";
            return false;
        }


        // From oversold to not oversold after a stob or storsi

        if (CandleLast.IsStochOscillatorOversold(2))
        {
            ExtraText = "last stoch not oversold";
            return false;
        }

        if (!GetPrevCandle(CandleLast!, out CryptoCandle? candlePrev))
            return false;


        if (!candlePrev!.IsStochOscillatorOversold(2))
        {
            ExtraText = "prev stoch not oversold";
            return false;
        }

        // Needs to be be pretty oversold (not just slighly)
        int limit = 10;
        if (!HadStochOscillatorPrettyOversoldInThelastXCandles(10, limit))
        {
            ExtraText = $"stoch osc not oversold < {limit}";
            return false;
        }
        


        // Parameter BB...
        // Parameter RSI ...
        // Parameter candles
        // Parameters Stoch/Storsi/Both

        //if (HadStobbInThelastXCandles(SignalSide, 0, 40) == null && HadStorsiInThelastXCandles(SignalSide, 0, 40) == null)
        //{
        //    ExtraText = "no prev stobb/storsi";
        //    return false;
        //}

        return true;
    }


}


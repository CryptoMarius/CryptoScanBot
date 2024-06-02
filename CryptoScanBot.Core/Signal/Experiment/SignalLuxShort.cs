using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal.Momentum;

namespace CryptoScanBot.Core.Signal.Experiment;

public class SignalFluxShort : SignalSbmBaseShort
{
    public SignalFluxShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.Lux;
    }


    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        response = "";

        // ********************************************************************
        // Price must to above BB.upper
        if (CandleLast.Close <= (decimal)CandleLast.CandleData.BollingerBandsUpperBand)
        {
            ExtraText = "price below bb.high";
            return false;
        }

        // ********************************************************************
        // percentage ema5 and close at least 1%
        decimal percentage = 100 * CandleLast.Close / (decimal)CandleLast.CandleData.Ema5;
        if (percentage < 100.25m)
        {
            response = $"distance close and ema5 not ok {percentage:N2}";
            return false;
        }

        //// ********************************************************************
        //// MA lines without psar
        //if (!CandleLast.IsSbmConditionsOverbought(false))
        //{
        //    response = "geen sbm condities";
        //    return false;
        //}

        //// ********************************************************************
        //if (!IsRsiDecreasingInTheLast(3, 1))
        //{
        //    response = string.Format("RSI niet afnemend in de laatste 3,1");
        //    return false;
        //}

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // ********************************************************************
        // BB width is within certain percentages
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Sbm.BBMinPercentage, GlobalData.Settings.Signal.Sbm.BBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }


        // ********************************************************************
        // Rsi
        if (CandleLast.CandleData.Rsi < GlobalData.Settings.General.RsiValueOverbought)
        {
            ExtraText = $"rsi {CandleLast.CandleData.Rsi} not below {GlobalData.Settings.General.RsiValueOverbought}";
            return false;
        }


        // ********************************************************************
        // Flux
        LuxIndicator.Calculate(Symbol, out int _, out int luxOverBought, CryptoIntervalPeriod.interval5m, CandleLast.OpenTime + Interval.Duration);
        if (luxOverBought < 100)
        {
            ExtraText = $"flux overbought {luxOverBought} below 100";
            return false;
        }

        decimal percentage = 100 * CandleLast.Close / (decimal)CandleLast.CandleData.Ema5;
        ExtraText = $"{percentage:N2}%";
        return true;
    }
}

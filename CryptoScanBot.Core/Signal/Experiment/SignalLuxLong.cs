using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal.Momentum;

namespace CryptoScanBot.Core.Signal.Experiment;

public class SignalLuxLong : SignalSbmBaseLong
{
    public SignalLuxLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.Lux;
    }

    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        response = "";

        // ********************************************************************
        // Price must be below BB.Low
        if (CandleLast.Close >= (decimal)CandleLast.CandleData.BollingerBandsLowerBand)
        {
            ExtraText = "price above bb.low";
            return false;
        }

        // ********************************************************************
        // percentage ema5 and close at least 1%
        decimal percentage = 100 * (decimal)CandleLast.CandleData.Ema5 / CandleLast.Close;
        if (percentage < 100.25m)
        {
            response = $"distance close and ema5 not ok {percentage:N2}";
            return false;
        }


        //// ********************************************************************
        //// MA lines without psar
        //if (!CandleLast.IsSbmConditionsOversold(false))
        //{
        //    response = "geen sbm condities";
        //    return false;
        //}

        //// ********************************************************************
        //if (!IsRsiIncreasingInTheLast(3, 1))
        //{
        //    response = string.Format("RSI niet oplopend in de laatste 3,1");
        //    return false;
        //}


        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        //RSI 100 % oversold: Komt ook niet vaak voor, wwl die rode spikes een drop naar beneden
        //icm de ema5 kijk ik meer naar maar
        //de rsi is niet heilig
        //Check natuurlijk ook dat koers is onder BB, en ik doe alles op de 5 min chart


        // ********************************************************************
        // BB width is within certain percentages
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Sbm.BBMinPercentage, GlobalData.Settings.Signal.Sbm.BBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }


        // ********************************************************************
        // Rsi
        if (CandleLast.CandleData.Rsi > GlobalData.Settings.General.RsiValueOversold)
        {
            ExtraText = $"rsi {CandleLast.CandleData.Rsi} not above {GlobalData.Settings.General.RsiValueOversold}";
            return false;
        }


        // ********************************************************************
        // Flux
        SignalCreate.GetLuxIndicator(Symbol, out int luxOverSold, out int _);
        if (luxOverSold < 100)
        {
            ExtraText = $"flux oversold {luxOverSold} below 100";
            return false;
        }

        decimal percentage = 100 * (decimal)CandleLast.CandleData.Ema5 / CandleLast.Close;
        ExtraText = $"{percentage:N2}%";
        return true;
    }

}

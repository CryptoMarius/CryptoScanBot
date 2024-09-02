using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

// WGHM - Wave Generation High Momentum

// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class SignalStoRsiLong : SignalSbmBaseLong
{
    public SignalStoRsiLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.StoRsi;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Rsi == null
           || candle.CandleData.StochSignal == null
           || candle.CandleData.StochOscillator == null
           || candle.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }

    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        // Check above/below STOBB BB bands
        if (GlobalData.Settings.Signal.StoRsi.CheckBollingerBandsCondition)
        {
            if (!CandleLast.IsBelowBollingerBands(GlobalData.Settings.Signal.Stobb.UseLowHigh))
            {
                response = "not below bb.lower";
                return false;
            }
        }

        // disable sbm conditions
        response = "";
        return true;
    }

    public override bool IsSignal()
    {
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = "bb.width too small " + CandleLast.CandleData!.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        if (!CandleLast.IsStochOversold(GlobalData.Settings.Signal.StoRsi.AddStochAmount))
        {
            ExtraText = "stoch not oversold";
            return false;
        }

        if (!CandleLast.IsRsiOversold(GlobalData.Settings.Signal.StoRsi.AddRsiAmount))
        {
            ExtraText = "rsi not oversold";
            return false;
        }


        ExtraText = "";
        return true;
    }


}

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

// WGHM - Wave Generation High Momentum

// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class SignalStoRsiShort : SignalSbmBaseShort
{
    public SignalStoRsiShort(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(account, symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
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
            //if (!CandleLast.IsAboveBollingerBands(GlobalData.Settings.Signal.Stobb.UseHighLow))
            if (!IsInUpperPartOfBollingerBands(3, 5.0m))
            {
                response = "not in upper part of bb";
                return false;
            }
        }

        if (GlobalData.Settings.Signal.StoRsi.SkipFirstSignal)
        {
            if (HadStorsiInThelastXCandles(SignalSide, 1, 3) == null)
            {
                response = "skip first storsi";
                return false;
            }
        }

        // disable sbm conditions (inheritance)
        response = "";
        return true;
    }


    public override bool IsSignal()
    {
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StoRsi.BBMinPercentage, GlobalData.Settings.Signal.StoRsi.BBMaxPercentage))
        {
            ExtraText = "bb.width too small " + CandleLast.CandleData!.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        if (!CandleLast.IsStochOverbought(GlobalData.Settings.Signal.StoRsi.AddStochAmount))
        {
            ExtraText = "stoch not overbought";
            return false;
        }

        if (!CandleLast.IsRsiOverbought(GlobalData.Settings.Signal.StoRsi.AddRsiAmount))
        {
            ExtraText = "rsi not overbought";
            return false;
        }

        ExtraText = "";
        return true;
    }

}

using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;


// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class SignalStoRsiShort : SignalSbmBaseShort
{
    public SignalStoRsiShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
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

        if (!CandleLast.IsStochOverbought(GlobalData.Settings.Signal.StoRsi.AddAmount))
        {
            ExtraText = "stoch not overbought";
            return false;
        }

        if (!CandleLast.IsRsiOverbought(GlobalData.Settings.Signal.StoRsi.AddAmount))
        {
            ExtraText = "rsi not overbought";
            return false;
        }

        ExtraText = "";
        return true;
    }

}

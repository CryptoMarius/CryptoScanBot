using CryptoScanner.Analyzers.Stobb;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.BbRsiEngulfing.Signal;

public class BbRsiEngulfingLong : SignalCreateBase
{

    public override bool IndicatorsOkay(MyData data)
    {
        if ((data == null)
           || data.Candle.OpenTime == 0
           || (data.CandleData == null)
            || (data.CandleData.Rsi == null)
            || (data.CandleData.BollingerBandsLowerBand == null)
            )
            return false;

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // BB width must be at least 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(StobbPlugin.Settings.BBMinPercentage, 0)) //GlobalData.Settings.Signal.AnalysisBBMaxPercentage
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }


        if (!GetPrevCandle(CandleLast!, out MyData? prev))
            return false;


        // Prev below BB
        if (prev!.Candle.Close >= (decimal)prev!.CandleData!.BollingerBandsLowerBand!)
        {
            ExtraText = "not below bb.lower";
            return false;
        }

        // Rsi oversold
        if (!prev!.RsiOversold(4))
        {
            ExtraText = "rsi not oversold";
            return false;
        }

        // Candle last closes above the high of the previous — or a real engulfing, see UseStrictEngulfing
        if (BbRsiEngulfingPlugin.Settings.UseStrictEngulfing)
        {
            if (!CandlePatternHelper.Matches(CryptoCandlePattern.Engulfing, CryptoTradeSide.Long,
                    CandleLast.Candle, prev!.Candle, null, new CandlePatternSettings()))
            {
                ExtraText = "not engulfing (strict)";
                return false;
            }
        }
        else if (CandleLast.Candle.Close <= prev!.Candle.High)
        {
            ExtraText = "not engulfing";
            return false;
        }

        if (HadStorsiInThelastXCandles(SignalSide, 0, 25, 4) == null && HadStobbInThelastXCandles(SignalSide, 0, 25, StobbPlugin.Settings.UseLowHigh) == null)
        {
            ExtraText = "no previous storsi/stobb found";
            return false;
        }

        return true;
    }


}

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Sbm.Signal;

public class SignalSbmBase : SignalCreateBase
{
    public override int MacdRecoveryBarCount => SbmPlugin.Settings.CandlesForMacdRecovery;

    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Sma20 == null
           || data.CandleData.Sma50 == null
           || data.CandleData.Sma200 == null
           || data.CandleData.PSar == null
           || data.CandleData.MacdHistogram == null
           || data.CandleData.StochSignal == null
           || data.CandleData.StochOscillator == null
           || data.CandleData.BollingerBandsDeviation == null
           )
        {
            ExtraText = "indicators not ok!";
            return false;
        }

        return true;
    }

    public override bool AdditionalChecks(MyData candle, out string response)
    {
        switch (SignalSide)
        {
            case CryptoTradeSide.Long:
                if (!this.IsMacdRecoveryOversold(SbmPlugin.Settings.CandlesForMacdRecovery))
                {
                    response = "no macd recovery";
                    return false;
                }

                if (SbmPlugin.Settings.CheckMa200AndMa50Percentage &&
                    !candle.IsPercentageSma200AndSma50OkayOversold(SbmPlugin.Settings.Ma200AndMa50Percentage, out response))
                    return false;
                if (SbmPlugin.Settings.CheckMa200AndMa20Percentage &&
                    !candle.IsPercentageSma200AndSma20OkayOversold(SbmPlugin.Settings.Ma200AndMa20Percentage, out response))
                    return false;
                if (SbmPlugin.Settings.CheckMa50AndMa20Percentage &&
                    !candle.IsPercentageSma50AndSma20OkayOversold(SbmPlugin.Settings.Ma50AndMa20Percentage, out response))
                    return false;

                break;
            case CryptoTradeSide.Short:
                if (!this.IsMacdRecoveryOverbought(SbmPlugin.Settings.CandlesForMacdRecovery))
                {
                    response = "no macd recovery";
                    return false;
                }

                if (SbmPlugin.Settings.CheckMa200AndMa50Percentage &&
                    !candle.IsPercentageSma200AndSma50OkayOverbought(SbmPlugin.Settings.Ma200AndMa50Percentage, out response))
                    return false;
                if (SbmPlugin.Settings.CheckMa200AndMa20Percentage &&
                    !candle.IsPercentageSma200AndSma20OkayOverbought(SbmPlugin.Settings.Ma200AndMa20Percentage, out response))
                    return false;
                if (SbmPlugin.Settings.CheckMa50AndMa20Percentage &&
                    !candle.IsPercentageSma50AndSma20OkayOverbought(SbmPlugin.Settings.Ma50AndMa20Percentage, out response))
                    return false;
                break;
        }

        var sbm = SbmPlugin.Settings;
        if (!CheckMaCrossings(
            sbm.Ma200AndMa20Crossing, sbm.Ma200AndMa20Lookback,
            sbm.Ma200AndMa50Crossing, sbm.Ma200AndMa50Lookback,
            sbm.Ma50AndMa20Crossing, sbm.Ma50AndMa20Lookback,
            out response))
            return false;

        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        if (base.GiveUp(signal))
            return true;

        switch (SignalSide)
        {
            case CryptoTradeSide.Long:
                if (CandleLast?.Candle.Close > (decimal?)CandleLast?.CandleData?.BollingerBandsUpperBand ||
                    Symbol.LastPrice > (decimal?)CandleLast?.CandleData?.BollingerBandsUpperBand)
                {
                    ExtraText = "Close of LastPrice above bb.upper";
                    return true;
                }
                break;
            case CryptoTradeSide.Short:
                if (CandleLast!.Candle.Close < (decimal)CandleLast!.CandleData?.BollingerBandsLowerBand! ||
                    Symbol.LastPrice < (decimal)CandleLast.CandleData?.BollingerBandsLowerBand!)
                {
                    ExtraText = "Close of LastPrice below bb.lower";
                    return true;
                }
                break;
        }

        ExtraText = "";
        return false;
    }

}


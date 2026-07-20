using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Sbm.Signal;

public class SignalSbmBase : SignalCreateBase
{
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
                if (!this.IsMacdRecoveryOversold(GlobalData.Settings.Signal.Sbm.CandlesForMacdRecovery))
                {
                    response = "no macd recovery";
                    return false;
                }

                if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa50Percentage &&
                    !candle.IsPercentageSma200AndSma50OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa20Percentage &&
                    !candle.IsPercentageSma200AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa50AndMa20Percentage &&
                    !candle.IsPercentageSma50AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
                    return false;

                break;
            case CryptoTradeSide.Short:
                if (!this.IsMacdRecoveryOverbought(GlobalData.Settings.Signal.Sbm.CandlesForMacdRecovery))
                {
                    response = "no macd recovery";
                    return false;
                }

                if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa50Percentage &&
                    !candle.IsPercentageSma200AndSma50OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa20Percentage &&
                    !candle.IsPercentageSma200AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa50AndMa20Percentage &&
                    !candle.IsPercentageSma50AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
                    return false;
                break;
        }

        if (!CheckMaCrossings(out response))
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


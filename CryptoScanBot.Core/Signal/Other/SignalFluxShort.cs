using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Other;

#if EXTRASTRATEGIES
public class SignalFluxShort: SignalSbmBaseLong
{
    public SignalFluxShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.Flux;
    }

    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Sbm.BBMinPercentage, GlobalData.Settings.Signal.Sbm.BBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        // De ma lijnen en psar goed staan
        if (!CandleLast.IsSbmConditionsOverbought(false))
        {
            ExtraText = "geen sbm condities";
            return false;
        }

        // ********************************************************************
        // RSI
        if (CandleLast.CandleData.Rsi < 80)
        {
            ExtraText = string.Format("RSI {0:N8} niet onder de 80", CandleLast.CandleData.Rsi);
            return false;
        }

        SignalCreate.GetFluxIndcator(Symbol, out int _, out int fluxOverBought);
        if (fluxOverBought == 100)
            return true;

        return false;
    }
}
#endif

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;



public class SignalFluxOversold : SignalSbmBaseOversold
{
    public SignalFluxOversold(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.Flux;
    }

    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.SbmBBMinPercentage, GlobalData.Settings.Signal.SbmBBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        // De ma lijnen en psar goed staan
        if (!CandleLast.IsSbmConditionsOversold(false))
        {
            ExtraText = "geen sbm condities";
            return false;
        }

        // ********************************************************************
        // RSI
        if (CandleLast.CandleData.Rsi > 20) // was 25
        {
            ExtraText = string.Format("RSI {0:N8} niet boven de 20", CandleLast.CandleData.Rsi);
            return false;
        }

        SignalCreate.GetFluxIndcator(Symbol, out int fluxOverSold, out int _);
        if (fluxOverSold == 100)
            return true;

        return false;
    }

}
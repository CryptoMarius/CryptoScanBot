using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalFairValueGapShort : SignalCreateBase
{
    public SignalFairValueGapShort(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(account, symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.FairValueGap;
    }


    public override bool IsSignal()
    {
        return false;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        return false;
    }
}
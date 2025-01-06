using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalDoubleTopBottomLong : SignalCreateBase
{
    public SignalDoubleTopBottomLong(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(account, symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.DoubleTopBottomLong;
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
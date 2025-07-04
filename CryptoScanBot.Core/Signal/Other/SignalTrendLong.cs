using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalTrendLong : SignalCreateBase
{
    public SignalTrendLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.Trend;
    }


    public override bool IsSignal()
    {
        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary, 0, 0, null).Result;

        if (SymbolInterval.TrendPrimary.PrevTime + Interval.Duration == SymbolInterval.TrendPrimary.Time && 
            SymbolInterval.TrendPrimary.PrevTrend == CryptoTrendIndicator.Bearish &&
            SymbolInterval.TrendPrimary.Trend == CryptoTrendIndicator.Bullish)
            return true;

        ExtraText = "no trend change";
        return false;

    }

}

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalTrendShort : SignalCreateBase
{
    public SignalTrendShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        // nothing
    }


    public override bool IsSignal()
    {
        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary, 0, 0, null).Result;

        CryptoTrendData data = SymbolInterval.TrendPrimary;
        if ( SymbolInterval.TrendPrimary.PrevTime + Interval.Duration == SymbolInterval.TrendPrimary.Time && 
            data.PrevTime > 0 && data.PrevTrend == CryptoTrendIndicator.Bullish && data.Trend == CryptoTrendIndicator.Bearish)
        {
            if (!data.ReversalSignaled)
            {
                data.ReversalSignaled = true;
                return true;
            }
        }

        ExtraText = "no trend change";
        return false;
    }

}

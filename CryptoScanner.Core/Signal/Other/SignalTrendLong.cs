using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal.Other;

public class SignalTrendLong : SignalCreateBase
{
    public SignalTrendLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }


    public override bool IsSignal()
    {
        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;

        CryptoTrendData data = SymbolInterval.TrendPrimary;
        if (data.PrevTime != null && data.PrevTime > 0 && 
            data.PrevTime + Interval.Duration == data.Time && 
            data.PrevTrend == CryptoTrendIndicator.Bearish && data.Trend == CryptoTrendIndicator.Bullish)
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

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal.Other;

public class SignalTrendShort : SignalCreateBase
{
    public SignalTrendShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
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

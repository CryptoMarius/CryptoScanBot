namespace CryptoScanner.Core.Enums;


public enum SymbolColumnEnum
{
    Id,
    Symbol,
    Volume,
    //Price
    Distance,
    //MarketTrendPrimary, to much cpu needed

    /// <summary>
    /// The name the instrument has at the exchange ("BTC-USDT-SWAP"), which is not the same as the
    /// scanner name and is what every call to the exchange actually sends. Hidden by default: it is
    /// there to look something up when a symbol behaves oddly, not to read every day.
    /// </summary>
    ExchangeName,
}


public enum SignalColumnEnum
{
    Id,
    Date,
    Exchange,
    Symbol,
    Side,
    Interval,
    Strategy,
    EventText,
    SignalPrice,
    PriceChange,
    SignalVolume,

    TrendInterval,
    TrendPercentagePrimary,
    TrendPercentageSecondary,
    Last24HoursChange,
    LastXDaysEffective,

    BB,
    BbUpper,
    BbLower,
    AvgBB,

    Rsi,
    LuxIndicator5m,
    MacdValue,
    MacdSignal,
    MacdHistogram,
    StochOscillator,
    StochSignal,
    Sma200,
    Sma50,
    Sma20,
    PSar,

    Trend15m,
    Trend30m,
    Trend1h,
    Trend4h,
    Trend1d,

    Barometer15m,
    Barometer30m,
    Barometer1h,
    Barometer4h,
    Barometer1d,

    MinimumEntry,

    //PriceMinPerc,
    //PriceMaxPerc,
    //SignalStatus,
}


public enum PositionColumnEnum
{
    Id,
    AltradyId,
    CreateTime,
    UpdateTime,
    CloseTime,
    Duration,
    Exchange,
    Symbol,
    Interval,
    Side,
    Strategy,
    Status,

    Invested,
    Returned,
    Commission,
    BreakEvenPrice,
    BreakEvenPercent,
    Quantity,
    Open,
    CurrentProfit,
    CurrentProfitPercentage,
    Parts,
    EntryPrice,
    ProfitPrice,
    FundingRate,
    QuantityTick,
    RemainingDust,
    RemainingDustValue,

    SignalDate,
    SignalPrice,
    EventText,
    SignalVolume,

    TrendInterval,
    TrendPercentagePrimary,
    TrendPercentageSecondary,
    Last24HoursChange,
    LastXDaysEffective,

    BB,
    BbUpper,
    BbLower,
    AvgBB,

    Rsi,
    //SlopeRsi,
    LuxIndicator5m,
    MacdValue,
    MacdSignal,
    MacdHistogram,
    StochOscillator,
    StochSignal,
    Sma200,
    Sma50,
    Sma20,
    PSar,

    Trend15m,
    Trend30m,
    Trend1h,
    Trend4h,
    Trend1d,

    Barometer15m,
    Barometer30m,
    Barometer1h,
    Barometer4h,
    Barometer1d,

    MinimumEntry,

    //PriceMin,
    //PriceMax,
    //PriceMinPerc,
    //PriceMaxPerc,
}


public enum LiveDataColumnEnum
{
    Date,
    Exchange,
    Symbol,
    Interval,
    Price,
    Volume,
    BB,
    BbUpper,
    BbLower,
    Rsi,
    LuxIndicator5m,
    MacdValue,
    MacdSignal,
    MacdHistogram,
    StochOscillator,
    StochSignal,
    Sma200,
    Sma50,
    Sma20,
    PSar,
    FundingRate,
#if StrategyBbma
    // Debug
    Wma05Low,
    Wma05High,
    Wma10Low,
    Wma10High,
#endif
}


public enum LogColumnEnum
{
    Date,
    Text,
}

#if DEBUG
using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Signal.Trend;

public class SignalTrendHtfLong : SignalTrendHtfBase
{
    protected override CryptoTrendIndicator RequiredTrend => CryptoTrendIndicator.Bullish;
    protected override char ExpectedPivotType => 'L';

    // Long: break upward through the pullback Low.
    protected override bool IsBreakConfirmed(decimal close, decimal pivotValue) => close > pivotValue;
}
#endif
#if DEBUG
using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Signal.Trend;

public class SignalTrendHtfShort : SignalTrendHtfBase
{
    protected override CryptoTrendIndicator RequiredTrend => CryptoTrendIndicator.Bearish;
    protected override char ExpectedPivotType => 'H';

    // Short: break downward through the pullback High.
    protected override bool IsBreakConfirmed(decimal close, decimal pivotValue) => close < pivotValue;
}
#endif
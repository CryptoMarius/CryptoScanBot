using CryptoScanner.Core.Core;

namespace CryptoScanner.Analyzers.Trend.Signal;

/// <summary>Registered as "trend": reads the primary (rough) trend slot.</summary>
public class SignalTrendLong : SignalTrendLongBase
{
    protected override TrendType TrendType => TrendType.Primary;
}

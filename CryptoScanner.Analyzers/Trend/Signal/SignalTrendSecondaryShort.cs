using CryptoScanner.Core.Core;

namespace CryptoScanner.Analyzers.Trend.Signal;

/// <summary>Registered as "trend.secondary": reads the secondary (fine) trend slot.</summary>
public class SignalTrendSecondaryShort : SignalTrendShortBase
{
    protected override TrendType TrendType => TrendType.Secondary;
}

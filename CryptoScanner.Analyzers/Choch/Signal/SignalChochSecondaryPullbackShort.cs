using CryptoScanner.Core.Core;

namespace CryptoScanner.Analyzers.Choch.Signal;

public class SignalChochSecondaryPullbackShort : SignalChochShortBase
{
    protected override TrendType TrendType => TrendType.Secondary;
    protected override bool RequirePullback => true;
}

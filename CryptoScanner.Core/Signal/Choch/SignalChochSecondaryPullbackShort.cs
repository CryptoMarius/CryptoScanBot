using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Signal.Choch;

public class SignalChochSecondaryPullbackShort : SignalChochShortBase
{
    protected override TrendType TrendType => TrendType.Secondary;
    protected override bool RequirePullback => true;
}

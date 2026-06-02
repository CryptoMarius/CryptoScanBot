using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Signal.Choch;

public class SignalChochSecondaryPullbackLong : SignalChochLongBase
{
    protected override TrendType TrendType => TrendType.Secondary;
    protected override bool RequirePullback => true;
}

using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Signal.Choch;

public class SignalChochPrimaryPullbackLong : SignalChochLongBase
{
    protected override TrendType TrendType => TrendType.Primary;
    protected override bool RequirePullback => true;
}

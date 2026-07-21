using CryptoScanner.Core.Core;

namespace CryptoScanner.Analyzers.Choch.Signal;

public class SignalChochPrimaryPullbackLong : SignalChochLongBase
{
    protected override TrendType TrendType => TrendType.Primary;
    protected override bool RequirePullback => true;
}

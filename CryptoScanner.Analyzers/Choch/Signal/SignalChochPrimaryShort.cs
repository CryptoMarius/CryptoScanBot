using CryptoScanner.Core.Core;

namespace CryptoScanner.Analyzers.Choch.Signal;

public class SignalChochPrimaryShort : SignalChochShortBase
{
    protected override TrendType TrendType => TrendType.Primary;
}

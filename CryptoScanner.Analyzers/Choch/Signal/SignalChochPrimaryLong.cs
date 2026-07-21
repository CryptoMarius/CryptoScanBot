using CryptoScanner.Core.Core;

namespace CryptoScanner.Analyzers.Choch.Signal;

public class SignalChochPrimaryLong : SignalChochLongBase
{
    protected override TrendType TrendType => TrendType.Primary;
}
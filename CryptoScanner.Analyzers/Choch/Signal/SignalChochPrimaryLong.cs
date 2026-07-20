using CryptoScanner.Core.Core;

#if DEBUG
namespace CryptoScanner.Analyzers.Choch.Signal;

public class SignalChochPrimaryLong : SignalChochLongBase
{
    protected override TrendType TrendType => TrendType.Primary;
}
#endif
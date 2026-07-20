using CryptoScanner.Core.Core;

#if DEBUG
namespace CryptoScanner.Analyzers.Choch.Signal;

public class SignalChochPrimaryShort : SignalChochShortBase
{
    protected override TrendType TrendType => TrendType.Primary;
}
#endif

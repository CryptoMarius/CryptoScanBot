using CryptoScanner.Core.Core;

#if DEBUG
namespace CryptoScanner.Core.Signal.Choch;

public class SignalChochPrimaryShort : SignalChochShortBase
{
    protected override TrendType TrendType => TrendType.Primary;
}
#endif

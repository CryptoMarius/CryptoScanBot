using CryptoScanner.Core.Core;

#if DEBUG
namespace CryptoScanner.Core.Signal.Choch;

public class SignalChochPrimaryLong : SignalChochLongBase
{
    protected override TrendType TrendType => TrendType.Primary;
}
#endif
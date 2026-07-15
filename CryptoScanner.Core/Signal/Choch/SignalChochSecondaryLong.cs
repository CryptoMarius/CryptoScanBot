using CryptoScanner.Core.Core;

#if DEBUG
namespace CryptoScanner.Core.Signal.Choch;

public class SignalChochSecondaryLong : SignalChochLongBase
{
    protected override TrendType TrendType => TrendType.Secondary;
}
#endif

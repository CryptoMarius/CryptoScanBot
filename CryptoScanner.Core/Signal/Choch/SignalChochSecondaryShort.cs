using CryptoScanner.Core.Core;

#if DEBUG
namespace CryptoScanner.Core.Signal.Choch;

public class SignalChochSecondaryShort : SignalChochShortBase
{
    protected override TrendType TrendType => TrendType.Secondary;
}
#endif

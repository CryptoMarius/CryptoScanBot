using CryptoScanner.Core.Core;

#if DEBUG
namespace CryptoScanner.Analyzers.Choch.Signal;

public class SignalChochSecondaryShort : SignalChochShortBase
{
    protected override TrendType TrendType => TrendType.Secondary;
}
#endif

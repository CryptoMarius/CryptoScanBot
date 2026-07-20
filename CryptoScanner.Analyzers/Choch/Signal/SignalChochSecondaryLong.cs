using CryptoScanner.Core.Core;

#if DEBUG
namespace CryptoScanner.Analyzers.Choch.Signal;

public class SignalChochSecondaryLong : SignalChochLongBase
{
    protected override TrendType TrendType => TrendType.Secondary;
}
#endif

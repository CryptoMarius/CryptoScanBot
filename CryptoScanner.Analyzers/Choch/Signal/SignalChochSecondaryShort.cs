using CryptoScanner.Core.Core;

namespace CryptoScanner.Analyzers.Choch.Signal;

public class SignalChochSecondaryShort : SignalChochShortBase
{
    protected override TrendType TrendType => TrendType.Secondary;
}

using CryptoScanner.Core.Core;

namespace CryptoScanner.Analyzers.Choch.Signal;

public class SignalChochSecondaryLong : SignalChochLongBase
{
    protected override TrendType TrendType => TrendType.Secondary;
}

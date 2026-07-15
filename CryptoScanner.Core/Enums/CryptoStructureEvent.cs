using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Enums;

public enum CryptoStructureEvent
{
    None,
    Bos,    // Break of Structure - trend continuation confirmed
    ChoCh   // Change of Character - trend reversal signal
}

public record StructureEvent(CandleTime Time, CryptoStructureEvent Type, decimal Price, CryptoTrendIndicator TrendAfter);

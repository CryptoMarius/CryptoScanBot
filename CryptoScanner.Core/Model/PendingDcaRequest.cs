using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Model;

/// <summary>
/// Lightweight DTO written by the candle thread when a signal-based DCA qualifies.
/// The background thread (CheckThePosition) picks it up and performs the actual ExtendPosition.
/// </summary>
public sealed record PendingDcaRequest
(
    CryptoInterval Interval,
    CryptoSignalStrategy Strategy,
    decimal DcaPrice,
    DateTime CandleCloseTime
);

namespace CryptoScanner.Core.Contracts;

/// <param name="IsZoneStrategy">
/// True for strategies that fire on a zone touch (DLZ, FVG, SMC) instead of on an indicator of the
/// signal interval. Those are prepared and evaluated on 1m and skip the barometer check, and their
/// signals are kept five times longer. Declared here because until now that distinction was hidden
/// in the numeric value of the former CryptoSignalStrategy enum (anything from DominantLevel
/// upwards), which is exactly what kept that enum alive.
/// </param>
/// <param name="ParallelPositions">
/// True for a strategy that may hold SEVERAL positions on the same coin at the same time, each with
/// its own entry, DCA's and take profit. Every other strategy keeps the one position per coin the
/// trader was built on. Paper trading (and so the emulator) only: Altrady and real trading keep one
/// position per coin. See PositionTools.PositionListKey.
/// </param>
/// <param name="StandingEntry">
/// True for a strategy whose entry is a limit order that stays on the book until it fills or the
/// strategy withdraws it (SignalCreateBase.WithdrawEntry). Such an entry is always a limit order at
/// the signal price, whatever Trading.EntryOrderType and EntryLimitOffsetPercentage say, and it does
/// not expire after Trading.EntryRemoveTime candles.
/// </param>
/// <param name="WithoutStopLoss">
/// True for a strategy that never has a stop loss: Trading.StopLossPercentage is not applied to its
/// positions. A stop the strategy hands over itself (SignalCreateBase.OverrideSlPercentage) and the
/// profit lock (MoveSlToBreakEven) are not affected.
/// </param>
public record StrategyRegistration(
    string Name,
    Type? AnalyzeLongType,
    Type? AnalyzeShortType,
    bool IsZoneStrategy = false,
    bool ParallelPositions = false,
    bool StandingEntry = false,
    bool WithoutStopLoss = false);

using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Contracts;

/// <param name="IsZoneStrategy">
/// True for strategies that fire on a zone touch (DLZ, FVG, SMC) instead of on an indicator of the
/// signal interval. Those are prepared and evaluated on 1m and skip the barometer check, and their
/// signals are kept five times longer. Declared here because until now that distinction was hidden
/// in the numeric value of <see cref="CryptoSignalStrategy"/> (anything from DominantLevel upwards),
/// which is exactly what keeps that enum alive.
/// </param>
public record StrategyRegistration(
    CryptoSignalStrategy Strategy,
    string Name,
    Type? AnalyzeLongType,
    Type? AnalyzeShortType,
    bool IsZoneStrategy = false);

using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Contracts;

public record StrategyRegistration(
    CryptoSignalStrategy Strategy,
    string Name,
    Type? AnalyzeLongType,
    Type? AnalyzeShortType);

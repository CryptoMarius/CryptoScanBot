using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Signal;

// Class for registering all algorithms
public class AlgorithmDefinition
{
    public required string Name { get; set; }
    public required CryptoSignalStrategy Strategy { get; set; }
    public required Type? AnalyzeLongType { get; set; }
    public required Type? AnalyzeShortType { get; set; }

    /// <summary>
    /// When true this strategy bypasses all market-condition filters (barometer, volume,
    /// performance feedback). Use for informational strategies that should always fire
    /// when their own condition is met, regardless of market regime.
    /// Examples: Jump, DLZ, DLZ.Near, FVG.
    /// </summary>
    public bool BypassFilters { get; set; } = false;
}

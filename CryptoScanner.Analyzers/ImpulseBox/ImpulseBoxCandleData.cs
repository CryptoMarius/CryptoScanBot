namespace CryptoScanner.Analyzers.ImpulseBox;

/// <summary>
/// What the impulse box detector saw on one candle, computed once by
/// <see cref="Indicators.ImpulseBoxIndicatorExtension"/> and shared by the long and short signal.
/// Only attached once the ATR has a value, so a missing slot means "not ready yet".
/// </summary>
public sealed class ImpulseBoxCandleData
{
    /// <summary>The events of this candle, null when there are none.</summary>
    public List<ImpulseBoxEvent>? Events { get; set; }

    /// <summary>Where both directions are, for the signal text.</summary>
    public string Status { get; set; } = "";
}

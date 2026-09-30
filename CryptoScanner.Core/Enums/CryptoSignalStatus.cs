namespace CryptoScanner.Core.Enums;

/// <summary>
/// The outcome of a virtual position at the signal price, kept as a statistic on the signal
/// (see SignalStatistics). It only ever moves forward: Run until either the stop is reached
/// (Lost, final) or the first take-profit level is reached (Tp1); from there only to a higher
/// level. The numeric order is the display order of the grid: run, sl, tp1..tp5.
/// </summary>
public enum CryptoSignalStatus
{
    Run = 0,
    Lost = 1,
    Tp1 = 2,
    Tp2 = 3,
    Tp3 = 4,
    Tp4 = 5,
    Tp5 = 6,
}

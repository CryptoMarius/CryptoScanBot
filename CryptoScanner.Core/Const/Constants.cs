namespace CryptoScanner.Core.Const;

public static class Constants
{
    public const string AppName = "CryptoScanBot";

    public const int BarometerGraphHours = 7;

    public const string SymbolNameBarometerPrice = "$BMP"; // Price barometer
    //public const string SymbolNameBarometerVolume = "$BMV"; // Volume barometer an experiment, needs to be continued someday

    // A candle holds five numbers (open/high/low/close/volume) and the barometer produces more than
    // that, so the measurement is spread over two symbols. Both are written in the same pass from the
    // same measurement; see BarometerCandleFields for which figure lives where.
    public const string SymbolNameBarometerExtra = "$BMX"; // Second page of the price barometer

    /// <summary>
    /// How often a replay writes a barometer measurement that belongs to no position, so a run keeps
    /// its own market context (see CryptoBarometerSnapshot). Once an hour over seven months is about
    /// 5.000 measurements per quote coin per interval, which is nothing next to a database that
    /// counts in gigabytes - and one per minute would be sixty times that for a figure that moves
    /// slowly on the intervals it is measured over.
    /// </summary>
    public const int BarometerHeartbeatMinutes = 60;

}
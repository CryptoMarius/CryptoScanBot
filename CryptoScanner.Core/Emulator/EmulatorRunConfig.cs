namespace CryptoScanner.Core.Emulator;

/// <summary>
/// Configuration for a single emulator run. Strategies, active intervals, trend filters and
/// every other tuning knob live in <c>GlobalData.Settings</c> (the regular scanner settings) —
/// the same JSON the live scanner uses. We just need to know which symbols to replay and over
/// what period. The full settings snapshot at run start is captured in
/// <see cref="SettingsSnapshotJson"/> so the run remains reproducible even after the user
/// changes settings later.
/// </summary>
public class EmulatorRunConfig
{
    /// <summary>Exchange to source candles from (display name, e.g. "Binance Spot").</summary>
    public string ExchangeName { get; set; } = "";

    /// <summary>Symbol names to replay (e.g. ["BTCUSDT", "ETHUSDT"]). Order is irrelevant.</summary>
    public List<string> Symbols { get; set; } = [];

    /// <summary>
    /// Inclusive UTC start of the replay window. Higher-interval candles are aggregated from
    /// the 1m driving interval; enough 1m history is loaded before this date to fill the
    /// longest indicator lookback on the longest active interval.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>Inclusive UTC end of the replay window.</summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Snapshot of <c>GlobalData.Settings</c> serialised to JSON at run start. Captured into
    /// <see cref="Model.CryptoEmulatorRun.ConfigJson"/> so the exact strategy set, intervals,
    /// indicator parameters and trend filters used by this run can always be retrieved later.
    /// Null is allowed (engine reads current settings directly), but persisted runs should
    /// always have it set.
    /// </summary>
    public string? SettingsSnapshotJson { get; set; }

    /// <summary>
    /// Free-form label so the operator can spot a run in the EmulatorRun table without
    /// reading the full settings snapshot. Optional.
    /// </summary>
    public string Label { get; set; } = "";
}

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Configuration for a single emulator run. Strategies, active intervals, trend filters and
/// every other tuning knob live in <c>GlobalData.Settings</c> (the regular scanner settings) —
/// the same JSON the live scanner uses. We just need to know which symbols to replay and over
/// what period. The full settings snapshot at run start is captured separately in
/// <see cref="CryptoScanner.Core.Model.CryptoEmulatorRun.SettingsJson"/>, so it deliberately does NOT live here.
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
    /// Free-form label so the operator can spot a run in the EmulatorRun table without
    /// reading the full settings snapshot. Optional.
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// The algorithm names last picked in the "Run algorithms..." / sweep selection dialog.
    /// Persisted so the dialog can restore the previous choice instead of defaulting to all
    /// selected every time. Empty means "no prior choice" — the dialog then selects everything.
    /// </summary>
    public List<string> SelectedAlgorithms { get; set; } = [];

    /// <summary>
    /// Column header text of the last user-chosen sort in the Results grid.
    /// Null/empty means default sort (StartedAt descending).
    /// </summary>
    public string? SortColumn { get; set; }

    /// <summary>True when the saved sort is descending.</summary>
    public bool SortDescending { get; set; }

}

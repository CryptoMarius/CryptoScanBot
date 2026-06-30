using CryptoScanner.Core.Settings;

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
    /// Stop-loss percentages to sweep during a parameter sweep run (e.g. [1, 2, 3, 4, 5, 6]).
    /// Only used by the "Run all algorithms" sweep button.
    /// </summary>
    public List<decimal> StopLossPercentages { get; set; } = [1m, 2m, 3m, 4m, 5m, 6m];

    /// <summary>
    /// DCA variants to sweep during a parameter sweep run. Each entry is a complete DCA ladder
    /// (list of DCA steps). An empty inner list means "no DCA". Only used by the sweep button.
    /// </summary>
    public List<List<CryptoDcaEntry>> DcaVariants { get; set; } =
    [
        [], // no DCA
        [new CryptoDcaEntry { Factor = 200m, Percentage = 3.0m }],
        [
            new CryptoDcaEntry { Factor = 200m, Percentage = 3.0m },
            new CryptoDcaEntry { Factor = 400m, Percentage = 6.0m },
        ],
        [new CryptoDcaEntry { Factor = 200m, Percentage = 6.0m }],
        [
            new CryptoDcaEntry { Factor = 200m, Percentage = 6.0m },
            new CryptoDcaEntry { Factor = 400m, Percentage = 12.0m },
        ],
    ];
}

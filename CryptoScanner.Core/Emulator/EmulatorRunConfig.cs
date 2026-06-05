using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Emulator;

/// <summary>
/// Configuration for a single emulator run — everything the engine needs to know to replay
/// candles deterministically. Serialised as JSON into <see cref="Model.CryptoEmulatorRun.ConfigJson"/>
/// at run-start so the exact inputs can always be retrieved later, even when the live settings
/// have changed.
/// </summary>
public class EmulatorRunConfig
{
    /// <summary>Exchange to source candles from (display name, e.g. "Binance Spot").</summary>
    public string ExchangeName { get; set; } = "";

    /// <summary>Symbol names to replay (e.g. ["BTCUSDT", "ETHUSDT"]). Order is irrelevant.</summary>
    public List<string> Symbols { get; set; } = [];

    /// <summary>
    /// Driving interval — the timeline along which the TickRunner advances. Typically the
    /// shortest active interval (1m). Higher-timeframe candles are picked up via aggregation
    /// just as the live scanner does.
    /// </summary>
    public string DrivingInterval { get; set; } = "1m";

    /// <summary>
    /// Inclusive UTC start of the replay window. Indicators are warmed up on candles before
    /// this date so the first replayed bar already has stable values.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>Inclusive UTC end of the replay window.</summary>
    public DateTime ToDate { get; set; }

    /// <summary>Strategies enabled for the long side during this run.</summary>
    public List<CryptoSignalStrategy> StrategiesLong { get; set; } = [];

    /// <summary>Strategies enabled for the short side during this run.</summary>
    public List<CryptoSignalStrategy> StrategiesShort { get; set; } = [];

    /// <summary>
    /// Optional override snapshot of <c>GlobalData.Settings</c> (serialised JSON). When non-null
    /// the engine applies it for the duration of the run instead of the user's live settings.
    /// Use cases: parameter sweeps, reproducible experiments. Null means "use whatever
    /// settings.json is in this emulator folder right now".
    /// </summary>
    public string? SettingsOverrideJson { get; set; }

    /// <summary>
    /// Free-form label so the operator can spot a run in the EmulatorRun table without
    /// reading ConfigJson. Optional.
    /// </summary>
    public string Label { get; set; } = "";
}

using CryptoScanner.Core.Settings;

using System.Text.Json;

namespace CryptoScanner.Emulator.Engine;


/// <summary>
/// One item in the emulator queue file. Each entry describes a single run with explicit SL, TP
/// and DCA parameters — no matrix explosion. Future fields (e.g. interval overrides, indicator
/// tweaks) can be added here without breaking existing queue files.
/// </summary>
public class EmulatorQueueEntry
{
    /// <summary>Optional free-form label; used as part of the run label in the Results tab.</summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// When set, this entry only runs for the named algorithm
    /// When empty/null, the entry runs for every selected algorithm.
    /// </summary>
    public string? Algorithm { get; set; }

    /// <summary>Stop-loss percentage for this run (e.g. 2.5).</summary>
    public decimal StopLossPercentage { get; set; } = 2m;

    /// <summary>Take-profit levels for this run. Empty list = use the scanner's default.</summary>
    public List<CryptoTpEntry> TpList { get; set; } = [];

    /// <summary>DCA ladder for this run. Empty list = no DCA.</summary>
    public List<CryptoDcaEntry> DcaList { get; set; } = [];

    /// <summary>
    /// Signal parameter overrides for this run. Outer key = settings section name on
    /// SettingsSignal, inner key = property name, value = the value to set. 
    /// Empty or omitted = no signal overrides.
    /// </summary>
    public Dictionary<string, Dictionary<string, JsonElement>> SignalOverrides { get; set; } = new();

    /// <summary>
    /// Trading parameter overrides for this run. Key = property name on SettingsTrading
    /// (e.g. "EntryOrderType"), value = the value to set. Empty or omitted = no trading overrides.
    /// </summary>
    public Dictionary<string, JsonElement> TradingOverrides { get; set; } = new();
}

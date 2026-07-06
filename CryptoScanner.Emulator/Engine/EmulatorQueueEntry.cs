using CryptoScanner.Core.Settings;

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

    /// <summary>Stop-loss percentage for this run (e.g. 2.5).</summary>
    public decimal StopLossPercentage { get; set; } = 2m;

    /// <summary>Take-profit levels for this run. Empty list = use the scanner's default.</summary>
    public List<CryptoTpEntry> TpList { get; set; } = [];

    /// <summary>DCA ladder for this run. Empty list = no DCA.</summary>
    public List<CryptoDcaEntry> DcaList { get; set; } = [];
}

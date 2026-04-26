namespace CryptoScanner.Core.Settings.Strategy;

// Box = the high/low range of the previous full 1d candle. A signal fires when the
// price on the active interval breaks above the previous-day high (long) or below
// the previous-day low (short).
[Serializable]
public class SettingsSignalStrategyBox : SettingsSignalStrategyBase
{
    // Optional noise filter: rejects boxes whose height (top-bottom as % of bottom)
    // is below this threshold. Set to 0 to disable.
    public decimal MinBoxHeightPercent { get; set; } = 0m;

    public SettingsSignalStrategyBox() : base()
    {
    }
}

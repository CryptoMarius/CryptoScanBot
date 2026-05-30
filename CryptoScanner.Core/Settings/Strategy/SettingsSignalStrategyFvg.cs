namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyFvg : SettingsSignalStrategyBase
{
    public List<string> IntervalList { get; set; } = [];

    public double MinimumPercentage { get; set; } = 0.25;

    // How far outside the zone edge (in %) the candle low/high may still be for the combined
    // Stobb+FVG / StoRsi+FVG signals to qualify. Kept separate from WarnPercentage (which does
    // not exist here) so the two purposes do not interfere.
    public decimal NearZonePercentage { get; set; } = 0.25m;

    // Maximum number of wick-touches before a zone is considered exhausted and closed.
    // Supply/demand theory: 0=fresh, 1=tested, 2=weakening, 3+=avoid. Default 2 keeps the
    // first retest signal but suppresses everything after that. Set to 0 to disable
    // touch-based closure (zones only close on body break through the far side).
    public int MaxTouches { get; set; } = 2;

    // How many candles back (including the current one) the rejection check may inspect.
    // 1 = only the current candle must show the test+close-back-outside pattern.
    // 2 = a previous candle may have done the wick, with the current candle as confirmation close.
    public int RejectionLookback { get; set; } = 2;

    // ICT consequent encroachment: when true, a zone is disqualified for new combined-signals
    // once price has pierced past its 50% midpoint, even if TouchCount has not yet hit MaxTouches.
    public bool DisqualifyOnMitigation { get; set; } = false;


    public SettingsSignalStrategyFvg() : base()
    {
        SoundFileLong = "sound-fvg-oversold.wav";
        SoundFileShort = "sound-fvg-overbought.wav";

        IntervalList.Add("1h");
        IntervalList.Add("4h");
        IntervalList.Add("1d");
    }
}
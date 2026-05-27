using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyZones : SettingsSignalStrategyBase
{
    // Defaults for zigzag calculation
    public SettingsZigZag ZigZag { get; set; } = new(false, TrendType.Primary);

    // Show signals from
    public List<string> IntervalList { get; set; } = [];

    public int CandleCount { get; set; } = 500; // 3000; // 3000=150 day's back, 500=20.8 day's
    public int CandleCountZoom { get; set; } = 125;

    // Limits unzoomed box
    public bool ZonesApplyUnzoomed { get; set; } = false;
    public double MinimumUnZoomedPercentage { get; set; } = 0.0;
    public double MaximumUnZoomedPercentage { get; set; } = 0.0;

    // Limits zoomed box
    public bool ZoomLowerTimeFrames { get; set; } = true;
    public double MinimumZoomedPercentage { get; set; } = 0.2;
    public double MaximumZoomedPercentage { get; set; } = 0.7;

    // Signal percentage — used by SignalDominantLevelNearLong/Short for the "approaching zone" alarm.
    public decimal WarnPercentage { get; set; } = 0.25m;

    // How far outside the zone edge (in %) the candle low/high may still be for the combined
    // Stobb+DLZ / StoRsi+DLZ signals to qualify. Separate from WarnPercentage so the two
    // purposes (alarm timing vs. combined-signal proximity) can be tuned independently.
    public decimal NearZonePercentage { get; set; } = 0.25m;

    // Maximum number of wick-touches before a zone is considered exhausted and closed.
    // Supply/demand theory: 0=fresh, 1=tested, 2=weakening, 3+=avoid. Default 2 keeps the
    // first retest signal but suppresses everything after that. Set to 0 to disable
    // touch-based closure (zones only close on body break through the far side).
    public int MaxTouches { get; set; } = 2;

    // How many candles back (including the current one) the rejection check may inspect.
    // 1 = only the current candle must show the test+close-back-outside pattern.
    // 2 = a previous candle may have done the wick, with the current candle as confirmation close.
    public int RejectionLookback { get; set; } = 1;

    // ICT consequent encroachment: when true, a zone is disqualified for new combined-signals
    // once price has pierced past its 50% midpoint, even if TouchCount has not yet hit MaxTouches.
    public bool DisqualifyOnMitigation { get; set; } = false;

    // Filter on start
    public bool ZoneStartApply { get; set; } = false;
    public int ZoneStartCandleCount { get; set; } = 5; // 5 candles back
    public double ZoneStartPercentage { get; set; } = 2.5; // %


    public SettingsSignalStrategyZones() : base()
    {
        SoundFileLong = "sound-dlz-oversold.wav";
        SoundFileShort = "sound-dlz-overbought.wav";

        IntervalList.Add("1h");
    }

}
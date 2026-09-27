using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class ChochSettings : SettingsSignalStrategyBase
{
    // The signal classes skip everything under 5m ("the very noisy lower timeframes"). Saying so
    // here greys those intervals out in the interval picker, where before 1m and 3m were offered
    // and then produced zero signals without a word (open point 110).
    public override CryptoIntervalPeriod MinimumInterval => CryptoIntervalPeriod.interval5m;

    // When true the pullback variants require a BOS (Break of Structure) confirmation
    // in the new trend direction AFTER the CHoCH event before the signal fires.
    // Flow: CHoCH → pullback pivot → BOS confirms new trend → candle breaks pivot → signal.
    [SettingCaption("Require a Break of Structure confirmation",
        Tooltip = "Only for the pullback variants: after the change of character the new trend "
            + "direction first has to be confirmed by a Break of Structure before the signal fires. "
            + "Flow: CHoCH, pullback pivot, BOS confirms the new trend, candle breaks the pivot, signal.")]
    public bool RequireBosConfirmation { get; set; } = false;


    public ChochSettings() : base()
    {
        SoundFileLong = "sound-choch-oversold.wav";
        SoundFileShort = "sound-choch-overbought.wav";
    }

}

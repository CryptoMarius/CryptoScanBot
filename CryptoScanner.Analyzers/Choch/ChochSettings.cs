namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class ChochSettings : SettingsSignalStrategyBase
{
    // When true the pullback variants require a BOS (Break of Structure) confirmation
    // in the new trend direction AFTER the CHoCH event before the signal fires.
    // Flow: CHoCH → pullback pivot → BOS confirms new trend → candle breaks pivot → signal.
    public bool RequireBosConfirmation { get; set; } = false;


    public ChochSettings() : base()
    {
        SoundFileLong = "sound-choch-oversold.wav";
        SoundFileShort = "sound-choch-overbought.wav";
    }

}

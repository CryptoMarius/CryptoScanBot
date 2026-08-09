using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Jump;

[Serializable]
public class JumpSettings : SettingsSignalStrategyBase
{
    // NOTE: the declaration order is the order on screen and follows StrategyJumpSettingsView.axaml.
    // Serialization is by name, so moving a property does not affect an existing settings file.

    [SettingCaption("Jump percentage")]
    public decimal CandlePercentage { get; set; } = 4m;

    [SettingCaption("Candle lookback")]
    public int CandlesLookbackCount { get; set; } = 5;

    [SettingCaption("Use High/Low instead of Open/Close")]
    public bool UseLowHighCalculation { get; set; } = false;

    public JumpSettings() : base()
    {
        SoundFileLong = "sound-jump-up.wav";
        SoundFileShort = "sound-jump-down.wav";
    }

}
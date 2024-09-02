namespace CryptoScanBot.Core.Settings.Strategy;

public class SettingsSignalStrategyStoRsi : SettingsSignalStrategyBase
{
    public int AddRsiAmount { get; set; } = 0;
    public int AddStochAmount { get; set; } = 0;
    public bool CheckBollingerBandsCondition { get; set; } = false;

    public SettingsSignalStrategyStoRsi() : base()
    {
        SoundFileLong = "sound-storsi-oversold.wav";
        SoundFileShort = "sound-storsi-overbought.wav";
    }

}
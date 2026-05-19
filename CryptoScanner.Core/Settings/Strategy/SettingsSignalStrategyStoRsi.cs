namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyStoRsi : SettingsSignalStrategyBase
{
    public double BBMinPercentage { get; set; } = 1.50;
    public double BBMaxPercentage { get; set; } = 100.0;

    public int AddRsiAmount { get; set; } = 0;

    public bool CheckBollingerBandsCondition { get; set; } = false;
    public bool CheckMacdRecovery { get; set; } = false;
    public bool OnlyIfLux5m { get; set; } = false;
    public bool SkipFirstSignal { get; set; } = false;

    public bool CheckTrendPrimaryDirection { get; set; } = false;
    public int TrendPrimaryDirectionCount { get; set; } = 2;
    public bool CheckTrendSecondaryDirection { get; set; } = false;
    public int TrendSecondaryDirectionCount { get; set; } = 2;

    public SettingsSignalStrategyStoRsi() : base()
    {
        SoundFileLong = "sound-storsi-oversold.wav";
        SoundFileShort = "sound-storsi-overbought.wav";
    }

}
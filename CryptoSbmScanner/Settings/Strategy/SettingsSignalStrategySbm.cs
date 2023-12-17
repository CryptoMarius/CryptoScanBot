namespace CryptoSbmScanner.Settings.Strategy;

public class SettingsSignalStrategySbm : SettingsSignalStrategyBase
{
    // SBM1 signals
    public int Sbm1CandlesLookbackCount { get; set; } = 1;

    // SBM2 signals
    public int Sbm2CandlesLookbackCount { get; set; } = 2;
    public decimal Sbm2BbPercentage { get; set; } = 2.5m;
    public bool Sbm2UseLowHigh { get; set; } = false;

    // SBM3 signals
    public int Sbm3CandlesLookbackCount { get; set; } = 8;
    public decimal Sbm3CandlesBbRecoveryPercentage { get; set; } = 225m;


    // SBM algemene 
    public int CandlesForMacdRecovery { get; set; } = 2;

    public decimal Ma200AndMa50Percentage { get; set; } = 0.25m;
    public decimal Ma50AndMa20Percentage { get; set; } = 0.25m;
    public decimal Ma200AndMa20Percentage { get; set; } = 0.50m;

    public bool Ma200AndMa50Crossing { get; set; } = true;
    public int Ma200AndMa50Lookback { get; set; } = 30;
    public bool Ma50AndMa20Crossing { get; set; } = true;
    public int Ma50AndMa20Lookback { get; set; } = 10;
    public bool Ma200AndMa20Crossing { get; set; } = true;
    public int Ma200AndMa20Lookback { get; set; } = 15;

    // Het BB percentage kan via de user interface uit worden gezet (nomargin)
    public double BBMinPercentage { get; set; } = 1.50;
    public double BBMaxPercentage { get; set; } = 100.0;
    public bool UseLowHigh { get; set; } = false;

    public SettingsSignalStrategySbm() : base()
    {
        SoundFileLong = "sound-sbm-oversold.wav";
        SoundFileShort = "sound-sbm-overbought.wav";
    }

}
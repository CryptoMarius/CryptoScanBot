using CryptoScanBot.Settings.Strategy;

namespace CryptoScanBot.Settings;

[Serializable]
public class SettingsSignal
{
    // Naar general wellicht? (want het geld voor alles)
    public bool SoundsActive { get; set; } = true;

    // Is het signal algoritme actief
    public bool Active { get; set; } = true;

    // Aantal dagen dat de munt moet bestaan
    public int SymbolMustExistsDays { get; set; } = 60;
    public bool LogSymbolMustExistsDays { get; set; } = false;

    // Vermijden van "barcode" charts
    public decimal MinimumTickPercentage { get; set; } = 0.25m;
    public bool LogMinimumTickPercentage { get; set; } = false;

    // de 24 change moet binnen dit interval zitten (niet presentatief)
    public double AnalysisMinChangePercentage { get; set; } = -25;
    public double AnalysisMaxChangePercentage { get; set; } = 25;
    public bool LogAnalysisMinMaxChangePercentage { get; set; } = false;

    // de 24 effectief moet binnen dit interval zitten (de echte beweging)
    public double AnalysisMinEffectivePercentage { get; set; } = -40;
    public double AnalysisMaxEffectivePercentage { get; set; } = 40;
    public bool LogAnalysisMinMaxEffectivePercentage { get; set; } = false;

    // de 10 dagen effectief moet binnen dit interval zitten (de echte beweging)
    public double AnalysisMinEffective10DaysPercentage { get; set; } = -75;
    public double AnalysisMaxEffective10DaysPercentage { get; set; } = 75;
    public bool LogAnalysisMinMaxEffective10DaysPercentage { get; set; } = false;

    // STOBB signals
    public SettingsSignalStrategyStobb Stobb = new();

    // SBM signals
    public SettingsSignalStrategySbm Sbm = new();

    // STORSI
    public SettingsSignalStrategyStoRsi StoRsi = new();

    // JUMP
    public SettingsSignalStrategyJump Jump = new();


    // Logging
    public bool LogMinimalVolume { get; set; } = false;
    public bool LogMinimalPrice { get; set; } = false;
    public bool LogNotEnoughCandles { get; set; }

    // Fine tuning (later)
    public int AboveBollingerBandsSma { get; set; } = 1;
    public bool AboveBollingerBandsSmaCheck { get; set; } = false;

    // Fine tuning (later)
    public int AboveBollingerBandsUpper { get; set; } = 1;
    public bool AboveBollingerBandsUpperCheck { get; set; } = false;

    // Fine tuning (later)
    // Candles zonder volume
    public int CandlesWithZeroVolume { get; set; } = 20;
    public bool CandlesWithZeroVolumeCheck { get; set; } = false;

    // Fine tuning (later)
    // De zogenaamde platte candles
    public int CandlesWithFlatPrice { get; set; } = 20;
    public bool CandlesWithFlatPriceCheck { get; set; } = false;


    // Op welke intervallen, strategieën, trend, barometer willen we analyseren?
    public SettingsTextual Long { get; set; } = new();
    public SettingsTextual Short { get; set; } = new();


    public SettingsSignal()
    {
        Long.Barometer.List.Add("1h", (-1.5m, 999m));
        Short.Barometer.List.Add("1h", (-999m, 1.5m));

        // Geeft vragen.. (aansluiting oude scanner)
        //Long.IntervalTrend.List.Add("1h");
        //Short.IntervalTrend.List.Add("1h");

        //Long.MarketTrend.List.Add((0m, 100m));
        //Short.MarketTrend.List.Add((-100m, 0));
    }

}

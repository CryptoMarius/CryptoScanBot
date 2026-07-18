using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Core.Settings;

[Serializable]
public class SettingsSignal
{
    // Naar general wellicht? (want het geld voor alles)
    public bool SoundsActive { get; set; } = true;

    // Is het signal algoritme actief
    public bool Active { get; set; } = true;

    // Use the incremental Skender v3 QuoteHub per symbol+interval (IntervalIndicatorHub) instead of the
    // per-candle batch recompute. Both fill CryptoSymbolInterval.Data identically (verified field-for-field);
    // the hub is far cheaper for the tick-by-tick replay. Toggle for A/B comparison and safe fallback.
    public bool UseIndicatorHub { get; set; } = false;

    // Aantal dagen dat de munt moet bestaan
    public int SymbolMustExistsDays { get; set; } = 60;
    public bool LogSymbolMustExistsDays { get; set; } = false;
    public bool CheckVolumeOverPeriod { get; set; } = false;
    public int CheckVolumeOverDays { get; set; } = 10;

    // Vermijden van "barcode" charts
    public decimal MinimumTickPercentage { get; set; } = 0.25m;
    public bool LogMinimumTickPercentage { get; set; } = false;

    // de 24 change moet binnen dit interval zitten (start/end price)
    public float AnalysisMinChangePercentage { get; set; } = -25;
    public float AnalysisMaxChangePercentage { get; set; } = 25;
    public bool LogAnalysisMinMaxChangePercentage { get; set; } = false;

    // de x dagen effectief moet binnen dit interval zitten (full effective move)
    public int AnalysisEffectiveDays { get; set; } = 5;
    public float AnalysisEffectivePercentage { get; set; } = 35;
    public bool AnalysisMaxEffectiveLog { get; set; } = false;

    // STOBB signals
    public SettingsSignalStrategyStobb Stobb = new();

    // SBM signals
    public SettingsSignalStrategySbm Sbm = new();

    // STORSI
    public SettingsSignalStrategyStoRsi StoRsi = new();

    // JUMP
    public SettingsSignalStrategyJump Jump = new();

    // Dominant zones
    public SettingsSignalStrategyZones ZonesDlz = new();

    // Fair Value gap zones
    public SettingsSignalStrategyFvg ZonesFvg = new();

    // SMC (Smart Money Concepts) supply/demand order blocks
    public SettingsSignalStrategySmc ZonesSmc = new();

    // Nadaraya Watson Envelope
    public SettingsSignalStrategyNwe Nwe = new();

#if DEBUG
    // BBMA (Oma Ally)
    public SettingsSignalStrategyBbma Bbma = new();

    // CHoCH (Change of Character) — ZigZag structure reversal signals
    public SettingsSignalStrategyChoch Choch = new();
#endif

    // Baba, AtrRb and Bre settings have been migrated to the Analyzers plugin architecture
    // and are now managed by PluginManager (BabaPlugin.Settings, AtrRbPlugin.Settings, BrePlugin.Settings).

    // Analyzer plugin settings (keyed by plugin name, e.g. "demo")
    public Dictionary<string, SettingsSignalStrategyBase> AnalyzerSettings { get; set; } = [];

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

        // Migrations issues.. Nah..
        //Long.CryptoTrendData.List.Add("1h");
        //Short.CryptoTrendData.List.Add("1h");

        //Long.MarketTrend.List.Add((0m, 100m));
        //Short.MarketTrend.List.Add((-100m, 0));
    }
}

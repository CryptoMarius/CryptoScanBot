using System.Text.Json.Serialization;

using CryptoSbmScanner.Enums;

namespace CryptoSbmScanner.Settings;

[Serializable]
public class SettingsSignal
{
    // Naar general wellicht? (want het geld voor alles)
    public bool SoundsActive { get; set; } = true;

    /// Is het signal algoritme actief
    // TODO rename naar Active
    public bool SignalsActive { get; set; } = true;

    public bool ShowInvalidSignals { get; set; } = false;

    // Minimale 1h barometer om de meldingen te tonen (van - 1.5 tot hoger of iets dergelijks, en te hoog (>5) is eigenlijk ook niet goed)
    public decimal Barometer1hMinimal { get; set; } = -99m;
    public bool LogBarometerToLow { get; set; } = false;

    // Aantal dagen dat de munt moet bestaan
    public int SymbolMustExistsDays { get; set; } = 15;
    public bool LogSymbolMustExistsDays { get; set; } = false;

    // Vermijden van "barcode" charts
    public decimal MinimumTickPercentage { get; set; } = 0.4m;
    public bool LogMinimumTickPercentage { get; set; } = false;

    // de 24 change moet binnen dit interval zitten (niet presentatief)
    public double AnalysisMinChangePercentage { get; set; } = -25;
    public double AnalysisMaxChangePercentage { get; set; } = 25;
    public bool LogAnalysisMinMaxChangePercentage { get; set; } = true;

    // de 24 effectief moet binnen dit interval zitten (de echte beweging)
    public double AnalysisMinEffectivePercentage { get; set; } = -40;
    public double AnalysisMaxEffectivePercentage { get; set; } = 40;
    public bool LogAnalysisMinMaxEffectivePercentage { get; set; } = true;

    // STOBB signals
    // Het BB percentage kan via de user interface uit worden gezet (nomargin)
    public double StobbBBMinPercentage { get; set; } = 1.50;
    public double StobbBBMaxPercentage { get; set; } = 5.0;
    public bool StobbUseLowHigh { get; set; } = false;

    [JsonConverter(typeof(Intern.ColorConverter))]
    public Color ColorStobb { get; set; } = Color.White;
    public bool PlaySoundStobbSignal { get; set; } = false;
    public bool PlaySpeechStobbSignal { get; set; } = false;
    public string SoundStobbOversold { get; set; } = "sound-stobb-oversold.wav";
    public string SoundStobbOverbought { get; set; } = "sound-stobb-overbought.wav";
    public bool StobIncludeRsi { get; set; } = false;
    public bool StobIncludeSoftSbm { get; set; } = false;
    public bool StobIncludeSbmPercAndCrossing { get; set; } = false;
    public decimal StobMinimalTrend { get; set; } = -999m;

    // SBM1 signals
    // Het BB percentage kan via de user interface uit worden gezet (nomargin)
    public double SbmBBMinPercentage { get; set; } = 1.50;
    public double SbmBBMaxPercentage { get; set; } = 100.0;
    public bool SbmUseLowHigh { get; set; } = false;

    [JsonConverter(typeof(Intern.ColorConverter))]
    public Color ColorSbm { get; set; } = Color.White;
    public bool PlaySoundSbmSignal { get; set; } = true;
    public bool PlaySpeechSbmSignal { get; set; } = true;
    public string SoundSbmOversold { get; set; } = "sound-sbm-oversold.wav";
    public string SoundSbmOverbought { get; set; } = "sound-sbm-overbought.wav";
    public int Sbm1CandlesLookbackCount { get; set; } = 1;

    // SBM2 signals
    public int Sbm2CandlesLookbackCount { get; set; } = 2;
    public decimal Sbm2BbPercentage { get; set; } = 5m;
    public bool Sbm2UseLowHigh { get; set; } = false;

    // SBM3 signals
    public int Sbm3CandlesLookbackCount { get; set; } = 8;
    public decimal Sbm3CandlesBbRecoveryPercentage { get; set; } = 225m;

    // SBM algemene instellingen recovery, percentages, crossing && lookback
    public int SbmCandlesForMacdRecovery { get; set; } = 2;

    public decimal SbmMa200AndMa50Percentage { get; set; } = 0.3m;
    public decimal SbmMa50AndMa20Percentage { get; set; } = 0.3m;
    public decimal SbmMa200AndMa20Percentage { get; set; } = 0.7m;

    public bool SbmMa200AndMa50Crossing { get; set; } = true;
    public int SbmMa200AndMa50Lookback { get; set; } = 20;
    public bool SbmMa50AndMa20Crossing { get; set; } = true;
    public int SbmMa50AndMa20Lookback { get; set; } = 10;
    public bool SbmMa200AndMa20Crossing { get; set; } = true;
    public int SbmMa200AndMa20Lookback { get; set; } = 20;


    // JUMP
    [JsonConverter(typeof(Intern.ColorConverter))]
    public Color ColorJump { get; set; } = Color.White;
    public bool PlaySoundCandleJumpSignal { get; set; } = false;
    public bool PlaySpeechCandleJumpSignal { get; set; } = false;
    public bool JumpUseLowHighCalculation { get; set; } = false;
    public int JumpCandlesLookbackCount { get; set; } = 1;
    public string SoundCandleJumpDown { get; set; } = "sound-jump-down.wav";
    public string SoundCandleJumpUp { get; set; } = "sound-jump-up.wav";
    public decimal AnalysisCandleJumpPercentage { get; set; } = 2.5m;


    // Logging
    public bool LogMinimalVolume { get; set; } = false;
    public bool LogMinimalPrice { get; set; } = false;
    public bool LogNotEnoughCandles { get; set; }

    // Fine tuning (later)
    public int AboveBollingerBandsSma { get; set; } = 0;
    public bool AboveBollingerBandsSmaCheck { get; set; } = false;

    // Fine tuning (later)
    public int AboveBollingerBandsUpper { get; set; } = 0;
    public bool AboveBollingerBandsUpperCheck { get; set; } = false;

    // Fine tuning (later)
    // Candles zonder volume
    public int CandlesWithZeroVolume { get; set; } = 0;
    public bool CandlesWithZeroVolumeCheck { get; set; } = false;

    // Fine tuning (later)
    // De zogenaamde platte candles
    public int CandlesWithFlatPrice { get; set; } = 0;
    public bool CandlesWithFlatPriceCheck { get; set; } = false;


    // Op welke intervallen en strategieën willen we analyseren?
    public IntervalAndStrategyConfig Analyze { get; set; } = new ();


    public SettingsSignal()
    {
        Analyze.Interval.Add("1m");
        Analyze.Interval.Add("2m");

        Analyze.Strategy[CryptoOrderSide.Buy].Add("sbm1");
        Analyze.Strategy[CryptoOrderSide.Buy].Add("sbm2");
        Analyze.Strategy[CryptoOrderSide.Buy].Add("sbm3");
    }
}

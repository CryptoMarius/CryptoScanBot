using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class AnalyzerCommonViewModel : ObservableObject
{
    // Check relative change% 24 hours (groupBox9)
    [ObservableProperty]
    private float _analysisMinChangePercentage = 0;

    [ObservableProperty]
    private float _analysisMaxChangePercentage = 25;

    [ObservableProperty]
    private bool _logAnalysisMinMaxChangePercentage = false;

    // Check effective change% over multiple days (GroupBoxXDaysEffective)
    [ObservableProperty]
    private float _analysisEffectivePercentage = 25;

    [ObservableProperty]
    private int _analysisEffectiveDays = 1;

    [ObservableProperty]
    private bool _analysisMaxEffectiveLog = false;

    // Check volume multiple days (groupBox10)
    [ObservableProperty]
    private bool _checkVolumeOverPeriod = false;

    [ObservableProperty]
    private int _checkVolumeOverDays = 1;

    // Other common settings (add more as needed)
    [ObservableProperty]
    private int _symbolMustExistsDays = 3;

    [ObservableProperty]
    private bool _logSymbolMustExistsDays = false;

    [ObservableProperty]
    private decimal _minimumTickPercentage = 1.5m;

    [ObservableProperty]
    private bool _logMinimumTickPercentage = false;

    [ObservableProperty]
    private int _removeSignalAfterxCandles = 15;

    [ObservableProperty]
    private bool _showInvalidSignals  = false;

    // Fine tuning (later)
    [ObservableProperty]
    private int _aboveBollingerBandsSma = 1;
    [ObservableProperty]
    private bool _aboveBollingerBandsSmaCheck = false;

    // Fine tuning (later)
    [ObservableProperty]
    private int _aboveBollingerBandsUpper = 1;
    [ObservableProperty]
    private bool _aboveBollingerBandsUpperCheck = false;

    // Fine tuning (later)
    // Candles zonder volume
    [ObservableProperty]
    private int _candlesWithZeroVolume = 20;
    [ObservableProperty]
    private bool _candlesWithZeroVolumeCheck = false;

    // Fine tuning (later)
    // De zogenaamde platte candles
    [ObservableProperty]
    private int _candlesWithFlatPrice = 20;
    [ObservableProperty]
    private bool _candlesWithFlatPriceCheck = false;


    public AnalyzerCommonViewModel()
    {
    }

    internal void LoadConfig(SettingsSignal settings)
    {
        AnalysisMinChangePercentage = settings.AnalysisMinChangePercentage;
        AnalysisMaxChangePercentage = settings.AnalysisMaxChangePercentage;
        LogAnalysisMinMaxChangePercentage = settings.LogAnalysisMinMaxChangePercentage;

        AnalysisEffectivePercentage = settings.AnalysisEffectivePercentage;
        AnalysisEffectiveDays = settings.AnalysisEffectiveDays;
        AnalysisMaxEffectiveLog = settings.AnalysisMaxEffectiveLog;

        CheckVolumeOverPeriod = settings.CheckVolumeOverPeriod;
        CheckVolumeOverDays = settings.CheckVolumeOverDays;

        // Other settings
        // TODO: Refactor these two properties
        RemoveSignalAfterxCandles = GlobalData.Settings.General.RemoveSignalAfterxCandles;
        ShowInvalidSignals = GlobalData.Settings.General.ShowInvalidSignals;
        SymbolMustExistsDays = settings.SymbolMustExistsDays;
        LogSymbolMustExistsDays = settings.LogSymbolMustExistsDays;
        MinimumTickPercentage = settings.MinimumTickPercentage;
        LogMinimumTickPercentage = settings.LogMinimumTickPercentage;

        CandlesWithFlatPriceCheck = settings.CandlesWithFlatPriceCheck;
        CandlesWithFlatPrice = settings.CandlesWithFlatPrice;
        CandlesWithZeroVolume = settings.CandlesWithZeroVolume;
        AboveBollingerBandsSmaCheck = settings.AboveBollingerBandsSmaCheck;
        AboveBollingerBandsSma = settings.AboveBollingerBandsSma;
        AboveBollingerBandsUpperCheck = settings.AboveBollingerBandsUpperCheck;
        AboveBollingerBandsUpper = settings.AboveBollingerBandsUpper;
    }

    internal void SaveConfig(SettingsSignal settings)
    {
        settings.AnalysisMinChangePercentage = AnalysisMinChangePercentage;
        settings.AnalysisMaxChangePercentage = AnalysisMaxChangePercentage;
        settings.LogAnalysisMinMaxChangePercentage = LogAnalysisMinMaxChangePercentage;

        settings.AnalysisEffectivePercentage = AnalysisEffectivePercentage;
        settings.AnalysisEffectiveDays = AnalysisEffectiveDays;
        settings.AnalysisMaxEffectiveLog = AnalysisMaxEffectiveLog;

        settings.CheckVolumeOverPeriod = CheckVolumeOverPeriod;
        settings.CheckVolumeOverDays = CheckVolumeOverDays;

        // Other settings
        // TODO: Refactor these two properties
        GlobalData.Settings.General.RemoveSignalAfterxCandles = RemoveSignalAfterxCandles;
        GlobalData.Settings.General.ShowInvalidSignals = ShowInvalidSignals;
        settings.SymbolMustExistsDays = SymbolMustExistsDays;
        settings.LogSymbolMustExistsDays = LogSymbolMustExistsDays;
        settings.MinimumTickPercentage = MinimumTickPercentage;
        settings.LogMinimumTickPercentage = LogMinimumTickPercentage;

        settings.CandlesWithFlatPriceCheck = CandlesWithFlatPriceCheck;
        settings.CandlesWithFlatPrice = CandlesWithFlatPrice;
        settings.CandlesWithZeroVolume = CandlesWithZeroVolume;
        settings.AboveBollingerBandsSmaCheck = AboveBollingerBandsSmaCheck;
        settings.AboveBollingerBandsSma = AboveBollingerBandsSma;
        settings.AboveBollingerBandsUpperCheck = AboveBollingerBandsUpperCheck;
        settings.AboveBollingerBandsUpper = AboveBollingerBandsUpper;
    }
}
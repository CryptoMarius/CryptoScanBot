using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Stobb.Config;

public partial class StrategyStobbSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _bbMinPercentage = 1.50;

    [ObservableProperty]
    private double _bbMaxPercentage = 6.0;

    [ObservableProperty]
    private bool _useLowHigh = false;

    [ObservableProperty]
    private bool _includeRsi = false;

    [ObservableProperty]
    private bool _includeSbmMaLines = false;

    [ObservableProperty]
    private bool _includeSbmPercAndCrossing = false;

    [ObservableProperty]
    private bool _onlyIfPreviousStobb = false;

    public void LoadConfig(StobbSettings settings)
    {
        BbMinPercentage = settings.BBMinPercentage;
        BbMaxPercentage = settings.BBMaxPercentage;
        UseLowHigh = settings.UseLowHigh;
        IncludeRsi = settings.IncludeRsi;
        IncludeSbmMaLines = settings.IncludeSoftSbm;
        IncludeSbmPercAndCrossing = settings.IncludeSbmPercAndCrossing;
        OnlyIfPreviousStobb = settings.OnlyIfPreviousStobb;
    }

    public void SaveConfig(StobbSettings settings)
    {
        settings.BBMinPercentage = BbMinPercentage;
        settings.BBMaxPercentage = BbMaxPercentage;
        settings.UseLowHigh = UseLowHigh;
        settings.IncludeRsi = IncludeRsi;
        settings.IncludeSoftSbm = IncludeSbmMaLines;
        settings.IncludeSbmPercAndCrossing = IncludeSbmPercAndCrossing;
        settings.OnlyIfPreviousStobb = OnlyIfPreviousStobb;
    }
}

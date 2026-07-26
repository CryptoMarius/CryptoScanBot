using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.BbSqueeze.Config;

public partial class StrategyBbSqueezeSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _bbSqueezeMaxPercentage = 2.0;

    [ObservableProperty]
    private int _squeezeMinCandles = 6;

    [ObservableProperty]
    private bool _useMacdFilter = true;

    [ObservableProperty]
    private int _macdConfirmCandles = 2;

    [ObservableProperty]
    private bool _useVolumeFilter = false;

    [ObservableProperty]
    private double _volumeMultiplier = 1.5;

    [ObservableProperty]
    private int _volumeSmaLength = 20;

    [ObservableProperty]
    private int _reSqueezeGraceCandles = 2;

    public void LoadConfig(BbSqueezeSettings settings)
    {
        BbSqueezeMaxPercentage = settings.BBSqueezeMaxPercentage;
        SqueezeMinCandles = settings.SqueezeMinCandles;
        UseMacdFilter = settings.UseMacdFilter;
        MacdConfirmCandles = settings.MacdConfirmCandles;
        UseVolumeFilter = settings.UseVolumeFilter;
        VolumeMultiplier = settings.VolumeMultiplier;
        VolumeSmaLength = settings.VolumeSmaLength;
        ReSqueezeGraceCandles = settings.ReSqueezeGraceCandles;
    }

    public void SaveConfig(BbSqueezeSettings settings)
    {
        settings.BBSqueezeMaxPercentage = BbSqueezeMaxPercentage;
        settings.SqueezeMinCandles = SqueezeMinCandles;
        settings.UseMacdFilter = UseMacdFilter;
        settings.MacdConfirmCandles = MacdConfirmCandles;
        settings.UseVolumeFilter = UseVolumeFilter;
        settings.VolumeMultiplier = VolumeMultiplier;
        settings.VolumeSmaLength = VolumeSmaLength;
        settings.ReSqueezeGraceCandles = ReSqueezeGraceCandles;
    }
}

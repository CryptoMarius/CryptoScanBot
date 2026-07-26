using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.KumoSqueeze.Config;

public partial class StrategyKumoSqueezeSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _bbSqueezeMaxPercentage = 2.0;

    [ObservableProperty]
    private int _squeezeMinCandles = 6;

    [ObservableProperty]
    private bool _useVolumeFilter = true;

    [ObservableProperty]
    private double _volumeMultiplier = 1.5;

    [ObservableProperty]
    private int _volumeSmaLength = 20;

    [ObservableProperty]
    private int _tenkanPeriod = 9;

    [ObservableProperty]
    private int _kijunPeriod = 26;

    [ObservableProperty]
    private int _senkouBPeriod = 52;

    [ObservableProperty]
    private bool _useRsiFilter = true;

    [ObservableProperty]
    private bool _useTenkanKijunFilter = true;

    [ObservableProperty]
    private bool _useMacdFilter = false;

    [ObservableProperty]
    private int _macdConfirmCandles = 2;

    public void LoadConfig(KumoSqueezeSettings settings)
    {
        BbSqueezeMaxPercentage = settings.BBSqueezeMaxPercentage;
        SqueezeMinCandles = settings.SqueezeMinCandles;
        UseVolumeFilter = settings.UseVolumeFilter;
        VolumeMultiplier = settings.VolumeMultiplier;
        VolumeSmaLength = settings.VolumeSmaLength;
        TenkanPeriod = settings.TenkanPeriod;
        KijunPeriod = settings.KijunPeriod;
        SenkouBPeriod = settings.SenkouBPeriod;
        UseRsiFilter = settings.UseRsiFilter;
        UseTenkanKijunFilter = settings.UseTenkanKijunFilter;
        UseMacdFilter = settings.UseMacdFilter;
        MacdConfirmCandles = settings.MacdConfirmCandles;
    }

    public void SaveConfig(KumoSqueezeSettings settings)
    {
        settings.BBSqueezeMaxPercentage = BbSqueezeMaxPercentage;
        settings.SqueezeMinCandles = SqueezeMinCandles;
        settings.UseVolumeFilter = UseVolumeFilter;
        settings.VolumeMultiplier = VolumeMultiplier;
        settings.VolumeSmaLength = VolumeSmaLength;
        settings.TenkanPeriod = TenkanPeriod;
        settings.KijunPeriod = KijunPeriod;
        settings.SenkouBPeriod = SenkouBPeriod;
        settings.UseRsiFilter = UseRsiFilter;
        settings.UseTenkanKijunFilter = UseTenkanKijunFilter;
        settings.UseMacdFilter = UseMacdFilter;
        settings.MacdConfirmCandles = MacdConfirmCandles;
    }
}

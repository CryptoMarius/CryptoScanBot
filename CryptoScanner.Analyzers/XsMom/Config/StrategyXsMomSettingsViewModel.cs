using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.XsMom.Config;

public partial class StrategyXsMomSettingsViewModel : ObservableObject
{
    /// <summary>The values the "rebalance day" ComboBox offers.</summary>
    public static DayOfWeek[] RebalanceDays { get; } = Enum.GetValues<DayOfWeek>();

    [ObservableProperty]
    private int _lookbackDays = 14;

    [ObservableProperty]
    private int _topByVolume = 100;

    [ObservableProperty]
    private int _volumeDays = 30;

    [ObservableProperty]
    private bool _skipTradFi = true;

    [ObservableProperty]
    private int _coinsPerSide = 20;

    [ObservableProperty]
    private decimal _keepWithinFactor = 1m;

    [ObservableProperty]
    private DayOfWeek _rebalanceDay = DayOfWeek.Monday;

    [ObservableProperty]
    private int _rebalanceEveryWeeks = 1;


    public void LoadConfig(XsMomSettings settings)
    {
        LookbackDays = settings.LookbackDays;
        TopByVolume = settings.TopByVolume;
        VolumeDays = settings.VolumeDays;
        SkipTradFi = settings.SkipTradFi;
        CoinsPerSide = settings.CoinsPerSide;
        KeepWithinFactor = settings.KeepWithinFactor;
        RebalanceDay = settings.RebalanceDay;
        RebalanceEveryWeeks = settings.RebalanceEveryWeeks;
    }

    public void SaveConfig(XsMomSettings settings)
    {
        settings.LookbackDays = LookbackDays;
        settings.TopByVolume = TopByVolume;
        settings.VolumeDays = VolumeDays;
        settings.SkipTradFi = SkipTradFi;
        settings.CoinsPerSide = CoinsPerSide;
        settings.KeepWithinFactor = KeepWithinFactor;
        settings.RebalanceDay = RebalanceDay;
        settings.RebalanceEveryWeeks = RebalanceEveryWeeks;
    }
}

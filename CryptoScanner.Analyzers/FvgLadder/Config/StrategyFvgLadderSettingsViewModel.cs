using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.FvgLadder.Config;

public partial class StrategyFvgLadderSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _coins = "BTC";

    [ObservableProperty]
    private bool _useKalmanFilter = false;

    [ObservableProperty]
    private decimal _minimumGapPercentage = 0.4m;

    [ObservableProperty]
    private int _gapsInView = 4;

    [ObservableProperty]
    private decimal _placeOrderWithinPercentage = 5.0m;

    [ObservableProperty]
    private bool _useDca = true;

    [ObservableProperty]
    private int _maxDca = 2;

    [ObservableProperty]
    private decimal _dcaStepPercentage = 1.5m;

    [ObservableProperty]
    private decimal _dcaFvgMaxPercentage = 3.0m;

    [ObservableProperty]
    private decimal _dcaFactor = 2.0m;

    [ObservableProperty]
    private decimal _takeProfitPercentage = 1.0m;


    public void LoadConfig(FvgLadderSettings settings)
    {
        Coins = settings.Coins;
        UseKalmanFilter = settings.UseKalmanFilter;
        MinimumGapPercentage = settings.MinimumGapPercentage;
        GapsInView = settings.GapsInView;
        PlaceOrderWithinPercentage = settings.PlaceOrderWithinPercentage;
        UseDca = settings.UseDca;
        MaxDca = settings.MaxDca;
        DcaStepPercentage = settings.DcaStepPercentage;
        DcaFvgMaxPercentage = settings.DcaFvgMaxPercentage;
        DcaFactor = settings.DcaFactor;
        TakeProfitPercentage = settings.TakeProfitPercentage;
    }

    public void SaveConfig(FvgLadderSettings settings)
    {
        settings.Coins = Coins;
        settings.UseKalmanFilter = UseKalmanFilter;
        settings.MinimumGapPercentage = MinimumGapPercentage;
        settings.GapsInView = GapsInView;
        settings.PlaceOrderWithinPercentage = PlaceOrderWithinPercentage;
        settings.UseDca = UseDca;
        settings.MaxDca = MaxDca;
        settings.DcaStepPercentage = DcaStepPercentage;
        settings.DcaFvgMaxPercentage = DcaFvgMaxPercentage;
        settings.DcaFactor = DcaFactor;
        settings.TakeProfitPercentage = TakeProfitPercentage;
    }
}

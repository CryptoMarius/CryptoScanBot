using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.FvgLadder.Config;

public partial class StrategyFvgLadderSettingsViewModel : ObservableObject
{
    /// <summary>The values the preset ComboBox offers; see FvgLadderPreset.</summary>
    public static FvgLadderPreset[] Presets { get; } = Enum.GetValues<FvgLadderPreset>();

    [ObservableProperty]
    private string _coins = "BTC";

    [ObservableProperty]
    private FvgLadderPreset _preset = FvgLadderPreset.Ladder;

    [ObservableProperty]
    private decimal _minimumGapPercentage = 0.4m;

    [ObservableProperty]
    private decimal _stepPercentage = 1.5m;

    [ObservableProperty]
    private decimal _takeProfitPercentage = 1.0m;

    [ObservableProperty]
    private int _orderZones = 4;

    [ObservableProperty]
    private decimal _placeOrderDistancePercentage = 1.0m;

    [ObservableProperty]
    private decimal _withdrawOrderDistancePercentage = 2.0m;

    [ObservableProperty]
    private int _maxDca = 2;

    [ObservableProperty]
    private decimal _dcaFvgMaxPercentage = 3.0m;

    [ObservableProperty]
    private decimal _dcaFactor = 2.0m;


    public void LoadConfig(FvgLadderSettings settings)
    {
        Coins = settings.Coins;
        Preset = settings.Preset;
        MinimumGapPercentage = settings.MinimumGapPercentage;
        StepPercentage = settings.StepPercentage;
        TakeProfitPercentage = settings.TakeProfitPercentage;
        OrderZones = settings.OrderZones;
        PlaceOrderDistancePercentage = settings.PlaceOrderDistancePercentage;
        WithdrawOrderDistancePercentage = settings.WithdrawOrderDistancePercentage;
        MaxDca = settings.MaxDca;
        DcaFvgMaxPercentage = settings.DcaFvgMaxPercentage;
        DcaFactor = settings.DcaFactor;
    }

    public void SaveConfig(FvgLadderSettings settings)
    {
        settings.Coins = Coins;
        settings.Preset = Preset;
        settings.MinimumGapPercentage = MinimumGapPercentage;
        settings.StepPercentage = StepPercentage;
        settings.TakeProfitPercentage = TakeProfitPercentage;
        settings.OrderZones = OrderZones;
        settings.PlaceOrderDistancePercentage = PlaceOrderDistancePercentage;
        settings.WithdrawOrderDistancePercentage = WithdrawOrderDistancePercentage;
        settings.MaxDca = MaxDca;
        settings.DcaFvgMaxPercentage = DcaFvgMaxPercentage;
        settings.DcaFactor = DcaFactor;
    }
}

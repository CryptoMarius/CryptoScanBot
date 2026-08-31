using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.CandlePattern.Config;

public partial class StrategyCandlePatternSettingsViewModel : ObservableObject
{
    /// <summary>
    /// The shapes this strategy fires on. Shared with the entry conditions, which pick from the very
    /// same list for the shape an entry waits for.
    /// </summary>
    [ObservableProperty]
    private CandlePatternListViewModel _patternListViewModel;

    /// <summary>The thresholds those shapes are measured against, shared for the same reason.</summary>
    [ObservableProperty]
    private CandlePatternShapeViewModel _shapeViewModel;

    [ObservableProperty]
    private int _precedingCandles = 3;

    [ObservableProperty]
    private decimal _precedingPercentage = 0m;

    public StrategyCandlePatternSettingsViewModel()
    {
        _patternListViewModel = new();
        _shapeViewModel = new();
    }


    public void LoadConfig(CandlePatternStrategySettings settings)
    {
        PatternListViewModel.LoadConfig(settings.Patterns);
        ShapeViewModel.LoadConfig(settings.Shape);

        PrecedingCandles = settings.PrecedingCandles;
        PrecedingPercentage = settings.PrecedingPercentage;
    }

    public void SaveConfig(CandlePatternStrategySettings settings)
    {
        settings.Patterns = PatternListViewModel.SaveConfig();
        ShapeViewModel.SaveConfig(settings.Shape);

        settings.PrecedingCandles = PrecedingCandles;
        settings.PrecedingPercentage = PrecedingPercentage;
    }
}

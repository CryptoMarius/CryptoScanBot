using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;
      
namespace CryptoScanner.Config.ViewModels;

public partial class FeedbackFilterViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isActive = false;

    [ObservableProperty]
    private int _maxLookbackDays = 7;

    [ObservableProperty]
    private int _minSignals = 5;

    [ObservableProperty]
    private decimal _blockThresholdPercent = 40m;

    [ObservableProperty]
    private int _reEnableHours = 24;

    [ObservableProperty]
    private bool _log = true;

    public void LoadConfig(SettingsTextualFeedback settings)
    {
        IsActive = settings.IsActive;
        MaxLookbackDays = settings.MaxLookbackDays;
        MinSignals = settings.MinSignals;
        BlockThresholdPercent = settings.BlockThresholdPercent;
        ReEnableHours = settings.ReEnableHours;
        Log = settings.Log;
    }

    public void SaveConfig(SettingsTextualFeedback settings)
    {
        settings.IsActive = IsActive;
        settings.MaxLookbackDays = MaxLookbackDays;
        settings.MinSignals = MinSignals;
        settings.BlockThresholdPercent = BlockThresholdPercent;
        settings.ReEnableHours = ReEnableHours;
        settings.Log = Log;
    }
}

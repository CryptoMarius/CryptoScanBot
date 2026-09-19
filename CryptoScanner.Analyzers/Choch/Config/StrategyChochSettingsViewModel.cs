using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Choch.Config;

public partial class StrategyChochSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _requireBosConfirmation = false;

    public void LoadConfig(ChochSettings settings)
    {
        RequireBosConfirmation = settings.RequireBosConfirmation;
    }

    public void SaveConfig(ChochSettings settings)
    {
        settings.RequireBosConfirmation = RequireBosConfirmation;
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyEntryConditionsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _useCustomEntryConditions;

    [ObservableProperty]
    private TraderEntryConditionsViewModel _entryConditionsViewModel;

    public StrategyEntryConditionsViewModel()
    {
        _entryConditionsViewModel = new();
    }

    [RelayCommand]
    private void ResetToGlobal()
    {
        EntryConditionsViewModel.LoadConfig(GlobalData.Settings.Trading.EntryConditions);
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        if (settings.EntryConditions != null)
        {
            UseCustomEntryConditions = true;
            EntryConditionsViewModel.LoadConfig(settings.EntryConditions);
        }
        else
        {
            UseCustomEntryConditions = false;
            EntryConditionsViewModel.LoadConfig(GlobalData.Settings.Trading.EntryConditions);
        }
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        if (UseCustomEntryConditions)
        {
            settings.EntryConditions ??= new SettingsEntryConditions();
            EntryConditionsViewModel.SaveConfig(settings.EntryConditions);
        }
        else
        {
            settings.EntryConditions = null;
        }
    }
}

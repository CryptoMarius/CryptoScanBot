using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

using System.Collections.ObjectModel;

namespace CryptoScanner.Config.ViewModels;

public partial class TraderRulesViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<TraderRuleItemViewModel> _rules = [];

    public void LoadConfig(SettingsTrading settings)
    {
        Rules.Clear();
        int index = 1;
        foreach (var rule in settings.PauseTradingRules)
        {
            var item = new TraderRuleItemViewModel { Index = index++ };
            item.LoadFrom(rule);
            Rules.Add(item);
        }
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.PauseTradingRules.Clear();
        foreach (var item in Rules)
        {
            var rule = new PauseTradingRule();
            item.SaveTo(rule);
            settings.PauseTradingRules.Add(rule);
        }
    }

    [RelayCommand]
    private void AddRule()
    {
        var item = new TraderRuleItemViewModel
        {
            Index = Rules.Count + 1,
            Symbol = "BTCUSDT",
            Interval = CryptoIntervalPeriod.interval5m,
            Candles = 5,
            Percentage = 4.0,
            CoolDown = 20
        };

        Rules.Add(item);
        UpdateIndices();
    }

    [RelayCommand]
    private void RemoveRule()
    {
        if (Rules.Count > 0)
        {
            Rules.RemoveAt(Rules.Count - 1);
            UpdateIndices();
        }
    }

    private void UpdateIndices()
    {
        for (int i = 0; i < Rules.Count; i++)
        {
            Rules[i].Index = i + 1;
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Signal;

using System.Collections.ObjectModel;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;
}

public partial class StrategyViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<StrategyItem> _strategyList = [];

    public StrategyViewModel()
    {
    }

    public void LoadConfig(List<string> strategyList)
    {
        StrategyList.Clear();
        // Sort alphabetically by name so the UI lists e.g. sbm1/sbm2/sbm3, stobb/stobb.dlz/...,
        // storsi/storsi.dlz/... in a predictable order independent of the registration order
        // in RegisterAlgorithms (which is grouped by topic, not name).
        var ordered = RegisterAlgorithms.AlgorithmDefinitionList.Values
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var algorithm in ordered)
        {
            var item = new StrategyItem
            {
                Name = algorithm.Name,
                IsEnabled = strategyList.Contains(algorithm.Name)
            };
            StrategyList.Add(item);
        }
    }

    public void SaveConfig(List<string> strategyList)
    {
        strategyList.Clear();
        foreach (var strategy in StrategyList)
        {
            if (strategy.IsEnabled)
            {
                strategyList.Add(strategy.Name);
            }
        }
    }
}

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
        foreach (var algorithm in RegisterAlgorithms.AlgorithmDefinitionList.Values)
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

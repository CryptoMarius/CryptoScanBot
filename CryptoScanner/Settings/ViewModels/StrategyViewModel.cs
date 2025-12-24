using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;

using System.Collections.ObjectModel;

namespace CryptoScanner.Settings.ViewModels;

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
        foreach (var algorithm in RegisterAlgorithms.AlgorithmDefinitionList.Values)
        {
            StrategyList.Add(new StrategyItem { Name = algorithm.Name, IsEnabled = false });
        }
    }
}

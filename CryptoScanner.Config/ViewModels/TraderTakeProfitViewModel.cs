using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

using System.Collections.ObjectModel;

namespace CryptoScanner.Config.ViewModels;

public partial class TpItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int _index = 0;

    [ObservableProperty]
    private decimal _percentage = 1m; // decimal (EXACT match)

    [ObservableProperty]
    private decimal _factor = 100m; // decimal (EXACT match)

    public string Header => $"TP {Index}";

    partial void OnIndexChanged(int value)
    {
        OnPropertyChanged(nameof(Header));
    }

    public void LoadFrom(CryptoTpEntry entry)
    {
        Percentage = entry.Percentage;
        Factor = entry.Factor;
    }

    public void SaveTo(CryptoTpEntry entry)
    {
        entry.Percentage = Percentage;
        entry.Factor = Factor;
    }
}


public partial class TraderTakeProfitViewModel : ObservableObject
{
    private readonly Dictionary<string, CryptoOrderType> _orderTypeList = new()
    {
        { "Market order", CryptoOrderType.Market },
        { "Limit order", CryptoOrderType.Limit }
    };

    private readonly Dictionary<string, CryptoTakeProfitStrategy> _strategyList = new()
    {
        //{ "Direct na het kopen", CryptoTakeProfitStrategy.Immediately },
        { "Op het opgegeven percentage", CryptoTakeProfitStrategy.FixedPercentage },
    };

    [ObservableProperty]
    private CryptoOrderType _takeProfitOrderType = CryptoOrderType.Limit; // enum (EXACT match)

    [ObservableProperty]
    private CryptoTakeProfitStrategy _takeProfitStrategy = CryptoTakeProfitStrategy.FixedPercentage; // enum (EXACT match)

    [ObservableProperty]
    private bool _addDustToTp = true; // bool (EXACT match)

    [ObservableProperty]
    private ObservableCollection<TpItemViewModel> _tpItems = [];

    public Dictionary<string, CryptoOrderType> OrderTypeList => _orderTypeList;
    public Dictionary<string, CryptoTakeProfitStrategy> StrategyList => _strategyList;

    public void LoadConfig(SettingsTrading settings)
    {
        TakeProfitOrderType = settings.TakeProfitOrderType;
        TakeProfitStrategy = settings.TakeProfitStrategy;
        AddDustToTp = settings.AddDustToTp;

        TpItems.Clear();
        int index = 1;
        foreach (var tp in settings.TpList)
        {
            var item = new TpItemViewModel { Index = index++ };
            item.LoadFrom(tp);
            TpItems.Add(item);
        }
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.TakeProfitOrderType = TakeProfitOrderType;
        settings.TakeProfitStrategy = TakeProfitStrategy;
        settings.AddDustToTp = AddDustToTp;

        settings.TpList.Clear();
        foreach (var item in TpItems)
        {
            var tp = new CryptoTpEntry();
            item.SaveTo(tp);
            settings.TpList.Add(tp);
        }
    }

    [RelayCommand]
    private void AddTp()
    {
        decimal newFactor = 33m;
        decimal newPercentage = 1m;

        // Use last item's values + increment
        if (TpItems.Count > 0)
        {
            var last = TpItems[^1];
            newFactor = last.Factor;
            newPercentage = last.Percentage + 1m;
        }

        var item = new TpItemViewModel
        {
            Index = TpItems.Count + 1,
            Factor = newFactor,
            Percentage = newPercentage,
        };

        TpItems.Add(item);
        UpdateIndices();
    }

    [RelayCommand]
    private void RemoveTp()
    {
        if (TpItems.Count > 0)
        {
            TpItems.RemoveAt(TpItems.Count - 1);
            UpdateIndices();
        }
    }

    private void UpdateIndices()
    {
        for (int i = 0; i < TpItems.Count; i++)
        {
            TpItems[i].Index = i + 1;
        }
    }
}

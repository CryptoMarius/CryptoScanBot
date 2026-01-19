using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class DcaItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int _index = 0;

    [ObservableProperty]
    private decimal _percentage = 1.5m; // decimal (EXACT match)

    [ObservableProperty]
    private decimal _factor = 2m; // decimal (EXACT match)

    public string Header => $"DCA {Index}";

    partial void OnIndexChanged(int value)
    {
        OnPropertyChanged(nameof(Header));
    }

    public void LoadFrom(CryptoDcaEntry entry)
    {
        Percentage = entry.Percentage;
        Factor = entry.Factor;
    }

    public void SaveTo(CryptoDcaEntry entry)
    {
        entry.Percentage = Percentage;
        entry.Factor = Factor;
    }
}


public partial class TraderDcaViewModel : ObservableObject
{
    // Dropdown lists
    private readonly Dictionary<string, CryptoOrderType> _orderTypeList = new()
    {
        { "Limit order", CryptoOrderType.Limit }
    };

    private readonly Dictionary<string, CryptoEntryOrDcaStrategy> _strategyList = new()
    {
        { "Op het opgegeven percentage", CryptoEntryOrDcaStrategy.FixedPercentage }
    };

    private readonly Dictionary<string, CryptoEntryOrDcaPricing> _pricingList = new()
    {
        { "DCA percentage", CryptoEntryOrDcaPricing.SignalPrice }
    };

    [ObservableProperty]
    private CryptoOrderType _selectedOrderType = CryptoOrderType.Limit;

    [ObservableProperty]
    private CryptoEntryOrDcaStrategy _selectedStrategy = CryptoEntryOrDcaStrategy.FixedPercentage;

    [ObservableProperty]
    private CryptoEntryOrDcaPricing _selectedPricing = CryptoEntryOrDcaPricing.SignalPrice;

    [ObservableProperty]
    private ObservableCollection<DcaItemViewModel> _dcaItems = [];

    public Dictionary<string, CryptoOrderType> OrderTypeList => _orderTypeList;
    public Dictionary<string, CryptoEntryOrDcaStrategy> StrategyList => _strategyList;
    public Dictionary<string, CryptoEntryOrDcaPricing> PricingList => _pricingList;

    public void LoadConfig(SettingsTrading settings)
    {
        SelectedOrderType = settings.DcaOrderType;
        SelectedStrategy = settings.DcaStrategy;
        SelectedPricing = settings.DcaOrderPrice;

        DcaItems.Clear();
        int index = 1;
        foreach (var dca in settings.DcaList)
        {
            var item = new DcaItemViewModel { Index = index++ };
            item.LoadFrom(dca);
            DcaItems.Add(item);
        }
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.DcaOrderType = SelectedOrderType;
        settings.DcaStrategy = SelectedStrategy;
        settings.DcaOrderPrice = SelectedPricing;

        settings.DcaList.Clear();
        foreach (var item in DcaItems)
        {
            var dca = new CryptoDcaEntry();
            item.SaveTo(dca);
            settings.DcaList.Add(dca);
        }
    }

    [RelayCommand]
    private void AddDca()
    {
        decimal newPercentage = 6m;
        decimal newFactor = 2m;

        // Use last item's values + increment
        if (DcaItems.Count > 0)
        {
            var last = DcaItems[^1];
            newPercentage = last.Percentage + 6m;
            newFactor = last.Factor * 2m;
        }

        var item = new DcaItemViewModel
        {
            Index = DcaItems.Count + 1,
            Percentage = newPercentage,
            Factor = newFactor
        };

        DcaItems.Add(item);
        UpdateIndices();
    }

    [RelayCommand]
    private void RemoveDca()
    {
        if (DcaItems.Count > 0)
        {
            DcaItems.RemoveAt(DcaItems.Count - 1);
            UpdateIndices();
        }
    }

    private void UpdateIndices()
    {
        for (int i = 0; i < DcaItems.Count; i++)
        {
            DcaItems[i].Index = i + 1;
        }
    }
}

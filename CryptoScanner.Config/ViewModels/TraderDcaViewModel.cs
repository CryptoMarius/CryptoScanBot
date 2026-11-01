using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

using System.Collections.ObjectModel;

namespace CryptoScanner.Config.ViewModels;

public partial class DcaItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int _index = 0;

    [ObservableProperty]
    private decimal _percentage = 1.5m; // decimal (EXACT match)

    // Percentage of the entry amount (100 = 1x, 200 = 2x, ...)
    [ObservableProperty]
    private decimal _factor = 200m; // decimal (EXACT match)

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
    [ObservableProperty]
    private ObservableCollection<DcaItemViewModel> _dcaItems = [];

    public void LoadConfig(SettingsTrading settings)
    {
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
        settings.DcaList.Clear();
        foreach (var item in DcaItems)
        {
            var dca = new CryptoDcaEntry();
            item.SaveTo(dca);
            settings.DcaList.Add(dca);
        }
    }

    // First DCA starts at 3% with a full-size order; every next level doubles both the distance
    // and the order size, so each step down costs twice what the one before it did.
    private const decimal FirstDcaPercentage = 3m;
    private const decimal FirstDcaFactor = 100m;

    [RelayCommand]
    private void AddDca()
    {
        decimal newPercentage = FirstDcaPercentage;
        decimal newFactor = FirstDcaFactor;

        // Use last item's values + increment
        // (every next level doubles both the percentage and the factor of the previous one)
        if (DcaItems.Count > 0)
        {
            var last = DcaItems[^1];
            newPercentage = last.Percentage * 2m;
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

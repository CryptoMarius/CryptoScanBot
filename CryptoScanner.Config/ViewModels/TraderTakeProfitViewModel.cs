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
    [ObservableProperty]
    private bool _addDustToTp = true; // bool (EXACT match)

    [ObservableProperty]
    private decimal _takeProfitTrailPercentage = 0m;

    [ObservableProperty]
    private ObservableCollection<TpItemViewModel> _tpItems = [];

    public void LoadConfig(SettingsTrading settings)
    {
        AddDustToTp = settings.AddDustToTp;
        TakeProfitTrailPercentage = settings.TakeProfitTrailPercentage;

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
        settings.AddDustToTp = AddDustToTp;
        settings.TakeProfitTrailPercentage = TakeProfitTrailPercentage;

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

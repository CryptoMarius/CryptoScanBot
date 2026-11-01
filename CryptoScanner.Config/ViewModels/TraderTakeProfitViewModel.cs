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

    // First take profit sits at 0.75%; every next level doubles the distance. The quantities are
    // shared out over all levels because the position can only be sold once.
    private const decimal FirstTpPercentage = 0.75m;

    [RelayCommand]
    private void AddTp()
    {
        decimal newPercentage = FirstTpPercentage;

        // Use last item's values + increment
        // (every next level doubles the percentage of the previous one)
        if (TpItems.Count > 0)
        {
            var last = TpItems[^1];
            newPercentage = last.Percentage * 2m;
        }

        var item = new TpItemViewModel
        {
            Index = TpItems.Count + 1,
            Factor = 100m,
            Percentage = newPercentage,
        };

        TpItems.Add(item);
        UpdateIndices();
        SpreadTpFactors();
    }

    [RelayCommand]
    private void RemoveTp()
    {
        if (TpItems.Count > 0)
        {
            TpItems.RemoveAt(TpItems.Count - 1);
            UpdateIndices();
            SpreadTpFactors();
        }
    }

    /// <summary>
    /// Divide the quantity evenly over the take profit levels, keeping the total at exactly 100%:
    /// selling more than the whole position is not possible. The last level takes the rounding
    /// remainder, so three levels become 33.33 / 33.33 / 33.34 instead of falling short.
    /// </summary>
    private void SpreadTpFactors()
    {
        if (TpItems.Count == 0)
            return;

        decimal share = Math.Round(100m / TpItems.Count, 2, MidpointRounding.AwayFromZero);
        decimal assigned = 0m;

        for (int i = 0; i < TpItems.Count - 1; i++)
        {
            TpItems[i].Factor = share;
            assigned += share;
        }
        TpItems[^1].Factor = 100m - assigned;
    }

    private void UpdateIndices()
    {
        for (int i = 0; i < TpItems.Count; i++)
        {
            TpItems[i].Index = i + 1;
        }
    }
}

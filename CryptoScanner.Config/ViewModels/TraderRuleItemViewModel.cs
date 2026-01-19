using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class TraderRuleItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int _index = 0;

    [ObservableProperty]
    private string _symbol = "BTCUSDT"; // string (EXACT match)

    [ObservableProperty]
    private double _percentage = 4.0; // double (EXACT match)

    [ObservableProperty]
    private int _candles = 5; // int (EXACT match)

    [ObservableProperty]
    private CryptoIntervalPeriod _interval = CryptoIntervalPeriod.interval5m; // enum (EXACT match)

    [ObservableProperty]
    private int _coolDown = 20; // int (EXACT match, in minutes)

    public string Header => $"Trading rule {Index}";

    // Interval list for ComboBox
    public Dictionary<string, CryptoIntervalPeriod> IntervalList { get; } = new();

    public TraderRuleItemViewModel()
    {
        // Populate interval list from GlobalData
        foreach (var interval in GlobalData.IntervalList)
        {
            IntervalList.Add(interval.Name, interval.IntervalPeriod);
        }
    }

    partial void OnIndexChanged(int value)
    {
        OnPropertyChanged(nameof(Header));
    }

    public void LoadFrom(Core.Settings.PauseTradingRule rule)
    {
        Symbol = rule.Symbol;
        Percentage = rule.Percentage;
        Candles = rule.Candles;
        Interval = rule.Interval;
        CoolDown = rule.CoolDown;
    }

    public void SaveTo(Core.Settings.PauseTradingRule rule)
    {
        rule.Symbol = Symbol;
        rule.Percentage = Percentage;
        rule.Candles = Candles;
        rule.Interval = Interval;
        rule.CoolDown = CoolDown;
    }
}

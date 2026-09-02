using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal.Helpers;

using System.Collections.ObjectModel;

namespace CryptoScanner.Config.ViewModels;

/// <summary>
/// One reversal shape with its checkbox, the same shape as the <see cref="IntervalItem"/> behind the
/// interval picker - a list of names that can be ticked independently needs nothing more.
/// </summary>
public partial class CandlePatternItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isChecked;

    public CandlePatternItem(string name, bool isChecked = false)
    {
        _name = name;
        _isChecked = isChecked;
    }
}


/// <summary>
/// The list of reversal shapes, as checkboxes, for the shapes the CandlePattern strategy fires on -
/// a List&lt;string&gt; of <see cref="CryptoCandlePattern"/> names. The entry conditions had a second
/// copy of this list until 02-09-2026, for the shape an entry waits for; that setting is gone.
/// </summary>
public partial class CandlePatternListViewModel : ObservableObject
{
    /// <summary>
    /// One entry per member of <see cref="CryptoCandlePattern"/>, in the order they are declared.
    /// That is the order the Photino hosts list them in as well, and it is the order the first match
    /// is looked for in - so both hosts report the same shape for a candle that forms two.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CandlePatternItem> _patterns = [];

    public CandlePatternListViewModel()
    {
        LoadConfig([]);
    }


    public void LoadConfig(List<string> names)
    {
        Patterns.Clear();
        foreach (string name in Enum.GetNames<CryptoCandlePattern>())
        {
            // Case-insensitive: the list can also be typed by hand in the settings file or in the
            // emulator queue, and the scanner parses those names case-insensitively too.
            bool isChecked = names.Exists(p => p.Equals(name, StringComparison.OrdinalIgnoreCase));
            Patterns.Add(new CandlePatternItem(name, isChecked));
        }
    }

    public List<string> SaveConfig()
        => [.. Patterns.Where(p => p.IsChecked).Select(p => p.Name)];
}


/// <summary>
/// The thresholds the shapes are measured against, every one a percentage of the candle's own range.
/// The <see cref="CandlePatternSettings"/> behind the CandlePattern strategy, which is the only place
/// left that holds one.
/// </summary>
public partial class CandlePatternShapeViewModel : ObservableObject
{
    [ObservableProperty]
    private decimal _maxBodyPercentage = 30m;

    [ObservableProperty]
    private decimal _minBodyPercentage = 40m;

    [ObservableProperty]
    private decimal _minWickPercentage = 60m;

    [ObservableProperty]
    private decimal _maxOppositeWickPercentage = 10m;

    [ObservableProperty]
    private decimal _tweezerTolerancePercentage = 5m;


    public void LoadConfig(CandlePatternSettings shape)
    {
        MaxBodyPercentage = shape.MaxBodyPercentage;
        MinBodyPercentage = shape.MinBodyPercentage;
        MinWickPercentage = shape.MinWickPercentage;
        MaxOppositeWickPercentage = shape.MaxOppositeWickPercentage;
        TweezerTolerancePercentage = shape.TweezerTolerancePercentage;
    }

    public void SaveConfig(CandlePatternSettings shape)
    {
        shape.MaxBodyPercentage = MaxBodyPercentage;
        shape.MinBodyPercentage = MinBodyPercentage;
        shape.MinWickPercentage = MinWickPercentage;
        shape.MaxOppositeWickPercentage = MaxOppositeWickPercentage;
        shape.TweezerTolerancePercentage = TweezerTolerancePercentage;
    }
}

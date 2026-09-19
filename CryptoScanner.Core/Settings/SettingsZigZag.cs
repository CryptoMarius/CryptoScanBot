using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Core.Settings;

[Serializable]
public class SettingsZigZag
{
    // The captions are read by the reflection based settings editor of the Blazor hosts; the
    // Avalonia IndicatorZigZagView has its own labels for the same two values.
    [SettingCaption("Use high/low")]
    public bool UseHighLow { get; set; } = false;

    [SettingCaption("Trend type")]
    public TrendType TrendType { get; set; } = TrendType.Primary;

    public SettingsZigZag(bool useHighLow, TrendType trendType)
    {
        UseHighLow = useHighLow;
        TrendType = trendType;
    }
}

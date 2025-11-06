using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Settings;

[Serializable]
public class SettingsZigZag
{
    public bool UseHighLow { get; set; } = false;
    public TrendType TrendType { get; set; } = TrendType.Primary;

    public SettingsZigZag(bool useHighLow, TrendType trendType)
    {
        UseHighLow = useHighLow;
        TrendType = trendType;
    }
}

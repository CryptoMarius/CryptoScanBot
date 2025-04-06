using CryptoScanBot.Core.Core;

namespace CryptoScanBot.Core.Settings;

[Serializable]
public class SettingsTrend
{
    public SettingsZigZag Primary { get; set; } = new(true, TrendType.Primary);
    public SettingsZigZag Secondary { get; set; } = new(true, TrendType.Secondary);
}

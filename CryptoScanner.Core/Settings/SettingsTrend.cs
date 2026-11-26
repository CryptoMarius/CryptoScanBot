using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Settings;

/// <summary>
/// The two zigzag settings behind the trend: fixed values, not settings. The screens to change them
/// were hidden on 11-05-2026 ("it should always be the fixed values"), but a settings file that
/// still carried an older choice kept it alive without anyone being able to see or change it. A
/// Primary with TrendType Secondary wrote its result into the secondary slot, so the primary trend
/// stayed empty: every signal showed 0.00 and "-" in the trend columns and the trend filters had
/// nothing to read (HyperLiquid field user, 20-11-2026). Since then <see cref="SettingsBasic.Trend"/>
/// is neither read from nor written to the settings file, and these values cannot be replaced.
/// </summary>
[Serializable]
public class SettingsTrend
{
    public SettingsZigZag Primary { get; } = new(true, TrendType.Primary);
    public SettingsZigZag Secondary { get; } = new(true, TrendType.Secondary);
}

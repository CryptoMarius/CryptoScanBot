using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.IChimokuKumoBreakout;

// Settings of the ichimokukumobreakout strategy (price pushes through the Ichimoku cloud). The
// header used to describe dbr, copied along with the file (open point 112).
[Serializable]
public class IChimokuKumoBreakoutSettings : SettingsSignalStrategyBase
{
    // Own BB range since 26-09-2026 (open point 109): this strategy used to read stobb's pair, so
    // whoever tuned stobb silently retuned this one too. The defaults are what it effectively ran
    // with (stobb's 1,5 and no upper bound).
    [SettingCaption("Filter on BB%")]
    public double BBMinPercentage { get; set; } = 1.50;

    [SettingCaption("", SameRowAs = nameof(BBMinPercentage))]
    public double BBMaxPercentage { get; set; } = 0.0;

    public IChimokuKumoBreakoutSettings() : base()
    {
    }
}

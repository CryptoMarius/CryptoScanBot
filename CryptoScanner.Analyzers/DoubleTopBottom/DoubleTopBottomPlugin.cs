using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.DoubleTopBottom;

//***************************************************
// CHoCH — fires on a Change of Character of the ZigZag-derived structure.
// Primary / Secondary chooses which trend slot is read. The .pullback variants
// additionally require an opposite zigzag pivot + breakthrough before stepping in.
//***************************************************
public class DoubleTopBottomPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "DoubleTopBottom";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        //new(CryptoSignalStrategy.ChochPrimary,
        //    "dtb",
        //    typeof(Signal.SignalChochPrimaryLong),
        //    typeof(Signal.SignalChochPrimaryShort)
        //),
    ];


    public static DoubleTopBottomSettings Settings { get; internal set; } = new();
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not DoubleTopBottomSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new DoubleTopBottomSettings();
        return Settings;
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = null; // new Config.NweConfigView();
}

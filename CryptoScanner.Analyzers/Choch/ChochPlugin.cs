using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Choch;

//***************************************************
// CHoCH — fires on a Change of Character of the ZigZag-derived structure.
// Primary / Secondary chooses which trend slot is read. The .pullback variants
// additionally require an opposite zigzag pivot + breakthrough before stepping in.
//***************************************************
public class ChochPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "Choch";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new("choch.primary",
            typeof(Signal.SignalChochPrimaryLong),
            typeof(Signal.SignalChochPrimaryShort)
        ),

        new("choch.primary.pullback",
            typeof(Signal.SignalChochPrimaryPullbackLong),
            typeof(Signal.SignalChochPrimaryPullbackShort)
        ),

        new("choch.secondary",
            typeof(Signal.SignalChochSecondaryLong),
            typeof(Signal.SignalChochSecondaryShort)
        ),

        new("choch.secondary.pullback",
            typeof(Signal.SignalChochSecondaryPullbackLong),
            typeof(Signal.SignalChochSecondaryPullbackShort)
        ),
    ];


    public static ChochSettings Settings { get; internal set; } = new();
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not ChochSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new ChochSettings();
        return Settings;
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = null; // new Config.NweConfigView();
}

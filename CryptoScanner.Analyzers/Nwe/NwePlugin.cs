using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Nwe;

public class NwePlugin : IStrategyPlugin
{
    public const string StrategyInternal = "Nwe";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    private const string StrategyInternalNp = "Nwe.np";
    private const string StrategyInternalBb = "Nwe.bb";

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        // NWE Repaining
        new(CryptoSignalStrategy.Nwe,
            StrategyInternal.ToLower(),
            typeof(Signal.SignalNwe),
            typeof(Signal.SignalNwe)
        ),

        // NWE not repainting
        new(CryptoSignalStrategy.NweNp,
            StrategyInternalNp.ToLower(),
            typeof(Signal.SignalNweNp),
            typeof(Signal.SignalNweNp)
        ),

        // NWE × BB crossover: NWE curls through the BB band after extending beyond it
        new(CryptoSignalStrategy.NweBb,
            StrategyInternalBb.ToLower(),
            typeof(Signal.SignalNweBbLong),
            typeof(Signal.SignalNweBbShort)
        ),
    ];


    public static SettingsSignalStrategyNwe Settings { get; internal set; } = new();
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not SettingsSignalStrategyNwe s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new SettingsSignalStrategyNwe();
        return Settings;
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = null; // new Config.NweConfigView();
}

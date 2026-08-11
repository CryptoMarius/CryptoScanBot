using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.BbSqueeze;

// Bollinger Bands Squeeze + MACD breakout strategie
public class BbSqueezePlugin : IStrategyPlugin
{
    public const string StrategyInternal = "BbSqueeze";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new("bbsqueeze",
            typeof(Signal.SignalBbSqueezeLong),
            typeof(Signal.SignalBbSqueezeShort)
        ),
    ];

    public static BbSqueezeSettings Settings { get; internal set; } = new();
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not BbSqueezeSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new BbSqueezeSettings();
        return Settings;
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.BbSqueezeConfigView();
}

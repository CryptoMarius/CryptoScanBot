using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.KumoSqueeze;

/// <summary>
/// Ichimoku Kumo + Bollinger Squeeze Breakout: enters when price breaks out of a
/// squeezed Bollinger Band while positioned on the correct side of the Ichimoku Cloud,
/// confirmed by volume spike and optional RSI / Tenkan-Kijun filters.
/// </summary>
public class KumoSqueezePlugin : IStrategyPlugin
{
    private const string StrategyInternal = "KumoSqueeze";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(Signal.KumoSqueezeSignalLong),
            typeof(Signal.KumoSqueezeSignalShort)
        ),
    ];

    public static KumoSqueezeSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new KumoSqueezeSettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not KumoSqueezeSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.KumoSqueezeConfigView();
}

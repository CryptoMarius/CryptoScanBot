using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Signal.Indicators;

namespace CryptoScanner.Analyzers.PsarCross;

/// <summary>
/// The parabolic SAR cross: in when the SAR flips to the other side of the price, out when it flips
/// back. Reads the standard SAR (0.02, 0.2) every hub already computes and declares nothing of its
/// own. No chart overlay of its own: the SAR dots already exist in both charts.
/// </summary>
public class PsarCrossPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "PsarCross";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(Signal.PsarCrossLong),
            typeof(Signal.PsarCrossShort)
        ),
    ];

    /// <summary>
    /// Nothing extra: the SAR is part of the standard set. There is deliberately no ADX filter, because
    /// CryptoData.Adx14 only exists in a DEBUG build and this strategy is registered in Release too.
    /// </summary>
    public IReadOnlyList<IndicatorKey> RequiredIndicators { get; } = [];

    public static PsarCrossSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new PsarCrossSettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not PsarCrossSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.PsarCrossConfigView();
}

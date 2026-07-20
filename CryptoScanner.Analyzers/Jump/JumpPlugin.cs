using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Jump;

public class JumpPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "Jump";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(CryptoSignalStrategy.Jump,
            "jump",
            typeof(Signal.SignalCandleJumpLong),
            typeof(Signal.SignalCandleJumpShort)
        ),

    ];

    public static SettingsSignalStrategyJump Settings { get; internal set; } = new();
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not SettingsSignalStrategyJump s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new SettingsSignalStrategyJump();
        return Settings;
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.JumpConfigView();
}

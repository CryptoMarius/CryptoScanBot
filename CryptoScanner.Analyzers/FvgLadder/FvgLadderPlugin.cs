using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.FvgLadder;

/// <summary>
/// FVG &amp; DCA (no stop-loss): buy limits on the tops of bullish fair value gaps while the day close
/// is above its EMA200, DCA's on doubling sizes, take profit 1% above the break-even price, no stop
/// loss, and several positions on one coin at once. Built for BTC spot from the rules in
/// "BTC FVG DCA - Regels ter afstemming", with a preset in which every gap is a position of its own.
/// <para>
/// Registered with three trader properties no other strategy has (see StrategyRegistration):
/// parallel positions, a standing entry and no stop loss. Paper trading and the emulator only.
/// </para>
/// </summary>
public class FvgLadderPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "FvgLadder";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(Signal.FvgLadderLong),
            null,
            IsZoneStrategy: true,
            ParallelPositions: true,
            StandingEntry: true,
            WithoutStopLoss: true
        ),
    ];

    public static FvgLadderSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new FvgLadderSettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not FvgLadderSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    /// <summary>The day EMA it carries per symbol, so a next emulator run starts on its own candles.</summary>
    public void ResetRunState() => Signal.FvgLadderTrend.Reset();

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.FvgLadderConfigView();
}

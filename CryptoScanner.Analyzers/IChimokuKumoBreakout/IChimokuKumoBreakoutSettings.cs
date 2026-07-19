using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.IChimokuKumoBreakout;

// "bre" — Buddy Reversion Engine (BRE): Donchian-based outer bands (the gray "plateaus") with an
// EMA+ATR middle cloud (DIDO). A long alert fires when the Low breaks the macro LOWER band; a short
// when the High breaks the macro UPPER band, with optional HMA-trend / RSI / Stochastic-RSI filters.
// These parameters drive BOTH the chart drawer (BreBands) and the bre signal (BreBandsHelper),
// so the chart and the alert always stay in sync. Defaults match the original Pine inputs.
[Serializable]
public class IChimokuKumoBreakoutSettings : SettingsSignalStrategyBase
{

    public IChimokuKumoBreakoutSettings() : base()
    {
    }
}

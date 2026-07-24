# Nadaraya-Watson Envelope (NWE)

## Overview

The **NWE** strategy uses the Nadaraya-Watson kernel regression to build a non-parametric price envelope. Signals fire when price moves outside this envelope, indicating statistical extremes. This is a **counter-trend / exhaustion** strategy — it signals potential reversal points, not trend continuations.

Three signal variants:
- **nwe** — repainting variant (recalculates with each new bar, stronger signals but may shift).
- **nwe.np** — non-repainting variant (only uses confirmed bars).
- **nwe.bb** — NWE × Bollinger Bands crossover (fires when NWE band crosses back inside BB).

This is an experimental strategy (DEBUG-only).

## How it works

### NWE indicator

The Nadaraya-Watson estimator computes a kernel-smoothed price curve using a Gaussian kernel with configurable `BandWidth` (default 8.0). An envelope is placed at `± Multiplication` (default 3.0) standard deviations around this curve.

### Signal: NWE / NWE NP

1. BB width must be at least `BBMinPercentage`.
2. **Long**: both Open and Close are below the lower NWE band, and the candle is bullish (Close > Open) — price has overshot the statistical lower extreme and is recovering.
3. **Short**: both Open and Close are above the upper NWE band, and the candle is bearish (Close < Open).

### Signal: NWE BB crossover (nwe.bb)

Detects when the NWE band crosses back inside the Bollinger Band:

1. BB width within `[BBMinPercentage, 100]`.
2. **Long**: previous bar's NWE lower was below BB lower (outside); current bar's NWE lower is above BB lower (crossed inside). BB lower must be falling for 2 bars. At least one close below BB lower in the last 5 bars.
3. **Short**: previous bar's NWE upper was above BB upper; current bar's NWE upper crosses below BB upper. BB upper must be rising for 2 bars.

NWE BB gives up after 3 candles.

### Additional checks (NWE / NWE NP only)

| Check | Setting | Description |
|-------|---------|-------------|
| SBM conditions | `IncludeSoftSbm` | SMA 200/50/20 must be in the correct order |
| SBM crossing | `IncludeSbmPercAndCrossing` | MA percentage distances + crossings |
| RSI | `IncludeRsi` | RSI must confirm oversold/overbought |
| Recent signal | Always | Must have had a StoRsi or Stobb signal in last 10 candles |
| Volume climax | `RequireVolumeClimax` | Current volume ≥ multiplier × average volume |

## Signal conditions summary

### Long entry (nwe / nwe.np)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | BB width ≥ BBMinPercentage | Minimum volatility filter |
| 2 | Open < NWE lower AND Close < NWE lower | Entire candle body outside lower envelope |
| 3 | Close > Open | Bullish candle (recovery starting) |
| 4 | Recent StoRsi/Stobb signal (≤10 bars) | Confirms broader oversold context |

### Long entry (nwe.bb)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | BB width within range | Minimum volatility filter |
| 2 | Close < SMA20 | Price below mid-BB |
| 3 | Previous NWE lower < BB lower | NWE was outside BB |
| 4 | Current NWE lower > BB lower | NWE crossed back inside BB |
| 5 | BB lower falling for 2 bars | Band is expanding downward |
| 6 | Recent close below BB lower | At least one in last 5 bars |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `BandWidth` | 8.0 | Kernel regression bandwidth (smoothness) |
| `Multiplication` | 3.0 | Envelope distance multiplier (standard deviations) |
| `IncludeRsi` | false | Require RSI oversold/overbought |
| `IncludeSoftSbm` | false | Require SBM MA conditions |
| `IncludeSbmPercAndCrossing` | false | Require SBM MA percentages + crossings |
| `RequireVolumeClimax` | false | Require unusual volume spike |
| `VolumeClimaxLookback` | 20 | Period for average volume calculation |
| `VolumeClimaxMultiplier` | 1.5 | Current volume must be this × average |

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| Nadaraya-Watson Envelope | Statistical price envelope (kernel regression) |
| Bollinger Bands (20, 2σ) | Width gate + NWE BB crossover variant |
| SMA(20) | NWE BB: price below/above mid-line check |
| RSI | Optional oversold/overbought filter |
| SMA(50), SMA(200) | Optional SBM conditions |
| Stochastic | Implicit via StoRsi/Stobb signal requirement |

## Strategy type

- **Counter-trend / exhaustion** (reversal signals at statistical extremes)
- NWE BB variant = crossover signal (NWE re-entering BB)
- Experimental (DEBUG-only)

## File structure

```
CryptoScanner.Analyzers/Nwe/
├── NwePlugin.cs                          # Plugin registration (nwe, nwe.np, nwe.bb)
├── NweSettings.cs                        # Strategy-specific settings
├── Nwe.md                                # This document
├── Config/
│   ├── StrategyNweTabView.axaml          # Settings tab UI
│   └── StrategyNweTabViewModel.cs        # Settings viewmodel
└── Signal/
    ├── SignalNweBase.cs                   # Shared NWE logic + additional checks
    ├── SignalNwe.cs                       # Repainting variant
    ├── SignalNweNp.cs                     # Non-repainting variant
    ├── SignalNweBbBase.cs                 # NWE BB shared base
    ├── SignalNweBbLong.cs                 # NWE BB long: lower band crossover
    ├── SignalNweBbShort.cs                # NWE BB short: upper band crossover
    └── NweBbDetector.cs                   # NWE/BB bar data helper
```

Enum values: `CryptoSignalStrategy.Nwe = 25`, `CryptoSignalStrategy.NweNp = 26`, `CryptoSignalStrategy.NweBb = 27`

## Registration

Registered as a DEBUG-only strategy in `AnalyzerRegistration.cs`. Strategy names in the UI: **nwe**, **nwe.np**, **nwe.bb**.

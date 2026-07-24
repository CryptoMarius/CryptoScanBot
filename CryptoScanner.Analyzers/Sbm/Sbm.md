# SMA-Based Method (SBM)

## Overview

The **SBM** strategy family detects oversold/overbought conditions using a combination of SMA (Simple Moving Average) line alignment, Parabolic SAR positioning, MACD histogram recovery, and various BB/price-action triggers. SBM is described as "a special kind of STOBB" — it layers additional moving-average and trend-structure filters on top of BB-based signals to produce higher-quality entries.

Three sub-strategies exist with different trigger mechanisms:
- **SBM1** — requires a recent STOBB event (price was below/above BB + Stoch extreme) in the lookback window.
- **SBM2** — requires price to have been in the lower/upper part of the Bollinger Bands recently.
- **SBM3** — requires the Bollinger Bands width to have increased significantly (volatility expansion).

All three share the same base checks (MA alignment, PSar, MACD recovery, MA percentage distances, MA crossings).

## How it works

### Shared base conditions (all SBM variants)

Every SBM signal requires these conditions to pass:

1. **BB width filter**: `BBMinPercentage ≤ BB% ≤ BBMaxPercentage`
2. **SMA alignment** (SBM conditions):
   - Long (oversold): SMA(200) > SMA(50) > SMA(20) — the MAs are stacked in bearish order, indicating a downtrend where a reversal might occur.
   - Short (overbought): SMA(200) < SMA(50) < SMA(20) — the MAs are stacked in bullish order.
3. **Parabolic SAR positioning**:
   - Long: PSar is below SMA(20) AND above the candle close (bearish SAR, but starting to reverse).
   - Short: PSar is above SMA(20) AND below the candle close (bullish SAR, but starting to reverse).
4. **MACD histogram recovery** (AdditionalChecks):
   - Long: MACD histogram has been rising for `CandlesForMacdRecovery` consecutive bars.
   - Short: MACD histogram has been falling for `CandlesForMacdRecovery` consecutive bars.
5. **MA percentage distances** (AdditionalChecks, configurable):
   - The percentage distance between SMA(200)/SMA(50), SMA(50)/SMA(20), and SMA(200)/SMA(20) must exceed configurable minimums. This ensures the MAs are sufficiently spread apart (real trend, not a flat market).
6. **MA crossings** (AdditionalChecks, configurable):
   - Recent crossings between MA pairs must have occurred within configurable lookback windows. This confirms the trend structure developed recently.

### SBM1: STOBB lookback trigger

In addition to the shared base conditions, SBM1 requires that a STOBB-like event (price below/above BB + Stochastic oversold/overbought) occurred in the last `Sbm1CandlesLookbackCount` candles. This means SBM1 fires **after** a STOBB condition, once the MACD and MA structure also confirm.

### SBM2: BB position trigger

SBM2 requires that price was in the lower/upper part of the Bollinger Bands within the last `Sbm2CandlesLookbackCount` candles. "Lower/upper part" means the price was within `Sbm2BbPercentage`% of the band edge.

### SBM3: BB expansion trigger

SBM3 requires that the BB width has **increased significantly** over the last `Sbm3CandlesLookbackCount` candles. Specifically, the current BB width divided by the minimum BB width in the lookback window must exceed `Sbm3CandlesBbRecoveryPercentage`%. This detects a volatility expansion after a squeeze — similar to the BbSqueeze strategy but with all the SBM MA structure on top.

### Give-up condition

The signal is abandoned when price or LastPrice moves past the opposite Bollinger Band (close or LastPrice above upper BB for long, below lower BB for short).

## Signal conditions summary

### All variants — shared conditions

| # | Condition | Description |
|---|-----------|-------------|
| 1 | BB width in range | `BBMinPercentage ≤ BB% ≤ BBMaxPercentage` |
| 2 | SMA alignment | SMA(200) > SMA(50) > SMA(20) for long, reversed for short |
| 3 | PSar position | PSar below SMA(20) and above close for long, reversed for short |
| 4 | MACD recovery | Histogram rising (long) or falling (short) for N bars |
| 5 | MA percentage distances | Configurable minimum spread between MA pairs |
| 6 | MA crossings | Recent crossings between MA pairs within lookback windows |

### Per-variant trigger

| Variant | Trigger | Description |
|---------|---------|-------------|
| SBM1 | Recent STOBB event | Below/above BB + Stoch extreme in last N candles |
| SBM2 | BB position | Price in lower/upper part of BB in last N candles |
| SBM3 | BB expansion | BB width increased by ≥ N% over lookback window |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| **General** | | |
| `BBMinPercentage` | 1.50 | Minimum BB width percentage |
| `BBMaxPercentage` | 100.0 | Maximum BB width percentage |
| `CandlesForMacdRecovery` | 2 | Bars of MACD histogram recovery required |
| **MA percentages** | | |
| `CheckMa200AndMa50Percentage` | true | Enable SMA(200) vs SMA(50) distance check |
| `Ma200AndMa50Percentage` | 0.25 | Minimum distance (%) between SMA(200) and SMA(50) |
| `CheckMa50AndMa20Percentage` | true | Enable SMA(50) vs SMA(20) distance check |
| `Ma50AndMa20Percentage` | 0.25 | Minimum distance (%) between SMA(50) and SMA(20) |
| `CheckMa200AndMa20Percentage` | true | Enable SMA(200) vs SMA(20) distance check |
| `Ma200AndMa20Percentage` | 0.50 | Minimum distance (%) between SMA(200) and SMA(20) |
| **MA crossings** | | |
| `Ma200AndMa50Crossing` | true | Require SMA(200)/SMA(50) crossing in lookback |
| `Ma200AndMa50Lookback` | 30 | Lookback candles for SMA(200)/SMA(50) crossing |
| `Ma50AndMa20Crossing` | true | Require SMA(50)/SMA(20) crossing in lookback |
| `Ma50AndMa20Lookback` | 10 | Lookback candles for SMA(50)/SMA(20) crossing |
| `Ma200AndMa20Crossing` | true | Require SMA(200)/SMA(20) crossing in lookback |
| `Ma200AndMa20Lookback` | 15 | Lookback candles for SMA(200)/SMA(20) crossing |
| **SBM1** | | |
| `Sbm1CandlesLookbackCount` | 5 | Candles to look back for a STOBB event |
| `UseLowHigh` | false | Use Low/High instead of Open/Close for STOBB check |
| **SBM2** | | |
| `Sbm2CandlesLookbackCount` | 3 | Candles to look back for BB position |
| `Sbm2BbPercentage` | 2.5 | Percentage threshold for "lower/upper part" of BB |
| `Sbm2UseLowHigh` | false | Use Low/High instead of Open/Close |
| **SBM3** | | |
| `Sbm3CandlesLookbackCount` | 8 | Candles to look back for BB expansion |
| `Sbm3CandlesBbRecoveryPercentage` | 225 | Required BB width increase (%) over the lookback window |

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| SMA(20), SMA(50), SMA(200) | Trend structure / MA alignment |
| Bollinger Bands (SMA 20, 2σ) | Width filter, BB position check (SBM2), BB expansion (SBM3) |
| Parabolic SAR | Trend reversal confirmation |
| MACD histogram | Momentum recovery confirmation |
| Stochastic (%K, %D) | Used by SBM1 via STOBB lookback |

## Strategy type

- **Mean-reversion with trend-structure confirmation**
- More selective than STOBB — requires alignment of multiple trend and momentum indicators.
- SBM1 is the most popular variant; SBM3 is similar in spirit to BbSqueeze but with full SBM filters.

## File structure

```
CryptoScanner.Analyzers/Sbm/
├── SbmPlugin.cs                          # Plugin registration (sbm1, sbm2, sbm3)
├── SbmSettings.cs                        # Strategy-specific settings
├── Sbm.md                                # This document
├── Config/
│   ├── SbmConfigView.cs                  # Settings UI bridge
│   ├── StrategySbmTabView.axaml          # Settings tab UI
│   ├── StrategySbmTabViewModel.cs        # Settings viewmodel
│   ├── StrategySbmSettingsView.axaml     # Detailed settings sub-view
│   ├── StrategySbmSettingsViewModel.cs
│   ├── StrategySbmSettingsMethodsView.axaml
│   └── StrategySbmSettingsMethodsViewModel.cs
└── Signal/
    ├── SignalSbmBase.cs                  # Shared: indicator checks, MACD recovery, MA checks, GiveUp
    ├── SignalSbm1Long.cs                 # SBM1 long: MA alignment + PSar + recent STOBB + MACD recovery
    ├── SignalSbm1Short.cs                # SBM1 short
    ├── SignalSbm2Long.cs                 # SBM2 long: MA alignment + PSar + BB position + MACD recovery
    ├── SignalSbm2Short.cs                # SBM2 short
    ├── SignalSbm3Long.cs                 # SBM3 long: MA alignment + PSar + BB expansion + MACD recovery
    └── SignalSbm3Short.cs                # SBM3 short
```

Enum values: `CryptoSignalStrategy.Sbm1 = 1`, `CryptoSignalStrategy.Sbm2 = 2`, `CryptoSignalStrategy.Sbm3 = 3`

## Registration

Registered as a production strategy in `AnalyzerRegistration.cs`. Strategy names in the UI: **sbm1**, **sbm2**, **sbm3**.

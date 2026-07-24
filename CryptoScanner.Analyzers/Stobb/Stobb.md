# Stochastic + Bollinger Bands (STOBB)

## Overview

The **STOBB** strategy detects oversold/overbought conditions by combining Bollinger Bands with the Stochastic oscillator. It fires when price touches or crosses outside the Bollinger Bands while the Stochastic indicator confirms an extreme reading. This is one of the original core strategies of the scanner.

Two variants exist:
- **stobb** — single-timeframe signal.
- **stobb.multi** — multi-timeframe confirmation: the same conditions must hold across 4 consecutive timeframes simultaneously.

## How it works

### Phase 1: Bollinger Bands width filter

The BB width percentage must be within a configurable range (`BBMinPercentage` .. `BBMaxPercentage`). This filters out coins with bands that are too narrow (no volatility) or too wide (extreme volatility).

### Phase 2: Price outside Bollinger Bands

- **Long**: price (open or close, optionally low) must be at or below the **lower** Bollinger Band.
- **Short**: price (open or close, optionally high) must be at or above the **upper** Bollinger Band.

### Phase 3: Stochastic confirmation

- **Long**: both Stochastic %K and %D must be in the oversold zone.
- **Short**: both Stochastic %K and %D must be in the overbought zone.

### Multi-timeframe variant (stobb.multi)

Instead of checking one timeframe, the multi variant verifies the same conditions (below/above BB + Stochastic oversold/overbought) across **4 out of 6 consecutive timeframes**, starting from the signal interval upward. The first (lowest) interval must always qualify. Higher timeframes are checked by calculating indicators via `IndicatorEngine.CalculateIndicatorsForInterval`.

### Give-up condition

The signal is abandoned when price crosses back past the SMA(20) middle line (close above SMA20 for long, close below SMA20 for short).

## Signal conditions summary

### Long entry (stobb)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | `BBMinPercentage ≤ BB% ≤ BBMaxPercentage` | BB width within acceptable range |
| 2 | Price ≤ Lower Bollinger Band | Open/close (or low) at or below the lower band |
| 3 | Stochastic oversold | Both %K and %D below the oversold threshold |

### Short entry (stobb)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | `BBMinPercentage ≤ BB% ≤ BBMaxPercentage` | BB width within acceptable range |
| 2 | Price ≥ Upper Bollinger Band | Open/close (or high) at or above the upper band |
| 3 | Stochastic overbought | Both %K and %D above the overbought threshold |

## Optional additional checks

These checks are configured in the STOBB settings and only apply when enabled:

| Check | Setting | Description |
|-------|---------|-------------|
| SBM MA conditions | `IncludeSoftSbm` | Require the SMA 200/50/20 lines to be in the correct order (bearish for long, bullish for short) |
| SBM MA percentages + crossings | `IncludeSbmPercAndCrossing` | Require minimum percentage distance between MA lines and recent MA crossings |
| RSI confirmation | `IncludeRsi` | Require RSI to also be in oversold/overbought territory |
| Previous STOBB | `OnlyIfPreviousStobb` | Only fire if there was another STOBB signal in the recent past (confirms repeated extremes) |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `BBMinPercentage` | 1.50 | Minimum BB width percentage — filters out tight, choppy bands |
| `BBMaxPercentage` | 5.0 | Maximum BB width percentage — filters out extreme volatility |
| `UseLowHigh` | false | Use candle Low/High instead of Open/Close for band-crossing check |
| `IncludeRsi` | false | Additionally require RSI oversold/overbought |
| `IncludeSoftSbm` | false | Additionally require SBM MA line conditions |
| `IncludeSbmPercAndCrossing` | false | Additionally require SBM MA percentage distances and crossings |
| `OnlyIfPreviousStobb` | false | Only fire when a previous STOBB signal occurred in the last 5–60 candles |

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| Bollinger Bands (SMA 20, 2σ) | Band crossing detection + width filter |
| Stochastic (%K, %D) | Oversold/overbought confirmation |
| SMA(20) | Give-up threshold (middle BB line) |
| RSI | Optional oversold/overbought confirmation |
| SMA(50), SMA(200) | Optional SBM conditions |
| Parabolic SAR | Used by SBM checks when enabled |

## Strategy type

- **Mean-reversion / Oversold-overbought**
- The stobb.multi variant is more selective and produces fewer, higher-conviction signals.

## File structure

```
CryptoScanner.Analyzers/Stobb/
├── StobbPlugin.cs                        # Plugin registration (stobb + stobb.multi)
├── Stobb.md                              # This document
├── Config/
│   ├── StobbConfigView.cs                # Settings UI bridge
│   ├── StrategyStobbTabView.axaml        # Settings tab UI
│   └── StrategyStobbTabViewModel.cs      # Settings viewmodel
└── Signal/
    ├── SignalStobbBase.cs                 # Indicator checks, GiveUp (close vs SMA20)
    ├── SignalStobbLong.cs                 # Long signal: below BB + Stoch oversold
    ├── SignalStobbShort.cs                # Short signal: above BB + Stoch overbought
    ├── SignalStobbMultiLong.cs            # Multi-TF long: 4/6 timeframes below BB + Stoch oversold
    └── SignalStobbMultiShort.cs           # Multi-TF short: 4/6 timeframes above BB + Stoch overbought
```

Settings class: `CryptoScanner.Core/Settings/Strategy/StobbSettings.cs` (note: in the Core project, not the Analyzers project)
Enum values: `CryptoSignalStrategy.Stobb = 6`, `CryptoSignalStrategy.StobbMulti = 7`

## Registration

Registered as a production strategy in `AnalyzerRegistration.cs`. Strategy names in the UI: **stobb**, **stobb.multi**.

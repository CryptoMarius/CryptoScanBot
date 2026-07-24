# Stochastic + RSI (StoRsi / STORSI)

## Overview

The **StoRsi** strategy (also known as WGHM — Wave Generation High Momentum) detects extreme oversold/overbought conditions by requiring **both** the Stochastic oscillator and RSI to be in their extreme zones simultaneously. This dual-momentum confirmation produces fewer but higher-conviction signals compared to checking either indicator alone.

Based on the TradingView indicator [WGHBM](https://www.tradingview.com/script/0F1sNM49-WGHBM/).

Two variants exist:
- **storsi** — single-timeframe signal.
- **storsi.multi** — multi-timeframe confirmation: both Stochastic AND RSI must be simultaneously oversold/overbought across 4 consecutive timeframes.

## How it works

### Phase 1: Bollinger Bands width filter

The BB width percentage must be within a configurable range. This is the same volatility gate used by STOBB.

### Phase 2: Stochastic + RSI double confirmation

- **Long**: Stochastic must be oversold AND RSI must be oversold (with optional adjustment via `AddRsiAmount`).
- **Short**: Stochastic must be overbought AND RSI must be overbought.

### Multi-timeframe variant (storsi.multi)

The conditions must hold across **4 out of 6 consecutive timeframes**, starting from the signal interval upward. The first (lowest) interval must always qualify.

An important detail: on higher timeframes, the oversold/overbought thresholds are progressively **relaxed** (by −2 per step for both RSI and Stochastic). This is intentional — higher timeframes rarely reach the same extremes as lower ones, so the threshold is loosened to make the multi-TF check feasible.

### Give-up condition

Same as STOBB: the signal is abandoned when price crosses back past the SMA(20) middle line.

## Signal conditions summary

### Long entry (storsi)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | `BBMinPercentage ≤ BB% ≤ BBMaxPercentage` | BB width within range |
| 2 | Stochastic oversold | %K and %D below oversold threshold |
| 3 | RSI oversold | RSI below oversold threshold (adjusted by `AddRsiAmount`) |

### Short entry (storsi)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | `BBMinPercentage ≤ BB% ≤ BBMaxPercentage` | BB width within range |
| 2 | Stochastic overbought | %K and %D above overbought threshold |
| 3 | RSI overbought | RSI above overbought threshold (adjusted by `AddRsiAmount`) |

## Optional additional checks

| Check | Setting | Description |
|-------|---------|-------------|
| BB position | `CheckBollingerBandsCondition` | Require price to be in the lower/upper part of the Bollinger Bands (last 3 candles within 5% of the band edge) |
| Skip first signal | `SkipFirstSignal` | Ignore the first StoRsi signal — only fire on the second occurrence within 3 candles (confirms persistence) |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `BBMinPercentage` | 1.50 | Minimum BB width percentage |
| `BBMaxPercentage` | 100.0 | Maximum BB width percentage (effectively disabled by default) |
| `AddRsiAmount` | 0 | Adjustment to the RSI oversold/overbought threshold. Positive = stricter, negative = looser. |
| `CheckBollingerBandsCondition` | false | Additionally require price to be near the BB edge |
| `CheckMacdRecovery` | false | Reserved setting (not currently used in signal logic) |
| `SkipFirstSignal` | false | Skip the first StoRsi and only signal on the second within 3 candles |

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| Stochastic (%K, %D) | Oversold/overbought detection |
| RSI | Second momentum confirmation |
| Bollinger Bands (SMA 20, 2σ) | Width filter + optional position check |
| SMA(20) | Give-up threshold |

## Strategy type

- **Mean-reversion / Oversold-overbought**
- More selective than STOBB because it requires two independent momentum indicators to agree.
- The multi variant is intentionally strict: it only fires during broad market selloffs/rallies where extreme conditions have propagated from low to high timeframes.

## File structure

```
CryptoScanner.Analyzers/Storsi/
├── StorsiPlugin.cs                       # Plugin registration (storsi + storsi.multi)
├── StoRsiSettings.cs                     # Strategy-specific settings
├── Storsi.md                             # This document
├── Config/
│   ├── StorsiConfigView.cs               # Settings UI bridge
│   ├── StrategyStorsiTabView.axaml.cs    # Settings tab UI
│   ├── StrategyStorsiTabViewModel.cs     # Settings viewmodel
│   ├── StrategyStorsiSettingsView.axaml.cs
│   └── StrategyStorsiSettingsViewModel.cs
└── Signal/
    ├── StoRsiBase.cs                     # Indicator checks, GiveUp (close vs SMA20)
    ├── StoRsiLong.cs                     # Long signal: Stoch oversold + RSI oversold
    ├── StoRsiShort.cs                    # Short signal: Stoch overbought + RSI overbought
    ├── StoRsiMultiLong.cs                # Multi-TF long: 4/6 timeframes both oversold
    └── StoRsiMultiShort.cs               # Multi-TF short: 4/6 timeframes both overbought
```

Enum values: `CryptoSignalStrategy.StoRsi = 10`, `CryptoSignalStrategy.StoRsiMulti = 11`

## Registration

Registered as a production strategy in `AnalyzerRegistration.cs`. Strategy names in the UI: **storsi**, **storsi.multi**.

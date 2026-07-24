# Candle Jump (Jump)

## Overview

The **Jump** strategy detects sudden, sharp price movements — a percentage-based spike within a short window of candles. It does not use any technical indicators; it is purely price-action based. This makes it the simplest strategy in the scanner but also one of the most universal — it works on any market condition and any timeframe.

## How it works

The strategy examines the last `CandlesLookbackCount` candles and finds the lowest and highest price points within that window. It then checks two things:

1. **Direction**: did the low come before the high (= upward jump) or did the high come before the low (= downward jump)?
2. **Magnitude**: is the percentage move from low to high at least `CandlePercentage`%?

If both conditions are met, the signal fires:
- **Long**: the minimum was reached before the maximum (price moved up sharply).
- **Short**: the maximum was reached before the minimum (price moved down sharply).

### No give-up override

Jump uses the default `GiveUp` behavior from `SignalCreateBase` — there is no strategy-specific give-up logic. The base class handles timeout-based expiry.

### No additional checks

Jump has no `AdditionalChecks` — no indicator confirmations, no trend filters, no BB conditions. The raw price move is the signal.

## Signal conditions summary

### Long entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Min before max in lookback window | Price rose: lowest point appeared before highest point |
| 2 | `(max / min - 1) × 100 ≥ CandlePercentage` | The rise is at least N% |

### Short entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Max before min in lookback window | Price dropped: highest point appeared before lowest point |
| 2 | `(max / min - 1) × 100 ≥ CandlePercentage` | The drop is at least N% |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandlesLookbackCount` | 5 | Number of candles to scan for the price jump |
| `CandlePercentage` | 4.0 | Minimum percentage move required to trigger a signal |
| `UseLowHighCalculation` | false | Use candle Low/High for price extremes instead of Open/Close |

## Indicators used

None. This is a pure price-action strategy.

## Strategy type

- **Momentum / Price spike detection**
- Not trend-following and not mean-reversion — it simply detects sharp moves in either direction.
- Commonly used as an alerting tool rather than a trading signal.
- Lower `CandlePercentage` values produce many signals; values above 5% typically catch only significant market events.

## Tuning tips

- **Alert-only use**: set `CandlePercentage` high (e.g. 6–10%) to catch only major pumps/dumps.
- **Trading use**: combine with other strategies or entry conditions to filter false breakouts.
- **Lookback window**: shorter windows (2–3 candles) catch sharper, more violent spikes; longer windows (8–10) also catch more gradual but sustained moves.
- **Low/High vs Open/Close**: enabling `UseLowHighCalculation` includes wicks in the calculation, which captures flash-wick events but may also trigger on wicks that immediately reversed.

## File structure

```
CryptoScanner.Analyzers/Jump/
├── JumpPlugin.cs                         # Plugin registration
├── JumpSettings.cs                       # Strategy-specific settings
├── Jump.md                               # This document
├── Config/
│   ├── JumpConfigView.cs                 # Settings UI bridge
│   ├── StrategyJumpTabView.axaml         # Settings tab UI
│   ├── StrategyJumpTabViewModel.cs       # Settings viewmodel
│   ├── StrategyJumpSettingsView.axaml
│   └── StrategyJumpSettingsViewModel.cs
└── Signal/
    ├── SignalCandleJumpLong.cs            # Long signal: sharp price increase in lookback window
    └── SignalCandleJumpShort.cs           # Short signal: sharp price decrease in lookback window
```

Enum value: `CryptoSignalStrategy.Jump = 0`

## Registration

Registered as a production strategy in `AnalyzerRegistration.cs`. Strategy name in the UI: **jump**.

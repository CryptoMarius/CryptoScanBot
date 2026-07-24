# VWAP Band Strategy (VBS)

## Overview

The **VBS** strategy is a mean-reversion band strategy based on volume-weighted bands. Unlike Bollinger Bands (which use SMA + standard deviation), VBS uses a VWMA (Volume Weighted Moving Average) of HLC3 as the basis and a volume-weighted standard deviation for the envelope. This gives heavier weight to price levels with higher trading volume. This is a **production** strategy.

## How it works

### Phase 1: Band calculation

The bands are computed using volume-weighted statistics over `Length` bars (default 50):

- **Basis** = VWMA(HLC3, Length) — volume-weighted average of (High+Low+Close)/3
- **vwStdev** = √(E_w[HLC3²] − E_w[HLC3]²) — volume-weighted standard deviation
- **Upper band** = Basis + Mult × vwStdev (default Mult = 2.5)
- **Lower band** = Basis − Mult × vwStdev

### Phase 2: Bollinger Bands width gate

BB width must be within `[BBMinPercentage, BBMaxPercentage]` (default 1.50%, no max). This is a separate filter from the VBS bands themselves.

### Phase 3: Optional RSI / Stochastic filter

- When `UseRsiFilter = true` (default): RSI must be oversold for longs, overbought for shorts.
- When `RequireStochOsOb = true`: Stochastic must also be at an extreme.

### Phase 4: Band break

- **Long**: candle Low or Close must be below the lower VBS band.
- **Short**: candle High or Close must be above the upper VBS band.

### Entry and stop-loss

Entry price = min(Close, band) for long, max(Close, band) for short. Optional stop-loss at `lowerBand − SLStdevFactor × vwStdev`.

### Give-up

A VBS signal is superseded if a newer VBS signal of the same side fires on the same symbol+interval.

## Signal conditions summary

### Long entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | BB width within range | BBMinPercentage ≤ BB% ≤ BBMaxPercentage |
| 2 | RSI oversold (when enabled) | Default: enabled |
| 3 | Stochastic oversold (optional) | When RequireStochOsOb is enabled |
| 4 | Low or Close < lower VBS band | Price breaks below the VWAP-based lower band |

### Short entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | BB width within range | BBMinPercentage ≤ BB% ≤ BBMaxPercentage |
| 2 | RSI overbought (when enabled) | Default: enabled |
| 3 | Stochastic overbought (optional) | When RequireStochOsOb is enabled |
| 4 | High or Close > upper VBS band | Price breaks above the VWAP-based upper band |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Length` | 50 | VWMA and volume-weighted stdev window |
| `Mult` | 2.5 | Band distance multiplier (× vwStdev) |
| `UseRsiFilter` | true | Require RSI oversold/overbought for signals |
| `UseStopLoss` | false | Enable volume-weighted stdev-based stop-loss |
| `SLStdevFactor` | 1.0 | Stop-loss distance in vwStdev units |
| `BBMinPercentage` | 1.50 | Minimum Bollinger Bands width percentage |
| `BBMaxPercentage` | 0.0 | Maximum BB width percentage (0 = disabled) |
| `RequireStochOsOb` | false | Require Stochastic oversold/overbought |

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| VWMA (HLC3, 50) | Volume-weighted moving average (band basis) |
| Volume-weighted stdev | Band envelope width |
| Bollinger Bands | Width gate (separate filter) |
| RSI | Oversold/overbought filter (enabled by default) |
| Stochastic | Optional oversold/overbought filter |

## Strategy type

- **Mean-reversion / VWAP band-break**
- Production strategy

## File structure

```
CryptoScanner.Analyzers/Vbs/
├── VbsPlugin.cs                          # Plugin registration
├── VbsSettings.cs                        # Strategy-specific settings
├── Vbs.md                                # This document
├── Config/
│   ├── StrategyVbsTabView.axaml          # Settings tab UI
│   └── StrategyVbsTabViewModel.cs        # Settings viewmodel
└── Signal/
    ├── VbsBandsHelper.cs                 # VWMA band calculation
    ├── VbsSignalVbs.cs                   # Shared base with GiveUp (supersede check)
    ├── VbsSignalLong.cs                  # Long: price below lower VBS band
    └── VbsSignalShort.cs                 # Short: price above upper VBS band
```

Enum value: `CryptoSignalStrategy.Vbs = 28`

## Registration

Registered as a **production** strategy in `AnalyzerRegistration.cs`. Strategy name in the UI: **vbs**.

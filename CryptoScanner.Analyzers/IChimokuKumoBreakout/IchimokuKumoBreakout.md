# Ichimoku Cloud (Kumo) Breakout (IchimokuKumoBreakout)

## Overview

The **IchimokuKumoBreakout** strategy detects breakouts through the Ichimoku Cloud (Kumo). It fires when price closes above or below the cloud on the current candle after being on the opposite side on the previous candle, confirmed by Kijun-Sen alignment. This is an experimental strategy (DEBUG-only).

## How it works

### Phase 1: Bollinger Bands width gate

BB width must be at least `BBMinPercentage` (from Stobb settings). No maximum is applied — this is a momentum strategy that benefits from volatility.

### Phase 2: Ichimoku calculation

Standard Ichimoku parameters are used:
- Tenkan-Sen: 9 periods
- Kijun-Sen: 26 periods
- Senkou Span B: 52 periods

Cloud values (Senkou Span A and B) are aligned to the current candle by offsetting 26 periods back from the calculated values.

### Phase 3: Breakout detection

- **Long (bullish breakout)**:
  1. `cloudTop = max(SenkouSpanA, SenkouSpanB)`
  2. Previous candle close must be below `cloudTop` (still inside or below the cloud).
  3. Current candle close must be above `cloudTop` (the breakout).
  4. Current candle close must be above Kijun-Sen (momentum confirmation).

- **Short (bearish breakout)**:
  1. `cloudBottom = min(SenkouSpanA, SenkouSpanB)`
  2. Previous candle close must be above `cloudBottom`.
  3. Current candle close must be below `cloudBottom`.
  4. Current candle close must be below Kijun-Sen.

## Signal conditions summary

### Long entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | BB width ≥ BBMinPercentage | Minimum volatility filter |
| 2 | Previous close < cloud top | Price was inside or below the cloud |
| 3 | Current close > cloud top | Bullish breakout through the cloud |
| 4 | Current close > Kijun-Sen | Kijun-Sen momentum confirmation |

### Short entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | BB width ≥ BBMinPercentage | Minimum volatility filter |
| 2 | Previous close > cloud bottom | Price was inside or above the cloud |
| 3 | Current close < cloud bottom | Bearish breakout through the cloud |
| 4 | Current close < Kijun-Sen | Kijun-Sen momentum confirmation |

## Settings

No strategy-specific settings beyond the base class. Uses `StobbPlugin.Settings.BBMinPercentage` for the BB width gate.

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| Ichimoku (9/26/52) | Cloud boundaries, Kijun-Sen confirmation |
| Bollinger Bands | Width gate (minimum volatility filter) |

## Strategy type

- **Trend-following / momentum breakout**
- Experimental (DEBUG-only)

## File structure

```
CryptoScanner.Analyzers/IChimokuKumoBreakout/
├── IChimokuKumoBreakoutPlugin.cs                 # Plugin registration
├── IChimokuKumoBreakoutSettings.cs               # Settings (base only)
├── IchimokuKumoBreakout.md                       # This document
└── Signal/
    ├── IchimokuKumoBreakoutLong.cs                # Long: bullish cloud breakout + Kijun-Sen above
    └── IchimokuKumoBreakoutShort.cs               # Short: bearish cloud breakout + Kijun-Sen below
```

Enum value: `CryptoSignalStrategy.IchimokuKumoBreakout = 54`

## Registration

Registered as a DEBUG-only strategy in `AnalyzerRegistration.cs`. Strategy name in the UI: **ichimoku.kumo**.

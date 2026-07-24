# BB + RSI + Engulfing (BbRsiEngulfing)

## Overview

The **BbRsiEngulfing** strategy fires on engulfing candlestick patterns that occur at Bollinger Band extremes, confirmed by RSI. It acts as a follow-up filter — it only triggers when there was a recent StoRsi or Stobb signal on the same symbol, catching confirmed reversals that those strategies initiated.

This is an experimental strategy (DEBUG-only).

## How it works

### Phase 1: Bollinger Bands width gate

BB width must be at least `BBMinPercentage` (uses the value from Stobb settings).

### Phase 2: BB extreme + RSI

- **Long**: previous candle closed below the lower Bollinger Band, with RSI in oversold territory (threshold + 4-unit margin).
- **Short**: previous candle closed above the upper Bollinger Band, with RSI in overbought territory (threshold − 4-unit margin).

### Phase 3: Engulfing pattern

- **Bullish engulfing (long)**: current candle closes above the previous candle's high.
- **Bearish engulfing (short)**: current candle closes below the previous candle's low.

### Phase 4: Recent signal confirmation

A StoRsi or Stobb signal must have occurred on this symbol within the last 25 candles. This ensures the engulfing pattern happens in the context of a broader oversold/overbought setup, not in isolation.

## Signal conditions summary

### Long entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | BB width ≥ BBMinPercentage | Minimum volatility filter |
| 2 | Previous close < lower BB | Prior candle was outside the lower band |
| 3 | Previous RSI oversold | RSI confirms oversold condition (with 4-unit margin) |
| 4 | Close > previous High | Bullish engulfing pattern |
| 5 | Recent StoRsi/Stobb signal | Must have occurred within the last 25 candles |

### Short entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | BB width ≥ BBMinPercentage | Minimum volatility filter |
| 2 | Previous close > upper BB | Prior candle was outside the upper band |
| 3 | Previous RSI overbought | RSI confirms overbought condition (with 4-unit margin) |
| 4 | Close < previous Low | Bearish engulfing pattern |
| 5 | Recent StoRsi/Stobb signal | Must have occurred within the last 25 candles |

## Settings

No strategy-specific settings. Uses `StobbPlugin.Settings.BBMinPercentage` for the BB width gate.

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| Bollinger Bands (20, 2σ) | Band extreme detection + width filter |
| RSI | Oversold/overbought confirmation |
| Stochastic | Used by StoRsi/Stobb signal lookback |

## Strategy type

- **Mean-reversion with candlestick pattern confirmation**
- Experimental (DEBUG-only)

## File structure

```
CryptoScanner.Analyzers/BbRsiEngulfing/
├── BbRsiEngulfingPlugin.cs               # Plugin registration
├── BbRsiEngulfingSettings.cs             # Settings (base only)
├── BbRsiEngulfing.md                     # This document
└── Signal/
    ├── BbRsiEngulfingLong.cs             # Long: bullish engulfing at lower BB + RSI oversold
    └── BbRsiEngulfingShort.cs            # Short: bearish engulfing at upper BB + RSI overbought
```

Enum value: `CryptoSignalStrategy.BbRsiEngulfing = 53`

## Registration

Registered as a DEBUG-only strategy in `AnalyzerRegistration.cs`. Strategy name in the UI: **bbrsiengulfing**.

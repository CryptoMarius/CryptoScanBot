# Trend Following (Trend)

## Overview

The **Trend** strategy fires on primary market trend changes detected via ZigZag analysis. It signals the moment a downtrend transitions to an uptrend (long) or vice versa (short). The step-in logic then waits for a pullback pivot before confirming entry. This is an experimental strategy (DEBUG-only).

## How it works

### Phase 1: Trend change detection

The strategy calculates the primary market trend using `MarketTrend.CalculateMarketTrendAsync`. A signal fires when the trend flips:
- **Long**: previous trend was Bearish → current trend is Bullish.
- **Short**: previous trend was Bullish → current trend is Bearish.

Only intervals ≥ 10m are evaluated. A deduplication guard prevents multiple signals for the same trend transition.

### Phase 2: Step-in (AllowStepIn)

After the initial signal, the strategy waits for a confirmed pullback before stepping in:

1. **Long**: wait for a ZigZag Low pivot to form (the pullback creates a higher low). Then: current candle must close above the pivot value, and the candle must be bullish (Close > Open).
2. **Short**: wait for a ZigZag High pivot. Candle must close below pivot, must be bearish.

### Give-up conditions

- **Trend revert**: if the primary trend flips back (Bullish → Bearish for long), the signal is immediately abandoned.
- **Time budget**: once a pullback pivot has formed, the signal is abandoned if price does not resume within 5 candles (counted from the pivot, not the original signal).
- **While waiting for pivot**: no time limit — only a trend revert cancels.

## Signal conditions summary

### Long entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Trend flip: Bearish → Bullish | Primary ZigZag trend changed |
| 2 | Interval ≥ 10m | Small intervals excluded |
| 3 | Not already fired | One signal per transition |

### Step-in (long)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | ZigZag Low pivot formed | Pullback creates a higher low |
| 2 | Close > pivot value | Price resumes above pullback |
| 3 | Close > Open | Bullish confirmation candle |

## Settings

No strategy-specific settings beyond the base class. Sound files: `sound-trend-oversold.wav` / `sound-trend-overbought.wav`.

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| ZigZag (Primary) | Trend detection and pivot identification |
| MarketTrend | Trend state calculation |

## Strategy type

- **Trend-following**
- Experimental (DEBUG-only)

## File structure

```
CryptoScanner.Analyzers/Trend/
├── TrendPlugin.cs                        # Plugin registration
├── TrendSettings.cs                      # Settings (base only)
├── Trend.md                              # This document
└── Signal/
    ├── SignalTrendLong.cs                 # Long: bearish→bullish trend flip + pullback step-in
    └── SignalTrendShort.cs                # Short: bullish→bearish trend flip + pullback step-in
```

Enum value: `CryptoSignalStrategy.Trend = 31`

## Registration

Registered as a DEBUG-only strategy in `AnalyzerRegistration.cs`. Strategy name in the UI: **trend**.

# SuperTrend + DLZ Breakout (SuperTrendBreakout)

## Overview

The **SuperTrendBreakout** strategy combines the SuperTrend indicator (an ATR-based trend-following tool) with DLZ (Dominant Liquidity Zone) proximity to generate high-conviction breakout signals. The strategy fires when the SuperTrend flips direction while price is near a significant support/resistance zone, combining trend confirmation with structural context.

## How it works

### 1. SuperTrend indicator

The SuperTrend indicator creates a trailing stop line based on ATR (Average True Range). It has two states:

- **Bullish** (green): the SuperTrend line is below price, acting as support. The `LowerBand` property is populated.
- **Bearish** (red): the SuperTrend line is above price, acting as resistance. The `UpperBand` property is populated.

A **flip** occurs when the indicator transitions between states. This marks a potential trend reversal and is the primary trigger for this strategy.

### 2. DLZ zone proximity

A SuperTrend flip alone is not enough — many flips occur in choppy, structureless areas. The strategy requires that price is near a DLZ zone at the time of the flip, confirming that the reversal happens at a meaningful structural level.

#### The zone-closure problem

Other strategies (DLZ near, SMC rejection) may close a zone before the SuperTrend flip occurs on the same candle or shortly after. To prevent missed signals, the strategy checks **both open and recently closed zones**:

- **Open zones** (`LongOpen` / `ShortOpen`): zones that are still active.
- **Closed zones** (`LongClosed` / `ShortClosed`): zones that were recently closed by other strategies or by invalidation. Only zones closed within the last `ClosedZoneMaxAgeCandles` candles are considered.

This dual-check ensures that a zone closed moments before the SuperTrend flip is still recognized.

### 3. Give-up condition

After a signal fires, the position is abandoned if the SuperTrend flips back against the signal direction (bullish → bearish for longs, bearish → bullish for shorts). This keeps losses short when the breakout fails.

## Signal conditions summary

### Long entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | SuperTrend flips bullish | Previous candle had `UpperBand` set (bearish), current candle has `LowerBand` set (bullish) |
| 2 | Price near DLZ support zone | Candle is inside or near an open or recently closed long DLZ zone |

### Short entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | SuperTrend flips bearish | Previous candle had `LowerBand` set (bullish), current candle has `UpperBand` set (bearish) |
| 2 | Price near DLZ resistance zone | Candle is inside or near an open or recently closed short DLZ zone |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `IncludeOpenZones` | true | Check currently active (open) DLZ zones for proximity. |
| `IncludeClosedZones` | true | Check recently closed DLZ zones for proximity. Solves the zone-closure timing problem. |
| `ClosedZoneMaxAgeCandles` | 10 | Maximum age (in candles on the signal interval) for a closed zone to still be considered. Older closed zones are ignored. |
| `ZoneLookbackCandles` | 5 | Number of candles to look back for zone proximity (reserved for future use). |

The `NearZonePercentage` setting from the DLZ configuration is reused for proximity tolerance — no separate threshold is needed.

## SuperTrend indicator parameters

The SuperTrend indicator is configured in `IntervalIndicatorHub` with:

| Parameter | Value | Description |
|-----------|-------|-------------|
| ATR Length | 10 | Number of candles for the ATR calculation |
| Multiplier | 3.0 | ATR multiplier for the band distance |

These are the standard defaults. Higher multiplier values produce fewer flips (smoother trend), lower values produce more frequent flips.

## Indicators used

| Indicator | Source | Purpose |
|-----------|--------|---------|
| SuperTrend (10, 3.0) | Skender.Stock.Indicators `SuperTrendHub` | Trend direction and flip detection |
| Bollinger Bands | Existing pipeline | Required by `IndicatorsOkay` (shared infrastructure) |
| DLZ Zones | `CryptoSymbolIntervalZones.DlzZones` | Structural support/resistance levels |

## Visual example

```
Price
  │
  │     DLZ Resistance Zone
  │    ═══════════════════
  │                  ╲
  │         ST flip   ╲ ← SuperTrend flips bearish near resistance → SHORT signal
  │         (red)      ╲
  │                     ──── Price drops
  │
  │                     ──── Price rises
  │         ST flip   ╱
  │         (green)  ╱ ← SuperTrend flips bullish near support → LONG signal
  │                ╱
  │    ═══════════════════
  │     DLZ Support Zone
  │
  └──────────────────────── Time
```

## Strategy type

- **Trend-following / Breakout**
- Best suited for higher timeframes (15m, 1h, 4h) where DLZ zones are more meaningful and SuperTrend flips less noisy.
- The combination of trend reversal (SuperTrend) with structural context (DLZ) filters out many false breakouts that either signal would produce alone.

## Tuning tips

- **More signals**: increase `ClosedZoneMaxAgeCandles` to accept zones that were closed further back.
- **Fewer, higher-quality signals**: set `IncludeClosedZones = false` to require the zone to still be active at flip time.
- **SuperTrend sensitivity**: the ATR multiplier (3.0) and length (10) are hardcoded in the indicator hub. Lower multiplier = more flips, higher = fewer but more significant flips. Adjust in `IntervalIndicatorHub.cs` if needed.
- **Zone quality**: the DLZ `ZoneStartApply` and `NearZonePercentage` settings from the general DLZ configuration also apply here.

## File structure

```
CryptoScanner.Analyzers/SuperTrendBreakout/
├── SuperTrendBreakoutPlugin.cs                        # Plugin registration
├── SuperTrendBreakout.md                              # This document
└── Signal/
    ├── SignalSuperTrendBreakoutBase.cs                 # SuperTrend flip detection, DLZ zone proximity (open + closed), GiveUp
    ├── SignalSuperTrendBreakoutLong.cs                 # Long signal: bullish flip + DLZ support zone nearby
    └── SignalSuperTrendBreakoutShort.cs                # Short signal: bearish flip + DLZ resistance zone nearby
```

Settings class: `CryptoScanner.Core/Settings/Strategy/SuperTrendBreakoutSettings.cs`
Enum value: `CryptoSignalStrategy.SuperTrendBreakout = 56`

## Registration

Registered as an experimental strategy behind `#if DEBUG` in `AnalyzerRegistration.cs`. Strategy name in the UI: **supertrendbreakout**.

## Dependencies

- Requires the **SuperTrend indicator** fields on `CryptoData` (`SuperTrend`, `SuperTrendUpperBand`, `SuperTrendLowerBand`), populated by `IntervalIndicatorHub` (DEBUG builds only).
- Requires **DLZ zones** to be active (configured in the DLZ zone settings with at least one interval enabled).

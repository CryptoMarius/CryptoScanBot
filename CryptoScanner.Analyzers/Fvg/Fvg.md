# Fair Value Gap (FVG)

## Overview

The **FVG** strategy detects price interaction with Fair Value Gaps — unfilled price gaps identified by the ICT (Inner Circle Trader) methodology. Zones are pre-calculated by `ZoneFvg` and the strategy fires alarm signals when price wicks into an open FVG zone. This is a **production** strategy.

## How it works

### Zone source

FVG zones are identified by `ZoneFvg` using the classic three-candle gap pattern:
- **Bullish FVG**: candle[2].High < candle[0].Low — a gap below current price (demand zone).
- **Bearish FVG**: candle[2].Low > candle[0].High — a gap above current price (supply zone).

Each zone has Top/Bottom boundaries, a Side (Long/Short), TouchCount, and is tracked in the open/closed zone lists.

### Signal: FVG touch

The strategy iterates over the configured `IntervalList` (default: `["1h", "4h", "1d"]`):

- **Long**: for each open bullish FVG zone, if the candle wick intersects the zone (Low ≤ zone.Top AND High ≥ zone.Bottom), fire an alarm.
- **Short**: for each open bearish FVG zone, if the candle wick intersects the zone (High ≥ zone.Bottom AND Low ≤ zone.Top), fire an alarm.

This is an **alarm-only** strategy — zone closure is handled by `ZoneFvg` via `ZoneInvalidation`, not by the signal itself. At most one alarm per zone per hour.

## Signal conditions summary

### Long entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Open bullish FVG zone exists | Zone with Side = Long in configured intervals |
| 2 | Candle wick enters zone | Low ≤ zone.Top AND High ≥ zone.Bottom |
| 3 | Not already alarmed this hour | Rate-limited to once per zone per hour |

### Short entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Open bearish FVG zone exists | Zone with Side = Short in configured intervals |
| 2 | Candle wick enters zone | High ≥ zone.Bottom AND Low ≤ zone.Top |
| 3 | Not already alarmed this hour | Rate-limited to once per zone per hour |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `IntervalList` | ["1h", "4h", "1d"] | Timeframe intervals to monitor |
| `MinimumPercentage` | 0.25 | Minimum FVG zone width percentage |
| `NearZonePercentage` | 0.25 | Proximity percentage for combined strategy checks |
| `MaxTouches` | 2 | Max wick touches before zone exhaustion |
| `RejectionLookback` | 2 | Candles to look back for zone rejection |
| `DisqualifyOnMitigation` | false | Disqualify on 50% midpoint penetration |

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| FVG zones (pre-calculated) | Three-candle gap pattern detection |

## Strategy type

- **Zone-based (ICT Fair Value Gap)**
- Production strategy

## File structure

```
CryptoScanner.Analyzers/Fvg/
├── FvgPlugin.cs                          # Plugin registration
├── Fvg.md                                # This document
├── Config/
│   ├── StrategyFvgTabView.axaml          # Settings tab UI
│   └── StrategyFvgTabViewModel.cs        # Settings viewmodel
└── Signal/
    ├── SignalFairValueGapLong.cs          # Long: alarm on bullish FVG zone touch
    └── SignalFairValueGapShort.cs         # Short: alarm on bearish FVG zone touch
```

Settings class: `CryptoScanner.Core/Settings/Strategy/SettingsSignalStrategyFvg.cs`
Enum value: `CryptoSignalStrategy.FairValueGap = 1003`

## Registration

Registered as a **production** strategy in `AnalyzerRegistration.cs`. Strategy name in the UI: **fvg**.
